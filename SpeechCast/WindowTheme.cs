// Main window colors for the light and dark themes, 2026-10-19. GPLv2.
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpeechCastMe
{
    internal sealed class WindowTheme
    {
        public bool IsDark;
        public Color Window, Text, MutedText, Ink, Field, Menu, Border, Separator;
        public Color HoverFill, PressedFill, CheckedFill, CheckedHoverFill, CheckedBorder;

        // The light values are the ones the toolbars already used before themes covered the window.
        public static readonly WindowTheme Light = new WindowTheme {
            Window = Color.FromArgb(248, 250, 252), Text = SystemColors.ControlText, MutedText = SystemColors.GrayText,
            Ink = ToolbarIcons.Ink, Field = SystemColors.Window, Menu = Color.White, Border = Color.FromArgb(196, 206, 219),
            Separator = Color.FromArgb(160, 174, 192),
            HoverFill = Color.FromArgb(228, 234, 242), PressedFill = Color.FromArgb(208, 218, 230),
            CheckedFill = ReaderToolbarRenderer.CheckedFill, CheckedHoverFill = Color.FromArgb(255, 228, 130), CheckedBorder = ReaderToolbarRenderer.CheckedBorder };

        // Matches the dark comment view (#171d26) one step lighter, so the view still reads as the content area.
        public static readonly WindowTheme Dark = new WindowTheme {
            IsDark = true,
            Window = Color.FromArgb(31, 38, 48), Text = Color.FromArgb(226, 232, 240), MutedText = Color.FromArgb(128, 140, 158),
            Ink = Color.FromArgb(203, 213, 225), Field = Color.FromArgb(23, 29, 38), Menu = Color.FromArgb(37, 45, 56),
            Border = Color.FromArgb(74, 88, 107), Separator = Color.FromArgb(90, 103, 122),
            HoverFill = Color.FromArgb(51, 62, 77), PressedFill = Color.FromArgb(64, 77, 95),
            CheckedFill = Color.FromArgb(92, 74, 22), CheckedHoverFill = Color.FromArgb(112, 90, 28), CheckedBorder = Color.FromArgb(196, 154, 48) };

        private static WindowTheme custom;
        private static string customKey;

        // A chosen theme color (the comment background) tints the whole window.
        public static WindowTheme For(string commentTheme, string themeColor)
        {
            bool dark = commentTheme == "dark";
            if (string.IsNullOrEmpty(themeColor)) return dark ? Dark : Light;
            string key = themeColor + (dark ? "/dark" : "/light");
            if (key == customKey) return custom;
            Color color;
            try { color = ColorTranslator.FromHtml(themeColor); }
            catch (Exception) { return dark ? Dark : Light; }
            custom = FromColor(color, dark);
            customKey = key;
            return custom;
        }

        private static WindowTheme FromColor(Color content, bool dark)
        {
            var preset = dark ? Dark : Light;
            Color text = dark ? preset.Text : Color.FromArgb(30, 34, 40);
            Color window = Mix(content, dark ? Color.White : Color.Black, dark ? 0.05F : 0.03F);
            return new WindowTheme {
                IsDark = dark, Window = window, Text = text,
                MutedText = Mix(window, text, 0.5F), Ink = Mix(window, text, 0.8F),
                Field = Mix(content, dark ? Color.Black : Color.White, dark ? 0.15F : 0.7F),
                Menu = Mix(window, dark ? Color.White : Color.Black, 0.04F),
                Border = Mix(window, text, 0.28F), Separator = Mix(window, text, 0.4F),
                HoverFill = Mix(window, text, 0.12F), PressedFill = Mix(window, text, 0.2F),
                CheckedFill = preset.CheckedFill, CheckedHoverFill = preset.CheckedHoverFill, CheckedBorder = preset.CheckedBorder };
        }

        private static Color Mix(Color from, Color to, float amount)
        {
            return Color.FromArgb((int)Math.Round(from.R + (to.R - from.R) * amount),
                (int)Math.Round(from.G + (to.G - from.G) * amount), (int)Math.Round(from.B + (to.B - from.B) * amount));
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);

        // Dark title bar: attribute 20 on Windows 10 20H1 and later, 19 on earlier builds.
        public static void ApplyTitleBar(Form form, bool dark)
        {
            // No version check: without a compatibility manifest Windows reports 6.2, and older systems just reject the call.
            if (!form.IsHandleCreated) return;
            int value = dark ? 1 : 0;
            try
            {
                if (DwmSetWindowAttribute(form.Handle, 20, ref value, sizeof(int)) != 0)
                    DwmSetWindowAttribute(form.Handle, 19, ref value, sizeof(int));
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        // Windows 11 rounds the corners of pop-up menus that ask for it (DWMWA_WINDOW_CORNER_PREFERENCE, small).
        public static void ApplyRoundCorners(Control popup)
        {
            if (!popup.IsHandleCreated) return;
            int value = 3;
            try { DwmSetWindowAttribute(popup.Handle, 33, ref value, sizeof(int)); }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        // Dark scroll bars for native controls (list view, text boxes).
        public static void ApplyScrollBars(Control control, bool dark)
        {
            if (!control.IsHandleCreated) return;
            try { SetWindowTheme(control.Handle, dark ? "DarkMode_Explorer" : null, null); }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }
    }
}
