// Export the SC artwork as a multi-resolution Windows icon. GPLv2.
// csc /r:System.Drawing.dll CreateScIcon.cs
// CreateScIcon.exe SpeechCast/Assets/sc-icon-master.png SpeechCast/app3.ico [preview.png]
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

class CreateScIcon
{
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]
    static extern IntPtr LoadImage(IntPtr instance, string file, uint type, int width, int height, uint flags);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr handle);
    static readonly int[] Sizes = {16,20,24,32,40,48,64,96,128,256};
    static Bitmap Resize(Image source, int size)
    {
        var result = new Bitmap(size,size,PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(result))
        using (var attributes = new ImageAttributes())
        {
            g.CompositingMode = CompositingMode.SourceCopy;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            g.DrawImage(source,new Rectangle(0,0,size,size),0,0,source.Width,source.Height,GraphicsUnit.Pixel,attributes);
        }
        return result;
    }
    static byte[] Encode(Bitmap bitmap, bool png)
    {
        using (var memory = new MemoryStream())
        {
            if (png) bitmap.Save(memory,ImageFormat.Png);
            else using (var writer = new BinaryWriter(memory,System.Text.Encoding.UTF8,true))
            {
                int size = bitmap.Width, maskStride = ((size + 31) / 32) * 4;
                writer.Write(40); writer.Write(size); writer.Write(size * 2);
                writer.Write((short)1); writer.Write((short)32); writer.Write(0);
                writer.Write(size * size * 4 + maskStride * size);
                writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
                for (int y = size - 1; y >= 0; y--)
                    for (int x = 0; x < size; x++)
                    {
                        var c = bitmap.GetPixel(x,y);
                        writer.Write(c.B); writer.Write(c.G); writer.Write(c.R); writer.Write(c.A);
                    }
                for (int y = size - 1; y >= 0; y--)
                {
                    var mask = new byte[maskStride];
                    for (int x = 0; x < size; x++)
                        if (bitmap.GetPixel(x,y).A == 0) mask[x / 8] |= (byte)(128 >> (x % 8));
                    writer.Write(mask);
                }
            }
            return memory.ToArray();
        }
    }
    static void Main(string[] args)
    {
        if (args.Length < 2) throw new ArgumentException("Input PNG and output ICO are required.");
        using (var source = Image.FromFile(args[0]))
        {
            var frames = new List<byte[]>();
            foreach (int size in Sizes)
                using (var frame = Resize(source,size)) frames.Add(Encode(frame,size == 256));
            using (var writer = new BinaryWriter(File.Create(args[1])))
            {
                writer.Write((short)0); writer.Write((short)1); writer.Write((short)Sizes.Length);
                int offset = 6 + 16 * Sizes.Length;
                for (int i = 0; i < Sizes.Length; i++)
                {
                    writer.Write((byte)(Sizes[i] == 256 ? 0 : Sizes[i]));
                    writer.Write((byte)(Sizes[i] == 256 ? 0 : Sizes[i]));
                    writer.Write((byte)0); writer.Write((byte)0);
                    writer.Write((short)1); writer.Write((short)32);
                    writer.Write(frames[i].Length); writer.Write(offset);
                    offset += frames[i].Length;
                }
                foreach (var frame in frames) writer.Write(frame);
            }
            foreach (int size in Sizes)
            {
                // Windows resolves the compressed 256px frame directly; the
                // old managed Icon(file,size) selector can fall back to 128px.
                var handle = LoadImage(IntPtr.Zero,Path.GetFullPath(args[1]),1,size,size,0x10);
                if (handle == IntPtr.Zero) throw new Exception("Windows could not load ICO size " + size);
                try
                {
                    using (var icon = Icon.FromHandle(handle))
                    using (var bitmap = icon.ToBitmap())
                        if (bitmap.Width != size || bitmap.Height != size || bitmap.GetPixel(0,0).A != 0)
                            throw new Exception("ICO size/transparency validation failed: " + size);
                }
                finally { DestroyIcon(handle); }
            }
            if (args.Length > 2)
            {
                using (var preview = new Bitmap(640,180))
                using (var g = Graphics.FromImage(preview))
                using (var font = new Font("Segoe UI",9))
                {
                    g.Clear(Color.FromArgb(244,246,249));
                    g.FillRectangle(Brushes.DarkSlateGray,0,90,640,90);
                    int[] examples = {16,20,24,32,40,48,64};
                    for (int i = 0; i < examples.Length; i++)
                    {
                        int size = examples[i], x = 20 + i * 88;
                        using (var icon = new Icon(args[1],size,size))
                        {
                            g.DrawIcon(icon,new Rectangle(x,12,size,size));
                            g.DrawIcon(icon,new Rectangle(x,102,size,size));
                        }
                        g.DrawString(size + "px",font,Brushes.DimGray,x,73);
                    }
                    preview.Save(args[2],ImageFormat.Png);
                }
            }
        }
        Console.WriteLine("Validated 10 ICO sizes, 32-bit alpha, native Windows decoding.");
    }
}
