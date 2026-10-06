using System;
using System.Windows.Forms;
using TTSAgent.Core;
using TTSAgent.UI;

namespace TTSAgent
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var config = AppConfig.Load();
            var state = new AppState(config);

            Application.Run(new MainForm(state));
        }
    }
}
