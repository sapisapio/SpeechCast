// Bundled caption font, 2026-10-19. GPLv2.
// GenEi M Gothic v2 Black (源暎エムゴ v2) is licensed under the SIL Open Font
// License 1.1; see ThirdParty/GenEiMGothic2-OFL.txt.
using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace SpeechCastMe
{
    internal static class BundledFont
    {
        internal const string FamilyName = "GenEi M Gothic v2 Black";
        private const string ResourceName = "SpeechCastMe.Fonts.GenEiMGothic2-Black.ttf";
        private static readonly object sync = new object();
        private static PrivateFontCollection collection;
        private static FontFamily family;
        private static bool loaded;

        // GDI+ only: the font is not registered with GDI, so it is never offered by
        // the Windows font dialog (whose result GDI+ could not convert back).
        internal static FontFamily Family
        {
            get
            {
                lock (sync)
                {
                    if (loaded) return family;
                    loaded = true;
                    try
                    {
                        using (Stream stream = typeof(BundledFont).Assembly.GetManifestResourceStream(ResourceName))
                        {
                            if (stream == null) return null;
                            byte[] data = new byte[stream.Length];
                            int read = 0;
                            while (read < data.Length)
                            {
                                int count = stream.Read(data, read, data.Length - read);
                                if (count <= 0) return null;
                                read += count;
                            }
                            // The collection reads from this memory for its whole lifetime.
                            IntPtr memory = Marshal.AllocCoTaskMem(data.Length);
                            Marshal.Copy(data, 0, memory, data.Length);
                            collection = new PrivateFontCollection();
                            collection.AddMemoryFont(memory, data.Length);
                            family = collection.Families.Length > 0 ? collection.Families[0] : null;
                        }
                    }
                    catch (Exception) { family = null; }
                    return family;
                }
            }
        }

        // The comment view (IE) cannot see a GDI+ private font or load a file font
        // from its about:blank page, but it accepts the font as a data URI.
        internal static string FontFaceStyle()
        {
            try
            {
                using (Stream stream = typeof(BundledFont).Assembly.GetManifestResourceStream(ResourceName))
                using (var memory = new MemoryStream())
                {
                    if (stream == null) return "";
                    stream.CopyTo(memory);
                    return "<style>@font-face{font-family:'" + FamilyName + "';src:url(data:font/ttf;base64,"
                        + Convert.ToBase64String(memory.ToArray()) + ") format('truetype')}</style>";
                }
            }
            catch (Exception) { return ""; }
        }

        // Settings store the English family name; GDI+ reports a localized one.
        internal static FontFamily Resolve(string name)
        {
            FontFamily bundled = Family;
            if (bundled == null || string.IsNullOrEmpty(name)) return null;
            return name == FamilyName || name == bundled.Name || name == bundled.GetName(0x0411) ? bundled : null;
        }
    }
}
