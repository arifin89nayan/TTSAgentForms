using System;
using System.Collections.Generic;
using TTSAgent.Models;
using TTSAgent.Services;

namespace TTSAgent.Core
{
    /// <summary>
    /// One instance is created in Program.Main and handed to every screen.
    /// Holds everything the 13-step pipeline needs to carry between screens:
    /// project settings, the loaded dictionary, the extraction result, the
    /// human-review queue, and the final merged TTS list.
    /// </summary>
    public class AppState
    {
        public AppConfig Config { get; }
        public FixedDictionaryService Dictionary { get; }
        public KanjiExtractionService Extractor { get; private set; }
        public AzureTtsService Tts { get; }
        public QualityCheckService Quality { get; }
        public TtsPipelineService Pipeline { get; }
        public ConfirmationListStore Confirmation { get; } = new();

        // --- project settings (Screen 2) ---
        public string ProjectName { get; set; } = "Museum_Edo_Script_01";
        public string Voice { get; set; } = "ja-JP-NanamiNeural";
        public string Speed { get; set; } = "Normal";
        public string Script { get; set; } = "";

        // --- pipeline artifacts ---
        public TtsExtractionResult Extraction { get; set; }
        public List<ReviewItem> ReviewQueue { get; } = new();
        public List<TtsListRow> FinalTtsList { get; } = new();
        public string LastSsml { get; set; } = "";
        public string LastAudioPath { get; set; } = "";

        // Raised whenever a screen wants the shell to refresh nav badges, etc.
        public event Action StateChanged;
        public void NotifyChanged() => StateChanged?.Invoke();

        public AppState(AppConfig config)
        {
            Config = config;
            Dictionary = new FixedDictionaryService();
            // Auto-load the Fixed List so it is checked before every LLM call
            // and approved words are appended to it.
            try { Dictionary.LoadCsv(config.ResolveFixedListPath()); } catch { /* start empty */ }
            Extractor = new KanjiExtractionService(config, Dictionary);
            Tts = new AzureTtsService(config);
            Quality = new QualityCheckService(config);
            Pipeline = new TtsPipelineService(Dictionary, Tts);
        }

        public void RebuildExtractor() =>
            Extractor = new KanjiExtractionService(Config, Dictionary);
    }
}
