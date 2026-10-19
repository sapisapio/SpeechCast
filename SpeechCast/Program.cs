using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SpeechCastMe
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppIcon.UseAsDefault();
            Application.Run(new FormMain());
        }
    }
}
