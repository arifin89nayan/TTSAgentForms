using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using TTSAgent.Core;
using TTSAgent.Models;

namespace TTSAgent.UI.Screens
{
    public partial class HumanReviewScreen : UserControl, IScreen
    {
        private readonly AppState _state;
        private readonly Action<string> _nav;
        private ReviewItem _current;

        public HumanReviewScreen() { InitializeComponent(); }

        public HumanReviewScreen(AppState state, Action<string> nav) : this()
        {
            _state = state; _nav = nav;

            cmbCategory.Items.AddRange(new object[]
            { "place_name", "shrine_name", "museum_name", "cultural_term", "historical_term", "technical_term", "general_word" });
            cmbSaveType.Items.AddRange(new object[] { "Fixed List", "General Only", "Skip" });
            cmbCategory.SelectedIndex = 0; cmbSaveType.SelectedIndex = 0;

            Theme.StyleGrid(dgvQueue);
            dgvQueue.Columns.Add("no", "No");
            dgvQueue.Columns.Add("word", "Word");
            dgvQueue.Columns.Add("api", "API Reading");
            dgvQueue.Columns.Add("correct", "Correct");
            dgvQueue.Columns.Add("state", "Status");
            dgvQueue.Columns[0].FillWeight = 20;
            dgvQueue.SelectionChanged += dgvQueue_SelectionChanged;
        }

        public void OnShown() { BindQueue(); SelectFirstPending(); }

        private void BindQueue()
        {
            dgvQueue.Rows.Clear();
            int n = 1;
            foreach (var i in _state.Confirmation.Items)
            {
                int idx = dgvQueue.Rows.Add(n++, i.Word, i.ApiReading, i.CorrectReading, i.State.ToString());
                dgvQueue.Rows[idx].Tag = i;
                dgvQueue.Rows[idx].DefaultCellStyle.BackColor = i.State switch
                {
                    ReviewState.Approved => Color.Honeydew,
                    ReviewState.Rejected => Color.WhiteSmoke,
                    ReviewState.Editing => Color.LemonChiffon,
                    _ => Color.White
                };
            }
            int pending = _state.Confirmation.Items.Count(i => i.State is ReviewState.Pending or ReviewState.Editing);
            lblStatus.Text = pending == 0
                ? "Confirmation list is empty — proceed to TTS Script."
                : $"{pending} word(s) awaiting review.";
        }

        private void SelectFirstPending()
        {
            foreach (DataGridViewRow row in dgvQueue.Rows)
                if (row.Tag is ReviewItem it && it.State is ReviewState.Pending or ReviewState.Editing)
                { row.Selected = true; LoadItem(it); return; }
            _current = null;
        }

        private void dgvQueue_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvQueue.SelectedRows.Count > 0 && dgvQueue.SelectedRows[0].Tag is ReviewItem it) LoadItem(it);
        }

        private void LoadItem(ReviewItem it)
        {
            _current = it;
            txtSentence.Text = it.SourceSentence;
            txtWord.Text = it.Word;
            txtApi.Text = it.ApiReading;
            txtCorrect.Text = it.CorrectReading;
            if (cmbCategory.Items.Contains(it.Category)) cmbCategory.SelectedItem = it.Category;
            cmbSaveType.SelectedItem = it.SaveType;
        }

        private void btnApprove_Click(object sender, EventArgs e) => Approve(false);
        private void btnEditApprove_Click(object sender, EventArgs e) => Approve(true);

        private void Approve(bool useEdited)
        {
            if (_current == null) { Info("Select a word from the queue first."); return; }
            _current.CorrectReading = useEdited ? txtCorrect.Text.Trim() : _current.CorrectReading;
            _current.Category = cmbCategory.SelectedItem?.ToString() ?? "general_word";
            _current.SaveType = cmbSaveType.SelectedItem?.ToString() ?? "Fixed List";
            _current.State = ReviewState.Approved;

            if (_current.SaveType == "Fixed List")
            {
                _state.Dictionary.AddOrUpdate(new DictionaryEntry
                {
                    Word = _current.Word,
                    Hiragana = _current.CorrectReading,
                    Category = MapCategory(_current.Category),
                    Status = "Approved"
                });
            }

            // Persist to the Fixed List CSV so the next script skips the LLM for this word.
            string saveMsg = null;
            if (_current.SaveType == "Fixed List")
            {
                try
                {
                    _state.Dictionary.SaveCsv();
                    saveMsg = $"Saved {_current.Word}={_current.CorrectReading} to {Path.GetFileName(_state.Dictionary.BackingCsvPath)}.";
                }
                catch (Exception ex) { Info("Could not save Fixed List CSV: " + ex.Message); }
            }

            BindQueue();
            if (saveMsg != null && !_state.Confirmation.IsEmpty) lblStatus.Text = saveMsg + "  " + lblStatus.Text;
            if (_state.Confirmation.IsEmpty) { lblStatus.Text = "All reviewed. Opening TTS Script…"; _nav("TTS Script"); }
            else SelectFirstPending();
        }

        private void btnReject_Click(object sender, EventArgs e)
        {
            if (_current == null) return;
            _current.State = ReviewState.Rejected;
            BindQueue();
            if (!_state.Confirmation.IsEmpty) SelectFirstPending();
        }

        private async void btnPreview_Click(object sender, EventArgs e)
        {
            if (_current == null) return;
            try
            {
                string ssml = _state.Tts.BuildSsml(_current.Word,
                    new[] { new TtsListRow { Word = _current.Word, Hiragana = txtCorrect.Text.Trim() } },
                    _state.Voice);
                string path = await _state.Tts.SynthesizeAsync(ssml, "preview.wav");
                using var player = new SoundPlayer(path);
                player.Play();
            }
            catch (Exception ex) { Info("Preview unavailable: " + ex.Message); }
        }

        private static string MapCategory(string c) => c switch
        {
            "place_name" or "shrine_name" or "museum_name" => "Place",
            "historical_term" => "History",
            "cultural_term" => "Culture",
            "technical_term" => "Technical",
            _ => "General"
        };

        private void Info(string m) => MessageBox.Show(m, "Human Review", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
