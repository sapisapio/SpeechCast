// Speech Cast SP, 2026-10-18. GPLv2.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace SpeechCastMe
{
    internal static class CaptionRasterizer
    {
        // Rasterize at the actual output size with font hinting. No fractional rescale,
        // ClearType colour fringes, or unhinted vector fill on small Japanese counters.
        internal static void Draw(Graphics target, Rectangle rectangle, string text, CaptionFont style, bool aaMode)
        {
            var config = FormMain.UserConfig;
            rectangle.Offset(config.CaptionIndentLeftPadding, config.CaptionIndentTopPadding);
            // A chosen effect (not for AA, whose shapes it would smear); 標準 keeps the original look.
            if (!aaMode && !string.IsNullOrEmpty(config.CaptionEffect) && config.CaptionEffect != CaptionEffect.Standard)
            {
                var accent = FormFreeTelop.ParseColor(config.CaptionEffectColor, CaptionEffect.HasColor(config.CaptionEffect) ? ColorTranslator.FromHtml(CaptionEffect.DefaultColor(config.CaptionEffect)) : Color.White);
                CaptionEffectRenderer.Draw(target, rectangle, text, style, aaMode, CaptionEffect.Build(config.CaptionEffect, accent, config.CaptionEffectStrength / 100F, style, config.CaptionEffectImage));
                return;
            }
            DrawOutlined(target, rectangle, text, style, aaMode);
        }

        // Outlined text with per-pixel alpha at an exact rectangle (no caption indent).
        internal static void DrawOutlined(Graphics target, Rectangle rectangle, string text, CaptionFont style, bool aaMode)
        {
            if (rectangle.Width < 1 || rectangle.Height < 1 || string.IsNullOrEmpty(text)) return;
            int radius = Math.Max(0, Math.Min(12, style.BorderSize));
            int width = rectangle.Width + radius * 2, height = rectangle.Height + radius * 2;
            using (var mask = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var result = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var font = style.CreateFont())
            using (var format = (StringFormat)StringFormat.GenericTypographic.Clone())
            {
                mask.SetResolution(target.DpiX, target.DpiY);
                result.SetResolution(target.DpiX, target.DpiY);
                if (style.IsDirectionVertical) format.FormatFlags |= StringFormatFlags.DirectionVertical | StringFormatFlags.DirectionRightToLeft;
                format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                using (var graphics = Graphics.FromImage(mask))
                {
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    graphics.TextContrast = 4;
                    if (aaMode) graphics.DrawString(text, font, Brushes.White, new PointF(radius, radius), format);
                    else graphics.DrawString(text, font, Brushes.White, new RectangleF(radius, radius, rectangle.Width, rectangle.Height), format);
                }
                var area = new Rectangle(0, 0, width, height);
                BitmapData input = mask.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] pixels = new byte[input.Stride * height];
                int stride = input.Stride;
                try { Marshal.Copy(input.Scan0, pixels, 0, pixels.Length); }
                finally { mask.UnlockBits(input); }
                byte[] output = new byte[pixels.Length];
                // Restrict dilation to the ink's bounding rectangle for long-lived overlays.
                int left = width, right = -1, top = height, bottom = -1;
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                    if (pixels[y * stride + x * 4 + 3] != 0) { left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y); }
                left = Math.Max(0, left - radius); right = Math.Min(width - 1, right + radius);
                top = Math.Max(0, top - radius); bottom = Math.Min(height - 1, bottom + radius);
                float lineHeight = Math.Max(1, font.GetHeight(target));
                // Outline coverage falls off over the last half pixel of the radius,
                // so the outer edge of the border is antialiased instead of stepped.
                int span = radius * 2 + 1;
                var weights = new float[span * span];
                for (int dy = -radius; dy <= radius; dy++)
                    for (int dx = -radius; dx <= radius; dx++)
                        weights[(dy + radius) * span + dx + radius] = Math.Max(0, Math.Min(1, radius + 0.5F - (float)Math.Sqrt(dx * dx + dy * dy)));
                Color high = style.Colors.HighForeColor, low = style.Colors.LowForeColor, border = style.Colors.BorderColor;
                for (int y = top; y <= bottom; y++)
                {
                    float mix = Math.Max(0, Math.Min(1, (y - radius) / lineHeight));
                    int r = (int)(high.R + (low.R - high.R) * mix), gr = (int)(high.G + (low.G - high.G) * mix), b = (int)(high.B + (low.B - high.B) * mix);
                    for (int x = left; x <= right; x++)
                    {
                        int index = y * stride + x * 4, coverage = pixels[index + 3], outline = coverage;
                        if (coverage < 255 && radius > 0)
                            for (int dy = -radius; dy <= radius; dy++)
                            {
                                int yy = y + dy;
                                if (yy < 0 || yy >= height) continue;
                                for (int dx = -radius; dx <= radius; dx++)
                                {
                                    int xx = x + dx;
                                    float weight = weights[(dy + radius) * span + dx + radius];
                                    if (xx >= 0 && xx < width && weight > 0)
                                        outline = Math.Max(outline, (int)(pixels[yy * stride + xx * 4 + 3] * weight));
                                }
                            }
                        float foregroundAlpha = coverage / 255F;
                        float borderAlpha = outline / 255F * border.A / 255F * (1 - foregroundAlpha);
                        float alpha = foregroundAlpha + borderAlpha;
                        if (alpha == 0) continue;
                        output[index] = (byte)Math.Min(255, (b * foregroundAlpha + border.B * borderAlpha) / alpha);
                        output[index + 1] = (byte)Math.Min(255, (gr * foregroundAlpha + border.G * borderAlpha) / alpha);
                        output[index + 2] = (byte)Math.Min(255, (r * foregroundAlpha + border.R * borderAlpha) / alpha);
                        output[index + 3] = (byte)Math.Min(255, alpha * 255);
                    }
                }
                BitmapData data = result.LockBits(area, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try { Marshal.Copy(output, 0, data.Scan0, output.Length); }
                finally { result.UnlockBits(data); }
                target.DrawImageUnscaled(result, rectangle.X - radius, rectangle.Y - radius);
            }
        }
    }
}
