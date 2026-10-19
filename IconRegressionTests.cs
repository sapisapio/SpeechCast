// csc /platform:x86 /r:System.Drawing.dll /r:System.Windows.Forms.dll IconRegressionTests.cs
// IconRegressionTests.exe SpeechCast/app3.ico path/to/SpeechCastSP.exe
// GPLv2. Uses a hidden, plain Form: does not start SpeechCast, audio or networking.
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class IconRegressionTests
{
    sealed class TestForm : Form { internal void Recreate() { RecreateHandle(); } }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern uint PrivateExtractIcons(string file, int index, int cx, int cy,
        IntPtr[] icons, uint[] ids, uint count, uint flags);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr w, IntPtr l);
    static int checks;
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++; Console.WriteLine("PASS " + message);
    }
    static Bitmap Extract(string file, int size)
    {
        var handles = new IntPtr[1]; var ids = new uint[1];
        if (PrivateExtractIcons(Path.GetFullPath(file), 0, size, size, handles, ids, 1, 0) != 1 || handles[0] == IntPtr.Zero)
            throw new Exception("Cannot extract " + file + " at " + size);
        try { using (var icon = Icon.FromHandle(handles[0])) return icon.ToBitmap(); }
        finally { DestroyIcon(handles[0]); }
    }
    static bool Equal(Bitmap a, Bitmap b)
    {
        if (a.Size != b.Size) return false;
        for (int y = 0; y < a.Height; y++)
            for (int x = 0; x < a.Width; x++)
                if (a.GetPixel(x, y).ToArgb() != b.GetPixel(x, y).ToArgb()) return false;
        return true;
    }
    [STAThread] static void Main(string[] args)
    {
        try { Run(args); }
        catch (Exception error) { Console.Error.WriteLine(error.GetType().Name + ": " + error.Message); Environment.ExitCode = 1; }
    }
    static void Run(string[] args)
    {
        int[] sizes = {16,20,24,32,40,48,55,64,96,128,137,192,256};
        byte[] ico = File.ReadAllBytes(args[0]);
        Check(BitConverter.ToUInt16(ico, 4) == sizes.Length, "all 13 sizes present");
        for (int i = 0; i < sizes.Length; i++)
        {
            int width = ico[6 + i * 16]; if (width == 0) width = 256;
            Check(width == sizes[i], "native ICO frame " + sizes[i]);
            using (var source = Extract(args[0], sizes[i]))
            using (var embedded = Extract(args[1], sizes[i]))
            {
                Check(Equal(source, embedded), "EXE equals ICO pixel-for-pixel at " + sizes[i]);
                Check(embedded.GetPixel(0, 0).A == 0 && embedded.GetPixel(sizes[i]/2, sizes[i]/2).A >= 250,
                    "transparent exterior and solid disk at " + sizes[i]);
            }
        }
        Assembly app = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        using (Stream stream = app.GetManifestResourceStream("SpeechCastMe.app.ico"))
        using (var bytes = new MemoryStream())
        {
            stream.CopyTo(bytes);
            Check(Convert.ToBase64String(bytes.ToArray()) == Convert.ToBase64String(ico), "managed resource matches Win32 icon source");
            bytes.Position = 0;
            using (var legacy = new Icon(bytes)) Check(legacy.Width == 32, "reproduced old implicit 32px selection");
        }
        Type type = app.GetType("SpeechCastMe.AppIcon", true);
        var value = (Icon)type.GetProperty("Value", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null);
        Check(value.Width == 256 && value.Height == 256, "runtime icon retains 256px native image");
        using (var bitmap = value.ToBitmap())
        using (var embedded = Extract(args[1], 256))
            Check(Equal(bitmap, embedded), "runtime 256px icon matches EXE exactly");
        using (var form = new TestForm())
        {
            type.GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { form });
            IntPtr big = SendMessage(form.Handle, 0x007F, new IntPtr(1), IntPtr.Zero);
            IntPtr small = SendMessage(form.Handle, 0x007F, IntPtr.Zero, IntPtr.Zero);
            using (var i = Icon.FromHandle(big)) Check(i.Width == 256, "WM_GETICON BIG returns 256px for dock/Alt-Tab");
            using (var i = Icon.FromHandle(small)) Check(i.Size == SystemInformation.SmallIconSize, "title bar uses system small icon size");
            using (var i = Icon.FromHandle(small))
            using (var bitmap = i.ToBitmap())
            using (var source = Extract(args[0], SystemInformation.SmallIconSize.Width))
                Check(Equal(bitmap, source), "title bar uses exact ICO frame without resampling 256px");
            form.Recreate();
            big = SendMessage(form.Handle, 0x007F, new IntPtr(1), IntPtr.Zero);
            small = SendMessage(form.Handle, 0x007F, IntPtr.Zero, IntPtr.Zero);
            using (var i = Icon.FromHandle(big)) Check(i.Width == 256, "large icon survives handle recreation");
            using (var i = Icon.FromHandle(small)) Check(i.Size == SystemInformation.SmallIconSize, "small icon survives handle recreation");
        }
        type.GetMethod("UseAsDefault", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        using (var dialog = new Form())
        {
            IntPtr big = SendMessage(dialog.Handle, 0x007F, new IntPtr(1), IntPtr.Zero);
            using (var i = Icon.FromHandle(big)) Check(i.Width == 256, "default dialogs also retain 256px icon");
        }
        Console.WriteLine("Passed " + checks + " icon checks.");
    }
}
