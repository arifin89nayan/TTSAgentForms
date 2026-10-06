using System;
using System.Linq;
using System.Windows.Forms;
using TTSAgent.Core;

namespace TTSAgent.UI.Screens
{
    public partial class ScriptInputScreen : UserControl, IScreen
    {
        private readonly AppState _state;
        private readonly Action<string> _nav;

        public ScriptInputScreen() { InitializeComponent(); }

        public ScriptInputScreen(AppState state, Action<string> nav) : this()
        {
            _state = state; _nav = nav;

            cmbVoice.Items.AddRange(new object[]
            { "ja-JP-NanamiNeural", "ja-JP-AoiNeural", "ja-JP-MayuNeural", "ja-JP-ShioriNeural",
              "ja-JP-KeitaNeural", "ja-JP-DaichiNeural", "ja-JP-NaokiNeural" });
            cmbSpeed.Items.AddRange(new object[] { "Slow", "Normal", "Fast" });
            cmbVoice.SelectedIndex = 0; cmbSpeed.SelectedIndex = 1;
        }

        public void OnShown()
        {
            txtProject.Text = _state.ProjectName;
            txtScript.Text = _state.Script;
            if (_state.Extraction != null) ShowSummary();
        }

        private async void btnExtract_Click(object sender, EventArgs e)
        {
            SaveSettings();
            if (string.IsNullOrWhiteSpace(txtScript.Text)) { Info("Please paste a script first."); return; }
            try
            {
                btnExtract.Enabled = btnCheck.Enabled = false;
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Extracting… checking Fixed List, then LLM for unknown words.";
                _state.Extraction = await _state.Extractor.ExtractAsync(txtScript.Text.Trim());
                ShowSummary();
                lblStatus.Text = _state.Extraction.LlmCalled
                    ? "Extraction complete. Opening Kanji Review…"
                    : "All words found in Fixed List — LLM skipped. Opening Kanji Review…";
                _nav("Kanji Review");
            }
            catch (Exception ex) { Info("Extraction failed: " + ex.Message); lblStatus.Text = "Extraction failed."; }
            finally { btnExtract.Enabled = btnCheck.Enabled = true; Cursor = Cursors.Default; }
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            SaveSettings();
            if (_state.Dictionary.Count == 0) { Info("Import a dictionary first (Dictionary screen)."); return; }
            var matches = _state.Dictionary.FindMatchesInScript(txtScript.Text);
            lblSummary.Text = $"Offline check — dictionary words found in script: {matches.Count}";
            lblStatus.Text = matches.Count == 0 ? "No fixed terms found." :
                "Found: " + string.Join("、", matches.Take(8).Select(m => m.Word)) + (matches.Count > 8 ? "…" : "");
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtScript.Clear(); lblSummary.Text = ""; lblStatus.Text = "Cleared.";
        }

        private void ShowSummary()
        {
            var s = _state.Extraction.Summary;
            lblSummary.Text = $"Kanji Words: {s.TotalTerms}  |  Fixed Matched: {s.FixedDictionaryMatches}  |  " +
                              $"Need Review: {s.ReviewRequiredCount}  |  Conflicts: {s.ConflictCount}";
        }

        private void SaveSettings()
        {
            _state.ProjectName = string.IsNullOrWhiteSpace(txtProject.Text) ? "Project" : txtProject.Text.Trim();
            _state.Voice = cmbVoice.SelectedItem?.ToString() ?? "ja-JP-NanamiNeural";
            _state.Speed = cmbSpeed.SelectedItem?.ToString() ?? "Normal";
            _state.Script = txtScript.Text;
        }

        private void Info(string m) => MessageBox.Show(m, "Script Input", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
