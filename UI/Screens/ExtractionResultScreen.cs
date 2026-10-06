using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TTSAgent.Core;
using TTSAgent.Models;
using TTSAgent.Services;

namespace TTSAgent.UI.Screens
{
    /// <summary>
    /// Kanji Review: one grid of Fixed + ChatGPT words. The user corrects the
    /// hiragana inline; editing auto-checks Save, and saved rows go to the
    /// Fixed List CSV so the next script skips ChatGPT for them.
    /// </summary>
    public partial class ExtractionResultScreen : UserControl, IScreen
    {
        private readonly AppState _state;
        private readonly Action<string> _nav;
        private bool _binding;

        private const string ColSave = "save", ColWord = "word", ColHira = "hira", ColSource = "src",
                             ColStatus = "status", ColModel = "model", ColDiff = "diff", ColReason = "reason";

        public ExtractionResultScreen() { InitializeComponent(); }

        public ExtractionResultScreen(AppState state, Action<string> nav) : this()
        {
            _state = state; _nav = nav;

            Theme.StyleGrid(dgvTerms);
            dgvTerms.Columns.Add(new DataGridViewCheckBoxColumn { Name = ColSave, HeaderText = "Save", FillWeight = 22 });
            AddText(ColWord, "Word", 70, true);
            AddText(ColHira, "Correct Hiragana", 80, false);
            AddText(ColSource, "Source", 36, true);
            AddText(ColStatus, "Status", 40, true);
            AddText(ColModel, "Model Reading", 70, true);
            AddText(ColDiff, "Difficulty", 36, true);
            AddText(ColReason, "Reason", 120, true);
            dgvTerms.Columns[ColHira].DefaultCellStyle.BackColor = Color.FromArgb(255, 252, 235);
            dgvTerms.Columns[ColHira].DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            dgvTerms.CurrentCellDirtyStateChanged += (_, _) =>
            {
                // Commit checkbox clicks immediately so CellValueChanged fires.
                if (dgvTerms.CurrentCell is DataGridViewCheckBoxCell) dgvTerms.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvTerms.CellValueChanged += dgvTerms_CellValueChanged;

            cmbFilter.Items.AddRange(new object[] { "All", "Fixed", "ChatGPT", "Need Review", "Conflict", "Unsaved edits" });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += (_, _) => Bind();
        }

        private void AddText(string name, string header, int weight, bool readOnly)
        {
            int i = dgvTerms.Columns.Add(name, header);
            dgvTerms.Columns[i].FillWeight = weight;
            dgvTerms.Columns[i].ReadOnly = readOnly;
            dgvTerms.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        public void OnShown() => Bind();

        // ---------- binding ----------
        private void Bind()
        {
            _binding = true;
            try
            {
                dgvTerms.Rows.Clear();
                if (_state.Extraction == null) { lblStatus.Text = "No extraction yet — run Script Input first."; return; }

                string f = cmbFilter.SelectedItem?.ToString() ?? "All";
                foreach (var t in _state.Extraction.Terms.Where(t => Match(t, f)))
                {
                    int idx = dgvTerms.Rows.Add();
                    var row = dgvTerms.Rows[idx];
                    row.Tag = t;
                    FillRow(row, t);
                }
                UpdateStatus();
            }
            finally { _binding = false; }
        }

        private static void FillRow(DataGridViewRow row, TtsTerm t)
        {
            row.Cells[ColSave].Value = t.SaveToFixed;
            row.Cells[ColWord].Value = t.Word;
            row.Cells[ColHira].Value = t.Hiragana;
            row.Cells[ColSource].Value = t.Source;
            row.Cells[ColStatus].Value = StatusText(t);
            row.Cells[ColModel].Value = t.ModelSuggestedReading ?? (t.Source == "ChatGPT" ? t.OriginalHiragana : "");
            row.Cells[ColDiff].Value = t.Difficulty ?? "";
            row.Cells[ColReason].Value = t.Reason ?? "";
            row.DefaultCellStyle.BackColor =
                t.DictionaryStatus == "conflict" ? Color.MistyRose :
                t.SaveToFixed && !t.Saved ? Color.LemonChiffon :
                t.Source == "Fixed" || t.Saved ? Color.Honeydew :
                t.ReviewRequired ? Color.FromArgb(255, 245, 230) : Color.White;
        }

        private static string StatusText(TtsTerm t)
        {
            string s = t.DictionaryStatus ?? "new";
            if (t.Source == "ChatGPT" && t.Saved) s += " · saved";
            else if (t.ReviewRequired && t.DictionaryStatus != "matched") s += " · review";
            return s;
        }

        private static bool Match(TtsTerm t, string f) => f switch
        {
            "Fixed" => t.Source == "Fixed",
            "ChatGPT" => t.Source == "ChatGPT",
            "Need Review" => t.ReviewRequired,
            "Conflict" => t.DictionaryStatus == "conflict",
            "Unsaved edits" => t.SaveToFixed && !t.Saved,
            _ => true
        };

        private void UpdateStatus()
        {
            var terms = _state.Extraction.Terms;
            int fixedN = terms.Count(t => t.Source == "Fixed");
            int gptN = terms.Count - fixedN;
            int pending = terms.Count(t => t.SaveToFixed && !t.Saved);
            lblStatus.Text = $"Fixed: {fixedN}  |  ChatGPT: {gptN}  |  Checked to save: {pending}" +
                             (_state.Extraction.LlmCalled ? "" : "  |  ChatGPT not called (all words in Fixed List)");
        }

        // ---------- editing ----------
        private void dgvTerms_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_binding || e.RowIndex < 0) return;
            var row = dgvTerms.Rows[e.RowIndex];
            if (row.Tag is not TtsTerm t) return;
            string col = dgvTerms.Columns[e.ColumnIndex].Name;

            if (col == ColHira)
            {
                t.Hiragana = FixedDictionaryService.NormalizeToHiragana(row.Cells[ColHira].Value?.ToString() ?? "");
                // Auto-check Save when the reading is changed from what was suggested.
                if (!string.Equals(t.Hiragana, t.OriginalHiragana, StringComparison.Ordinal))
                {
                    t.SaveToFixed = true;
                    t.Saved = false;
                }
            }
            else if (col == ColSave)
            {
                t.SaveToFixed = row.Cells[ColSave].Value is true;
            }
            else return;

            _binding = true;
            try { FillRow(row, t); } finally { _binding = false; }
            UpdateStatus();
        }

        // ---------- buttons ----------
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_state.Extraction == null) return;
            dgvTerms.EndEdit();
            int n = SaveChecked();
            if (n >= 0) lblStatus.Text = n == 0 ? "Nothing checked to save." :
                $"Saved {n} word(s) to {System.IO.Path.GetFileName(_state.Dictionary.BackingCsvPath)}.";
        }

        /// <summary>Writes checked rows to the Fixed List CSV. Returns count, or -1 on error.</summary>
        private int SaveChecked()
        {
            var toSave = _state.Extraction.Terms.Where(t => t.SaveToFixed && !t.Saved).ToList();
            if (toSave.Count == 0) return 0;

            var bad = toSave.Where(t => !IsValidReading(t.Hiragana)).ToList();
            if (bad.Count > 0)
            {
                Warn("Fix the hiragana before saving:\n" + string.Join("\n", bad.Select(b => $"{b.Word} = \"{b.Hiragana}\"")));
                return -1;
            }

            foreach (var t in toSave)
                _state.Dictionary.AddOrUpdate(new DictionaryEntry
                {
                    Word = t.Word,
                    Hiragana = t.Hiragana,
                    Category = MapCategory(t.Category),
                    Status = "Approved"
                });
            try { _state.Dictionary.SaveCsv(); }
            catch (Exception ex) { Warn("Could not save Fixed List CSV: " + ex.Message); return -1; }

            foreach (var t in toSave)
            {
                t.Saved = true;
                t.SaveToFixed = false;
                t.OriginalHiragana = t.Hiragana;
                if (t.DictionaryStatus == "conflict") t.DictionaryStatus = "matched";
            }
            Bind();
            return toSave.Count;
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (_state.Extraction == null) { _nav("Script Input"); return; }
            dgvTerms.EndEdit();

            var bad = _state.Extraction.Terms.Where(t => !IsValidReading(t.Hiragana)).ToList();
            if (bad.Count > 0)
            {
                Warn("These words need a valid hiragana reading:\n" +
                     string.Join("\n", bad.Take(15).Select(b => $"{b.Word} = \"{b.Hiragana}\"")));
                return;
            }

            int pending = _state.Extraction.Terms.Count(t => t.SaveToFixed && !t.Saved);
            if (pending > 0)
            {
                var ans = MessageBox.Show($"{pending} checked word(s) are not saved to the Fixed List yet.\nSave them now?",
                    "Kanji Review", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (ans == DialogResult.Cancel) return;
                if (ans == DialogResult.Yes && SaveChecked() < 0) return;
            }

            _state.Extraction.Reviewed = true;
            _state.FinalTtsList.Clear();
            _state.FinalTtsList.AddRange(_state.Pipeline.BuildFinalList(
                _state.Extraction, _state.Script, _state.Confirmation.Items));
            _state.LastSsml = "";
            _nav("TTS Script");
        }

        private void btnBack_Click(object sender, EventArgs e) => _nav("Script Input");

        /// <summary>Optional: send review-required words to Human Review for audio preview.</summary>
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (_state.Extraction == null) return;
            int added = 0;
            foreach (var t in _state.Extraction.Terms.Where(t => t.ReviewRequired && !t.Saved))
            {
                int before = _state.Confirmation.Items.Count;
                _state.Confirmation.AddFromTerm(t);
                if (_state.Confirmation.Items.Count > before) added++;
            }
            lblStatus.Text = added == 0 ? "No new words to send." : $"Sent {added} word(s) to Human Review.";
            if (added > 0) _nav("Human Review");
        }

        // ---------- helpers ----------
        private static bool IsValidReading(string s) =>
            !string.IsNullOrWhiteSpace(s) &&
            s.All(c => (c >= 'ぁ' && c <= 'ゟ') || c == 'ー' || c == '・' || c == ' ' || c == '　');

        private static string MapCategory(string c) => c switch
        {
            "place_name" or "shrine_name" or "museum_name" => "Place",
            "historical_term" => "History",
            "cultural_term" => "Culture",
            "technical_term" => "Technical",
            _ => "General"
        };

        private static void Warn(string m) => MessageBox.Show(m, "Kanji Review", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
