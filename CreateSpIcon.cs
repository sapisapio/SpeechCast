using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
class CreateSpIcon
{
    [DllImport("user32.dll")] static extern bool DestroyIcon(System.IntPtr handle);
    static void Main(string[] args)
    {
        using(var bitmap=new Bitmap(32,32))
        using(var g=Graphics.FromImage(bitmap))
        using(var background=new SolidBrush(Color.Black))
        using(var font=new Font("Segoe UI",25,FontStyle.Bold | FontStyle.Italic,GraphicsUnit.Pixel))
        using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())
        {
            g.FillRectangle(background,0,0,32,32);
            g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
            // Match the original icon's upper-left S / lower-right letter arrangement.
            g.DrawString("S",font,Brushes.White,new PointF(0,-4),format);
            g.DrawString("P",font,Brushes.White,new PointF(13,6),format);
            if(args.Length>1)bitmap.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);
            var handle=bitmap.GetHicon();
            try {using(var icon=Icon.FromHandle(handle))using(var file=File.Create(args[0]))icon.Save(file);}
            finally {DestroyIcon(handle);}
        }
    }
}
