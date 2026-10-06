using System;
using System.IO;
using System.Text.Json;

namespace TTSAgent.Core
{
    /// <summary>
    /// Keys and endpoints. Reads from appsettings.json next to the exe so you
    /// don't hardcode secrets. Falls back to environment variables.
    /// </summary>
    public class AppConfig
    {
        public string LlmApiKey { get; set; } = "";
        public string LlmModel { get; set; } = "gpt-6-luna";

        public string AzureSpeechKey { get; set; } = "";
        public string AzureSpeechRegion { get; set; } = "japaneast";

        // Folder where generated .wav files and exported lists are written.
        public string OutputFolder { get; set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TTSAgent");

        // Fixed List CSV that is checked before the LLM and grows with every
        // approved word. Empty = auto-locate sample_fixed_list.csv (see ResolveFixedListPath).
        public string FixedListCsvPath { get; set; } = "";

        public const string DefaultFixedListFile = "sample_fixed_list.csv";

        /// <summary>
        /// Finds the Fixed List CSV. Walks up from the exe folder so that when
        /// running from bin\Debug\net8.0-windows the project's own CSV is used
        /// (and grows), not a copy that is wiped on rebuild.
        /// </summary>
        public string ResolveFixedListPath()
        {
            if (!string.IsNullOrWhiteSpace(FixedListCsvPath))
                return Path.GetFullPath(Path.IsPathRooted(FixedListCsvPath)
                    ? FixedListCsvPath
                    : Path.Combine(AppContext.BaseDirectory, FixedListCsvPath));

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, DefaultFixedListFile);
                if (File.Exists(candidate)) return candidate;
            }
            return Path.Combine(AppContext.BaseDirectory, DefaultFixedListFile);
        }

        public static AppConfig Load()
        {
            var cfg = new AppConfig();
            string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            try
            {
                if (File.Exists(path))
                {
                    var loaded = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path));
                    if (loaded != null) cfg = loaded;
                }
            }
            catch { /* fall back to defaults / env vars */ }

            // Environment variables override the file if present.
            cfg.LlmApiKey      = Env("OPENAI_API_KEY",     cfg.LlmApiKey);
            cfg.AzureSpeechKey = Env("AZURE_SPEECH_KEY",   cfg.AzureSpeechKey);
            cfg.AzureSpeechRegion = Env("AZURE_SPEECH_REGION", cfg.AzureSpeechRegion);

            Directory.CreateDirectory(cfg.OutputFolder);
            return cfg;
        }

        private static string Env(string name, string fallback)
        {
            string v = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(v) ? fallback : v;
        }
    }
}
