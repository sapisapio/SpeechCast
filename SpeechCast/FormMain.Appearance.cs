// Font dialog helper for the UI preferences (the options moved to FormSettings), 2026-10-18. GPLv2.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        // GDI+ rejects non-TrueType selections (e.g. CFF-based .otf) when the dialog
        // converts its result; report that instead of letting the exception escape.
        private static bool ShowFontDialog(FontDialog chooser, IWin32Window owner)
        {
            try { return chooser.ShowDialog(owner) == DialogResult.OK; }
            catch (ArgumentException)
            {
                MessageBox.Show(owner, "このフォントは使用できません。TrueType形式（.ttf / .ttc）のフォントを選んでください。", "フォント", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
        }
    }
}
