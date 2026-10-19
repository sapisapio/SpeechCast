// The comment theme (light / dark / a chosen color) applied to the whole main window, 2026-10-19. GPLv2.
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        private WindowTheme windowTheme = WindowTheme.Light;
        private bool windowThemeApplied;
        // Toolbar icons are bitmaps drawn in the theme's ink, so they are redrawn on a theme change.
        private readonly Dictionary<ToolStripItem, string> themedIcons = new Dictionary<ToolStripItem, string>();
        private readonly HashSet<ToolStripDropDownItem> roundedMenus = new HashSet<ToolStripDropDownItem>();
        private readonly Dictionary<ToolStripTextBox, BorderStyle> textBorders = new Dictionary<ToolStripTextBox, BorderStyle>();
        private readonly Dictionary<ToolStripComboBox, Controls.ComboChrome> comboChromes = new Dictionary<ToolStripComboBox, Controls.ComboChrome>();

        private Bitmap CreateThemedIcon(ToolStripItem item, string icon)
        {
            themedIcons[item] = icon;
            return ToolbarIcons.Create(icon, ToolbarIconSize, windowTheme.Ink);
        }

        private void ApplyWindowTheme()
        {
            var theme = UserConfig == null ? WindowTheme.Light : WindowTheme.For(UserConfig.CommentTheme, UserConfig.CommentBackground);
            if (windowThemeApplied && theme == windowTheme) return;
            windowTheme = theme;
            windowThemeApplied = true;
            toolbarRenderer.Palette = theme;

            SuspendLayout();
            BackColor = theme.Window;
            foreach (var strip in ThemedStrips(this))
            {
                strip.BackColor = theme.Window;
                strip.ForeColor = theme.Text;
                foreach (ToolStripItem item in strip.Items) ApplyItemTheme(item, theme);
            }
            foreach (var pair in themedIcons)
            {
                var old = pair.Key.Image;
                pair.Key.Image = ToolbarIcons.Create(pair.Value, ToolbarIconSize, theme.Ink);
                if (old != null) old.Dispose();
            }
            foreach (Control field in new Control[] { listViewResponses, textBoxDefaultCaption })
            {
                field.BackColor = theme.Field;
                field.ForeColor = theme.Text;
                WindowTheme.ApplyScrollBars(field, theme.IsDark);
            }
            ResumeLayout();
            WindowTheme.ApplyTitleBar(this, theme.IsDark);
            Invalidate(true);
        }

        private void ApplyItemTheme(ToolStripItem item, WindowTheme theme)
        {
            var text = item as ToolStripTextBox;
            if (text != null)
            {
                // The tool strip draws a text box's hover and focus edge in a fixed light color.
                if (!textBorders.ContainsKey(text)) textBorders[text] = text.BorderStyle;
                text.BorderStyle = theme.IsDark ? BorderStyle.None : textBorders[text];
                text.BackColor = theme.Field;
                text.ForeColor = theme.Text;
                return;
            }
            var combo = item as ToolStripComboBox;
            if (combo != null)
            {
                // Flat in every theme; on dark ones its white edge and button are painted over.
                combo.FlatStyle = FlatStyle.Flat;
                if (!comboChromes.ContainsKey(combo)) comboChromes[combo] = new Controls.ComboChrome(combo.ComboBox);
                comboChromes[combo].Palette = theme.IsDark ? theme : null;
                combo.BackColor = theme.Field;
                combo.ForeColor = theme.Text;
                return;
            }
            var host = item as ToolStripControlHost;
            if (host != null)
            {
                host.Control.BackColor = theme.Window;
                var slider = host.Control as Controls.ThemedTrackBar;
                if (slider != null) slider.Palette = theme; // the same drawn slider in every theme
                return;
            }
            var dropDown = item as ToolStripDropDownItem;
            if (dropDown == null) return;
            if (!roundedMenus.Contains(dropDown))
            {
                roundedMenus.Add(dropDown);
                dropDown.DropDown.Opened += delegate { WindowTheme.ApplyRoundCorners(dropDown.DropDown); };
            }
            foreach (ToolStripItem child in dropDown.DropDownItems)
            {
                if (child is ToolStripMenuItem) child.Padding = new Padding(0, 3, 0, 3);
                ApplyItemTheme(child, theme);
            }
        }

        private static IEnumerable<ToolStrip> ThemedStrips(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                var strip = child as ToolStrip;
                if (strip != null) yield return strip;
                foreach (var inner in ThemedStrips(child)) yield return inner;
            }
        }

        protected override void OnHandleCreated(System.EventArgs e)
        {
            base.OnHandleCreated(e);
            WindowTheme.ApplyTitleBar(this, windowTheme.IsDark);
        }
    }
}
