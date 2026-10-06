using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TTSAgent.Core;
using TTSAgent.Models;

namespace TTSAgent.Services
{
    /// <summary>
    /// Steps 1-4 of the flowchart: extract kanji words, then match against the
    /// Fixed List. The LLM proposes readings; this class then RE-ENFORCES the
    /// dictionary rules in C# so the Fixed List is always the final authority.
    /// </summary>
    public class KanjiExtractionService
    {
        private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(3) };

        private readonly AppConfig _config;
        private readonly FixedDictionaryService _dict;

        public KanjiExtractionService(AppConfig config, FixedDictionaryService dict)
        {
            _config = config;
            _dict = dict;
        }

        public async Task<TtsExtractionResult> ExtractAsync(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
                throw new ArgumentException("Script is empty.");

            // ---- Step 1: Fixed List first (no tokens, no human review) ----
            var known = _dict.FindMatchesInScript(script);
            int[] covered = BuildCoverage(script, known.Select(k => k.Word));
            var fixedTerms = known
                .Where(k => IsUsed(script, k.Word, covered))
                .Select(k => new TtsTerm
                {
                    Word = k.Word,
                    Hiragana = k.Hiragana,
                    OriginalHiragana = k.Hiragana,
                    SourceSentence = FindSentence(script, k.Word),
                    Category = "fixed_dictionary",
                    DictionaryStatus = "matched",
                    ReadingSource = "fixed_dictionary",
                    ReviewRequired = false,
                    Confidence = "high",
                    Difficulty = "-",
                    Reason = "Found in Fixed List (not sent to ChatGPT).",
                    Saved = true
                }).ToList();

            // ---- Step 2: mask fixed words so ChatGPT only sees what is unknown ----
            string masked = MaskScript(script, covered);

            if (!HasUncoveredKanji(script, covered))
            {
                var local = new TtsExtractionResult { Terms = fixedTerms, LlmCalled = false, MaskedScript = masked };
                RecomputeSummary(local);
                return local;
            }

            if (string.IsNullOrWhiteSpace(_config.LlmApiKey))
                throw new InvalidOperationException("LLM API key is not configured (appsettings.json or OPENAI_API_KEY).");

            string userMessage = "Input Japanese Script:\n" + masked;

            var body = new
            {
                model = _config.LlmModel,
                max_completion_tokens = 8000,
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = userMessage }
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            req.Headers.Add("Authorization", "Bearer " + _config.LlmApiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var resp = await Http.SendAsync(req).ConfigureAwait(false);
            string raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                throw new Exception("Extraction API failed: " + raw);

            string text = ExtractText(raw);
            string json = StripFences(text);

            var result = JsonSerializer.Deserialize<TtsExtractionResult>(json)
                         ?? throw new Exception("Model returned no parsable list.");
            result.Terms ??= new List<TtsTerm>();

            // Drop anything the Fixed List already answers: exact known words, and
            // fragments that only ever appear inside a known word (土器 inside 無文土器).
            var knownWords = new HashSet<string>(known.Select(k => k.Word), StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            result.Terms = result.Terms
                .Where(t => !string.IsNullOrWhiteSpace(t.Word))
                .Where(t => !t.Word.Contains("FIXED") && !t.Word.Contains('【') && !t.Word.Contains('】'))
                .Where(t => script.Contains(t.Word))   // must exist in the real script
                .Where(t => !knownWords.Contains(t.Word) && !IsFullyCovered(script, t.Word, covered))
                .Where(t => seen.Add(t.Word))
                .ToList();

            foreach (var t in result.Terms)
            {
                t.Hiragana = FixedDictionaryService.NormalizeToHiragana(t.Hiragana);
                t.OriginalHiragana = t.Hiragana;
                t.ModelSuggestedReading = t.Hiragana;
                t.SourceSentence = FindSentence(script, t.Word);   // unmasked sentence for reviewers
                t.ReadingSource = "model_suggestion";
            }

            EnforceDictionary(result);   // safety net for words added mid-session
            result.Terms.InsertRange(0, fixedTerms);
            result.LlmCalled = true;
            result.MaskedScript = masked;
            RecomputeSummary(result);
            return result;
        }

        // ---- Fixed List coverage / masking helpers ----

        /// <summary>For every script character, the id (1-based occurrence number) of
        /// the known word covering it, or 0. Longest words claim positions first.</summary>
        private static int[] BuildCoverage(string script, IEnumerable<string> words)
        {
            var covered = new int[script.Length];
            int id = 0;
            foreach (var w in words.Where(w => !string.IsNullOrEmpty(w)).OrderByDescending(w => w.Length))
            {
                int i = 0;
                while ((i = script.IndexOf(w, i, StringComparison.Ordinal)) >= 0)
                {
                    bool free = true;
                    for (int k = i; k < i + w.Length; k++) if (covered[k] != 0) { free = false; break; }
                    if (free)
                    {
                        id++;
                        for (int k = i; k < i + w.Length; k++) covered[k] = id;
                    }
                    i += free ? w.Length : 1;
                }
            }
            return covered;
        }

        /// <summary>盛岡の大國神社では → 【FIXED】の【FIXED】では</summary>
        private static string MaskScript(string script, int[] covered)
        {
            var sb = new StringBuilder(script.Length);
            for (int i = 0; i < script.Length; i++)
            {
                if (covered[i] == 0) sb.Append(script[i]);
                else if (i == 0 || covered[i - 1] != covered[i]) sb.Append("【FIXED】");
            }
            return sb.ToString();
        }

        /// <summary>True if at least one occurrence of the word claimed its own span
        /// (a known word only found inside a longer known word is not used).</summary>
        private static bool IsUsed(string script, string word, int[] covered)
        {
            int i = 0;
            while ((i = script.IndexOf(word, i, StringComparison.Ordinal)) >= 0)
            {
                int id = covered[i];
                bool own = id != 0 && (i == 0 || covered[i - 1] != id) &&
                           (i + word.Length == script.Length || covered[i + word.Length] != id);
                for (int k = i; own && k < i + word.Length; k++) if (covered[k] != id) own = false;
                if (own) return true;
                i++;
            }
            return false;
        }

        private static bool IsFullyCovered(string script, string word, int[] covered)
        {
            int i = 0, hits = 0;
            while ((i = script.IndexOf(word, i, StringComparison.Ordinal)) >= 0)
            {
                hits++;
                for (int k = i; k < i + word.Length; k++) if (covered[k] == 0) return false;
                i += word.Length;
            }
            return hits > 0;
        }

        private static bool HasUncoveredKanji(string script, int[] covered)
        {
            for (int i = 0; i < script.Length; i++)
                if (covered[i] == 0 && IsKanji(script[i])) return true;
            return false;
        }

        private static bool IsKanji(char c) =>
            (c >= '一' && c <= '鿿') || (c >= '㐀' && c <= '䶿') ||
            (c >= '豈' && c <= '﫿') || c == '々' || char.IsSurrogate(c);

        private static string FindSentence(string script, string word)
        {
            int i = script.IndexOf(word, StringComparison.Ordinal);
            if (i < 0) return "";
            int s = script.LastIndexOfAny(new[] { '。', '\n', '！', '？' }, Math.Max(0, i - 1));
            int e = script.IndexOfAny(new[] { '。', '\n', '！', '？' }, i);
            s = s < 0 ? 0 : s + 1;
            e = e < 0 ? script.Length : e + 1;
            return script[s..e].Trim();
        }

        // ---- rules 5 & 6: dictionary wins, conflicts flagged ----
        private void EnforceDictionary(TtsExtractionResult result)
        {
            foreach (var t in result.Terms)
            {
                if (_dict.TryGetReading(t.Word, out var dicReading))
                {
                    string dic = FixedDictionaryService.NormalizeToHiragana(dicReading);
                    string model = FixedDictionaryService.NormalizeToHiragana(t.Hiragana);
                    t.OriginalHiragana = dic;
                    t.Saved = true;
                    if (string.Equals(dic, model, StringComparison.Ordinal))
                    {
                        t.DictionaryStatus = "matched";
                        t.ReadingSource = "fixed_dictionary";
                        t.Hiragana = dic;
                        if (string.IsNullOrEmpty(t.Confidence)) t.Confidence = "high";
                    }
                    else
                    {
                        t.ModelSuggestedReading = model;
                        t.Hiragana = dic;                 // keep dictionary as final
                        t.DictionaryStatus = "conflict";
                        t.ReadingSource = "fixed_dictionary";
                        t.ReviewRequired = true;
                        t.Confidence = "low";
                        t.Reason = $"Dictionary reading kept; model suggested {model}.";
                    }
                }
                else
                {
                    t.DictionaryStatus = "new";
                    if (string.IsNullOrEmpty(t.ReadingSource)) t.ReadingSource = "model_suggestion";
                }
            }
        }

        private static void RecomputeSummary(TtsExtractionResult r)
        {
            r.Summary = new TtsExtractionSummary
            {
                TotalTerms = r.Terms.Count,
                FixedDictionaryMatches = r.Terms.Count(t => t.DictionaryStatus is "matched" or "conflict"),
                NewTerms = r.Terms.Count(t => t.DictionaryStatus == "new"),
                ReviewRequiredCount = r.Terms.Count(t => t.ReviewRequired),
                ConflictCount = r.Terms.Count(t => t.DictionaryStatus == "conflict")
            };
        }

        private static string ExtractText(string apiJson)
        {
            using var doc = JsonDocument.Parse(apiJson);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0) return "";
            var message = choices[0].GetProperty("message");
            return message.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String
                ? c.GetString()
                : "";
        }

        private static string StripFences(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            string t = text.Trim();
            if (t.StartsWith("```"))
            {
                int nl = t.IndexOf('\n');
                if (nl >= 0) t = t[(nl + 1)..];
                int last = t.LastIndexOf("```", StringComparison.Ordinal);
                if (last >= 0) t = t[..last];
            }
            int s = t.IndexOf('{'), e = t.LastIndexOf('}');
            if (s >= 0 && e > s) t = t.Substring(s, e - s + 1);
            return t.Trim();
        }

        private const string SystemPrompt = @"You are a Japanese TTS pronunciation extraction assistant for a human-in-the-loop TTS script generation system.

Analyze the input Japanese script and generate a pronunciation control list so Azure TTS does not incorrectly guess difficult kanji readings.

Rules:
1. Extract kanji-containing words or meaningful phrases, not single kanji unless the single kanji is an important standalone word. Prefer word/phrase level (e.g. 江戸時代, 大國神社, 都南歴史民俗資料館, 浮世絵, 献額).
2. Provide a hiragana reading for each term.
3. 【FIXED】 marks words whose readings are already confirmed. Ignore them: never return 【FIXED】 and never guess what it hides. Only extract kanji words that are visible in the text.
4. Set review_required=true for: place/shrine/temple/museum/person/organization names, historical/cultural/technical terms, rare kanji, and uncertain or multiple-reading words.
5. Never guess silently. Give your best hiragana but set review_required=true when uncertain.
6. Each word must appear exactly as written in the input text.
7. Return ONLY valid JSON, no prose, in this structure:
{""terms"":[{""word"":"""",""hiragana"":"""",""category"":""place_name|shrine_name|museum_name|cultural_term|historical_term|technical_term|general_word|unknown"",""review_required"":true,""confidence"":""high|medium|low"",""difficulty"":""easy|medium|hard"",""reason"":""""}]}
confidence: high=common, medium=normal kanji likely reading, low=proper noun/rare/uncertain.
difficulty: how likely a TTS engine is to misread it (easy=standard reading, hard=rare kanji/irregular/proper noun).
reason: one short phrase.
Be conservative: if any risk of wrong pronunciation, set review_required=true.";
    }
}
