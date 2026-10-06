using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TTSAgent.Models;

namespace TTSAgent.Services
{
    /// <summary>
    /// Holds the words sent for human review (Screen 3 → Screen 4). Step 4 of
    /// the flowchart: place names, technical terms, and difficult words not in
    /// the Fixed List. Step 5 asks "is this list empty?"
    /// </summary>
    public class ConfirmationListStore
    {
        public List<ReviewItem> Items { get; } = new();

        public bool IsEmpty => !Items.Any(i =>
            i.State is ReviewState.Pending or ReviewState.Editing);

        public void AddFromTerm(TtsTerm t)
        {
            if (Items.Any(i => i.Word == t.Word)) return;
            Items.Add(new ReviewItem
            {
                Word = t.Word,
                ApiReading = t.ModelSuggestedReading ?? t.Hiragana,
                CorrectReading = t.Hiragana,
                SourceSentence = t.SourceSentence,
                Category = t.Category ?? "general_word",
                State = ReviewState.Pending
            });
        }

        public void Clear() => Items.Clear();

        public void ExportCsv(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("word,api_reading,correct_reading,category,save_type,state,source_sentence");
            foreach (var i in Items)
                sb.AppendLine(string.Join(",",
                    Csv(i.Word), Csv(i.ApiReading), Csv(i.CorrectReading),
                    Csv(i.Category), Csv(i.SaveType), i.State, Csv(i.SourceSentence)));
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        private static string Csv(string v) =>
            string.IsNullOrEmpty(v) ? "" : "\"" + v.Replace("\"", "\"\"") + "\"";
    }
}
