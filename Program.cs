using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CanIRunGame
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int processId);

        [STAThread]
        static void Main(string[] args)
        {
            string api = Config.ApiUrl;
            int apiIndex = Array.IndexOf(args, "--api");
            if (apiIndex >= 0 && apiIndex + 1 < args.Length) api = args[apiIndex + 1];

            if (Array.IndexOf(args, "--dry-run") >= 0)
            {
                AttachConsole(-1);
                Console.WriteLine(Json.Write(Scanner.Collect(), true));
                return;
            }

            if (Array.IndexOf(args, "--send") >= 0)
            {
                AttachConsole(-1);
                try
                {
                    Dictionary<string, object> result = Scanner.Upload(api, Scanner.Collect());
                    Console.WriteLine("Code: " + result["code"]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed: " + ex.Message);
                }
                return;
            }

            bool demo = false;
#if DEMO
            demo = Array.IndexOf(args, "--demo") >= 0;
#endif
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(api, demo));
        }
    }
}
