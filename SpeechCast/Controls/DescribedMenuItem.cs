// A menu item with a bold title and a smaller grey explanation underneath, as in model pickers:
// used where a choice needs a sentence of context (the jpnkn reading modes), 2026-10-20. GPLv2.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpeechCastMe.Controls
{
    internal sealed class DescribedMenuItem : ToolStripMenuItem
    {
        private const int TextLeft = 32, Gap = 2, PadY = 5, PadRight = 18;
        public string Description { get; set; }

        public DescribedMenuItem(string title, string description, EventHandler click) : base(title, null, click)
        {
            Description = description;
        }

        private Font TitleFont { get { return new Font(Font, FontStyle.Bold); } }
        private Font NoteFont { get { return new Font(Font.FontFamily, Math.Max(7F, Font.Size - 1F)); } }

        public override Size GetPreferredSize(Size constrainingSize)
        {
            using (var title = TitleFont)
            using (var note = NoteFont)
            {
                var t = TextRenderer.MeasureText(Text, title);
                var d = TextRenderer.MeasureText(Description ?? "", note);
                return new Size(TextLeft + Math.Max(t.Width, d.Width) + PadRight, PadY * 2 + t.Height + Gap + d.Height);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var renderer = Owner != null ? Owner.Renderer : null;
            if (renderer == null) { base.OnPaint(e); return; }
            renderer.DrawMenuItemBackground(new ToolStripItemRenderEventArgs(e.Graphics, this));
            var palette = renderer is ReaderToolbarRenderer ? ((ReaderToolbarRenderer)renderer).Palette : null;
            Color text = palette != null ? palette.Text : SystemColors.MenuText;
            Color muted = palette != null ? palette.MutedText : SystemColors.GrayText;
            if (!Enabled) text = muted;
            using (var title = TitleFont)
            using (var note = NoteFont)
            {
                var t = TextRenderer.MeasureText(Text, title);
                if (Checked)
                    renderer.DrawItemCheck(new ToolStripItemImageRenderEventArgs(e.Graphics, this, new Rectangle(8, PadY + (t.Height - 16) / 2, 16, 16)));
                TextRenderer.DrawText(e.Graphics, Text, title, new Point(TextLeft, PadY), text);
                TextRenderer.DrawText(e.Graphics, Description ?? "", note, new Point(TextLeft, PadY + t.Height + Gap), muted);
            }
        }
    }
}
