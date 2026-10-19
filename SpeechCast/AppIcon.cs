// Application icon. GPLv2.
// WinForms sends Icon.Handle as WM_SETICON/ICON_BIG. Icon(stream) selects 32px;
// the .NET Framework frame selector can also skip a 256px directory entry (0).
// Decode the largest embedded frame with Windows rather than stretching 32px.
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpeechCastMe
{
    internal static class AppIcon
    {
        private static Icon icon;
        private static Icon smallIcon;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        // Keep an exact small frame too: a native-handle Icon does not retain ICO
        // directory bytes, so WinForms cannot select its small frame automatically.
        internal static void Apply(Form form)
        {
            form.Icon = Value;
            var window = new IconWindow();
            form.HandleCreated += delegate { window.AssignHandle(form.Handle); ApplySmallIcon(form); };
            if (form.IsHandleCreated) { window.AssignHandle(form.Handle); ApplySmallIcon(form); }
        }

        private sealed class IconWindow : NativeWindow
        {
            protected override void WndProc(ref Message message)
            {
                // Form updates its icons again after HandleCreated. Preserve the
                // exact title-bar image during those updates and handle recreation.
                if (message.Msg == 0x0080 && message.WParam == IntPtr.Zero &&
                    message.LParam != IntPtr.Zero && smallIcon != null)
                    message.LParam = smallIcon.Handle;
                base.WndProc(ref message);
            }
        }

        private static void ApplySmallIcon(Form form)
        {
            if (smallIcon == null)
                using (Stream stream = typeof(AppIcon).Assembly.GetManifestResourceStream("SpeechCastMe.app.ico"))
                    if (stream != null) smallIcon = new Icon(stream, SystemInformation.SmallIconSize);
            if (smallIcon != null && form.ShowIcon)
                SendMessage(form.Handle, 0x0080, IntPtr.Zero, smallIcon.Handle); // WM_SETICON / ICON_SMALL
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateIconFromResourceEx(byte[] data, uint length,
            [MarshalAs(UnmanagedType.Bool)] bool isIcon, uint version, int width, int height, uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr handle);

        private static Icon ReadLargestFrame(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                if (reader.ReadUInt16() != 0 || reader.ReadUInt16() != 1)
                    throw new InvalidDataException("Invalid application icon header.");
                int count = reader.ReadUInt16();
                int bestSize = 0, bestDepth = 0;
                uint length = 0, offset = 0;
                for (int i = 0; i < count; i++)
                {
                    int width = reader.ReadByte(), height = reader.ReadByte();
                    if (width == 0) width = 256;
                    if (height == 0) height = 256;
                    reader.ReadByte(); reader.ReadByte(); reader.ReadUInt16();
                    int depth = reader.ReadUInt16();
                    uint frameLength = reader.ReadUInt32(), frameOffset = reader.ReadUInt32();
                    int size = Math.Min(width, height);
                    if (size > bestSize || (size == bestSize && depth > bestDepth))
                    {
                        bestSize = size; bestDepth = depth;
                        length = frameLength; offset = frameOffset;
                    }
                }
                if (bestSize == 0 || length > int.MaxValue ||
                    (long)offset + length > stream.Length)
                    throw new InvalidDataException("Invalid application icon frame.");
                stream.Position = offset;
                byte[] pixels = reader.ReadBytes((int)length);
                IntPtr handle = CreateIconFromResourceEx(pixels, length, true, 0x00030000,
                    bestSize, bestSize, 0);
                if (handle == IntPtr.Zero)
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                try
                {
                    // Clone owns its native handle; the shared icon lives for the process.
                    using (Icon borrowed = Icon.FromHandle(handle)) return (Icon)borrowed.Clone();
                }
                finally { DestroyIcon(handle); }
            }
        }

        internal static Icon Value
        {
            get
            {
                if (icon != null) return icon;
                try
                {
                    using (Stream stream = typeof(AppIcon).Assembly.GetManifestResourceStream("SpeechCastMe.app.ico"))
                        if (stream != null) icon = ReadLargestFrame(stream);
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
