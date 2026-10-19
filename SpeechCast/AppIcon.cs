// Application icon with every size (16-256 px), 2026-10-19. GPLv2.
// Icon.ExtractAssociatedIcon only returns a single 32 px image, which Windows then
// stretches for the title bar and taskbar; loading the .ico itself lets each place
// use its own size.
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace SpeechCastMe
{
    internal static class AppIcon
    {
        private static Icon icon;

        internal static Icon Value
        {
            get
            {
                if (icon != null) return icon;
                try
                {
                    using (Stream stream = typeof(AppIcon).Assembly.GetManifestResourceStream("SpeechCastMe.app.ico"))
                        if (stream != null) icon = new Icon(stream);
                }
                catch (Exception) { }
                if (icon == null) icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                return icon;
            }
        }

        // Dialogs without their own icon (settings, bookmarks, ...) use it too.
        internal static void UseAsDefault()
        {
            try
            {
                FieldInfo field = typeof(Form).GetField("defaultIcon", BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null) field.SetValue(null, Value);
            }
            catch (Exception) { }
        }
    }
}
