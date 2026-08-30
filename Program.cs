using System;
using System.IO;
using System.Windows.Forms;

namespace SteamAchievementGenerator
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (ConsoleRunner.WantsConsole(args))
                return ConsoleRunner.Run(args);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string startupFile = args != null && args.Length == 1 && File.Exists(args[0]) ? args[0] : null;
            Application.Run(new MainForm(startupFile));
            return 0;
        }
    }
}
