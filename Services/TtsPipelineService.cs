using System;
using System.Collections.Generic;
using System.Linq;
using TTSAgent.Models;

namespace TTSAgent.Services
{
    /// <summary>
    /// Turns the reviewed extraction into the final TTS list and SSML.
    ///   Fixed List   = fixed / matched / conflict words (+ Fixed List words found in the script)
    ///   General List = reviewed new / ChatGPT words
    ///   Final list   = Fixed + General, Fixed wins on duplicates.
    /// </summary>
    public class TtsPipelineService
    {
        private readonly FixedDictionaryService _dict;
        private readonly AzureTtsService _tts;

        public TtsPipelineService(FixedDictionaryService dict, AzureTtsService tts)
        {
            _dict = dict;
            _tts = tts;
        }

        public List<TtsListRow> BuildFixedList(TtsExtractionResult extraction, string script,
                                               IEnumerable<ReviewItem> approved = null)
        {
            var rows = new List<TtsListRow>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var overrides = Overrides(approved);

            // Reviewed fixed rows first: the reading the human confirmed on Kanji Review.
            if (extraction != null)
                foreach (var t in extraction.Terms.Where(IsFixed))
                    Add(rows, seen, t.Word, Reading(t, overrides), "Fixed");

            // Any other Fixed List word in the script (e.g. saved during this session).
            foreach (var e in _dict.FindMatchesInScript(script ?? ""))
                Add(rows, seen, e.Word, e.Hiragana, "Fixed");

            return rows;
        }

        public List<TtsListRow> BuildGeneralList(TtsExtractionResult extraction,
                                                 IEnumerable<ReviewItem> approved = null)
        {
            var rows = new List<TtsListRow>();
            if (extraction == null) return rows;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var overrides = Overrides(approved);

            foreach (var t in extraction.Terms.Where(t => !IsFixed(t)))
                Add(rows, seen, t.Word, Reading(t, overrides), "General");
            return rows;
        }

        /// <summary>Fixed List has highest priority: a General row is dropped when the
        /// same word is already in the Fixed List.</summary>
        public List<TtsListRow> Merge(IEnumerable<TtsListRow> fixedList, IEnumerable<TtsListRow> generalList)
        {
            var merged = new List<TtsListRow>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in fixedList.Concat(generalList))
                if (seen.Add(r.Word)) merged.Add(r);
            return merged;
        }

        public List<TtsListRow> BuildFinalList(TtsExtractionResult extraction, string script,
                                               IEnumerable<ReviewItem> approved = null) =>
            Merge(BuildFixedList(extraction, script, approved), BuildGeneralList(extraction, approved));

        public string GenerateSsml(string script, IEnumerable<TtsListRow> finalList, string voice,
                                   string style = "narration", int ratePercent = 0, int pitchPercent = 0) =>
            _tts.BuildSsml(script, finalList, voice, style, ratePercent, pitchPercent);

        // ---- helpers ----
        private static bool IsFixed(TtsTerm t) =>
            t.DictionaryStatus is "matched" or "conflict" || t.ReadingSource == "fixed_dictionary";

        /// <summary>Human Review approvals win over the extraction reading.</summary>
        private static Dictionary<string, string> Overrides(IEnumerable<ReviewItem> approved) =>
            (approved ?? Enumerable.Empty<ReviewItem>())
                .Where(i => i.State == ReviewState.Approved && !string.IsNullOrWhiteSpace(i.CorrectReading))
                .GroupBy(i => i.Word)
                .ToDictionary(g => g.Key, g => g.Last().CorrectReading, StringComparer.Ordinal);

        private static string Reading(TtsTerm t, Dictionary<string, string> overrides) =>
            overrides.TryGetValue(t.Word, out var r) ? r : t.Hiragana;

        private static void Add(List<TtsListRow> rows, HashSet<string> seen, string word, string reading, string source)
        {
            if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(reading)) return;
            if (seen.Add(word))
                rows.Add(new TtsListRow { Word = word, Hiragana = FixedDictionaryService.NormalizeToHiragana(reading), Source = source });
        }
    }
}
