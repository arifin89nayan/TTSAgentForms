using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TTSAgent.Core;
using TTSAgent.Models;

namespace TTSAgent.UI.Screens
{
    public partial class TtsListScreen : UserControl, IScreen
    {
        private readonly AppState _state;
        private readonly Action<string> _nav;

        public TtsListScreen() { InitializeComponent(); }

        public TtsListScreen(AppState state, Action<string> nav) : this()
        {
            _state = state; _nav = nav;

            Theme.StyleGrid(dgvFinal);
            dgvFinal.Columns.Add("no", "No");
            dgvFinal.Columns.Add("word", "Word");
            dgvFinal.Columns.Add("hira", "Hiragana");
            dgvFinal.Columns.Add("src", "Source");
            dgvFinal.Columns.Add("use", "Use Type");
            dgvFinal.Columns[0].FillWeight = 20;
        }

        public void OnShown() => Build();

        private void btnGenGeneral_Click(object sender, EventArgs e) => Build();
        private void btnMerge_Click(object sender, EventArgs e) => Build();

        private void Build()
        {
            _state.FinalTtsList.Clear();
            if (_state.Extraction == null) { lblStats.Text = "No extraction yet."; return; }

            // Fixed List (highest priority) + General List → final TTS list.
            var fixedList = _state.Pipeline.BuildFixedList(_state.Extraction, _state.Script, _state.Confirmation.Items);
            var generalList = _state.Pipeline.BuildGeneralList(_state.Extraction, _state.Confirmation.Items);
            _state.FinalTtsList.AddRange(_state.Pipeline.Merge(fixedList, generalList));

            int fixedN = _state.FinalTtsList.Count(r => r.Source == "Fixed");
            int genN = _state.FinalTtsList.Count(r => r.Source == "General");
            // Conflicts are resolved once the user confirms readings on Kanji Review.
            int conflicts = _state.Extraction.Reviewed ? 0 : _state.Extraction.Terms.Count(t =>
                t.DictionaryStatus == "conflict" &&
                _state.Confirmation.Items.All(i => i.Word != t.Word || i.State != ReviewState.Approved));

            lblStats.Text = $"Fixed List Words: {fixedN}     General Words: {genN}\r\n" +
                            $"Total TTS Terms: {_state.FinalTtsList.Count}     Conflicts: {conflicts}";

            dgvFinal.Rows.Clear();
            int n = 1;
            foreach (var r in _state.FinalTtsList)
            {
                int idx = dgvFinal.Rows.Add(n++, r.Word, r.Hiragana, r.Source, r.UseType);
                dgvFinal.Rows[idx].DefaultCellStyle.BackColor = r.Source == "Fixed" ? Color.Honeydew : Color.White;
            }
        }

        private void btnGenTts_Click(object sender, EventArgs e)
        {
            Build();
            if (lblStats.Text.Contains("Conflicts: 0"))
            { lblStatus.Text = "Final TTS list ready. Opening Azure TTS…"; _nav("Azure TTS"); }
            else
            { lblStatus.Text = "Resolve remaining conflicts in Kanji Review before generating."; _nav("Kanji Review"); }
        }
    }
}
