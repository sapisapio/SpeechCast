// Speech Cast SP UI preferences, 2026-10-18. GPLv2.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        private void ShowReaderAppearance()
        {
            if (UserConfig == null) return;
            using (var dialog = new Form { Text = "表示項目・フォント", Font = Font, StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(440, 466), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
            {
                var layout = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Left = 22, Top = 42, Width = 396 };
                layout.Items.AddRange(new object[] { "1行：表示項目と本文を横並び", "2行：表示項目の下に本文" });
                layout.SelectedIndex = UserConfig.CommentLayout == "two" || UserConfig.CommentLayout == "named" ? 1 : 0;
                dialog.Controls.Add(new Label { Text = "コメントの並べ方", Left = 22, Top = 16, AutoSize = true });
                dialog.Controls.Add(layout);
                var fields = new FlowLayoutPanel { Left = 22, Top = 89, Width = 396, Height = 66 };
                string[] labels = { "レス番号", "名前", "日付", "時間", "ID" };
                bool[] values = { UserConfig.CommentShowNumber, UserConfig.CommentShowName, UserConfig.CommentShowDate, UserConfig.CommentShowTime, UserConfig.CommentShowID };
                var checks = new CheckBox[5];
                for (int i = 0; i < 5; i++) { checks[i] = new CheckBox { Text = labels[i], Checked = values[i], AutoSize = true, Margin = new Padding(0, 3, 16, 3) }; fields.Controls.Add(checks[i]); }
                dialog.Controls.Add(fields);
                dialog.Controls.Add(new Label { Text = "本文の文字サイズ（px）", Left = 22, Top = 164, AutoSize = true });
                var size = new NumericUpDown { Left = 280, Top = 160, Width = 76, Minimum = 10, Maximum = 40, Value = Math.Max(10, Math.Min(40, UserConfig.CommentFontSize)) };
                dialog.Controls.Add(size);
                // Keep the stored name: a GDI+ round trip turns the bundled font into a fallback.
                string family = string.IsNullOrEmpty(UserConfig.CommentFontName) ? "MS PGothic" : UserConfig.CommentFontName;
                Func<string, string> label = name => "本文フォント：" + (name == BundledFont.FamilyName ? "源暎エムゴ v2 Black（内蔵）" : name);
                var selectFont = new Button { Text = label(family), Left = 22, Top = 202, Width = 290, Height = 36 };
                selectFont.Click += delegate {
                    using (var chooser = new FontDialog { Font = new Font(family, (float)size.Value, FontStyle.Regular, GraphicsUnit.Pixel), FontMustExist = true, ShowEffects = false })
                        if (ShowFontDialog(chooser, dialog)) { family = chooser.Font.FontFamily.Name; size.Value = Math.Max(10, Math.Min(40, (decimal)(chooser.Font.SizeInPoints * 96F / 72F))); selectFont.Text = label(family); }
                };
                dialog.Controls.Add(selectFont);
                var bundledBody = new Button { Text = "内蔵フォント", Left = 318, Top = 202, Width = 100, Height = 36 };
                bundledBody.Click += delegate { family = BundledFont.FamilyName; selectFont.Text = label(family); };
                dialog.Controls.Add(bundledBody);
                // Stream comments window: its own font; size 0 follows the comment size.
                string streamName = string.IsNullOrEmpty(UserConfig.StreamFontName) ? BundledFont.FamilyName : UserConfig.StreamFontName;
                int streamSize = UserConfig.StreamFontSize;
                Func<string> streamLabel = () => "配信コメント：" + (streamName == BundledFont.FamilyName ? "源暎エムゴ v2 Black（内蔵）" : streamName)
                    + " / " + (streamSize > 0 ? streamSize + " px" : "本文と同じ大きさ");
                var streamFont = new Button { Text = streamLabel(), Left = 22, Top = 250, Width = 290, Height = 36 };
                streamFont.Click += delegate {
                    int px = streamSize > 0 ? streamSize : (int)size.Value;
                    using (var preview = streamName == BundledFont.FamilyName ? new Font(BundledFont.Family ?? FontFamily.GenericSansSerif, px, GraphicsUnit.Pixel) : new Font(streamName, px, GraphicsUnit.Pixel))
                    using (var chooser = new FontDialog { Font = preview, FontMustExist = true, ShowEffects = false })
                        if (ShowFontDialog(chooser, dialog)) { streamName = chooser.Font.FontFamily.Name; streamSize = Math.Max(10, Math.Min(80, (int)Math.Round(chooser.Font.SizeInPoints * 96F / 72F))); streamFont.Text = streamLabel(); }
                };
                dialog.Controls.Add(streamFont);
                var bundledStream = new Button { Text = "内蔵フォント", Left = 318, Top = 250, Width = 100, Height = 36 };
                bundledStream.Click += delegate { streamName = BundledFont.FamilyName; streamFont.Text = streamLabel(); };
                dialog.Controls.Add(bundledStream);
                CaptionFont pendingCaption = new CaptionFont(UserConfig.CaptionFont);
                var caption = new Button { Text = "テロップのフォント・サイズ…", Left = 22, Top = 298, Width = 290, Height = 36 };
                caption.Click += delegate {
                    using (var chooser = new FontDialog { Font = pendingCaption.CreateFont(), FontMustExist = true, ShowEffects = false })
                        if (ShowFontDialog(chooser, dialog)) {
                            pendingCaption.Name = chooser.Font.FontFamily.Name; pendingCaption.Size = chooser.Font.SizeInPoints;
                            pendingCaption.Bold = chooser.Font.Bold; pendingCaption.Italic = chooser.Font.Italic;
                            caption.Text = pendingCaption.Name + " / " + pendingCaption.Size.ToString("0.#") + " pt";
                        }
                };
                dialog.Controls.Add(caption);
                // The bundled font is not offered by the Windows font dialog.
                var bundledCaption = new Button { Text = "内蔵フォント", Left = 318, Top = 298, Width = 100, Height = 36 };
                bundledCaption.Click += delegate {
                    pendingCaption.Name = BundledFont.FamilyName; pendingCaption.Bold = false; pendingCaption.Italic = false;
                    caption.Text = "源暎エムゴ v2 Black / " + pendingCaption.Size.ToString("0.#") + " pt";
                };
                dialog.Controls.Add(bundledCaption);
                var separators = new CheckBox { Text = "レスごとに区切り線を引く", Checked = UserConfig.CommentSeparators, Left = 22, Top = 348, AutoSize = true };
                dialog.Controls.Add(separators);
                var title = new CheckBox { Text = "テロップ左上にスレッド名を表示", Checked = !UserConfig.HideCaptionTitle, Left = 22, Top = 376, AutoSize = true };
                dialog.Controls.Add(title);
                var ok = new Button { Text = "適用", DialogResult = DialogResult.OK, Left = 238, Top = 418, Width = 86 };
                var cancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Left = 332, Top = 418, Width = 86 };
                dialog.Controls.Add(ok); dialog.Controls.Add(cancel); dialog.AcceptButton = ok; dialog.CancelButton = cancel;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                UserConfig.CommentLayout = layout.SelectedIndex == 1 ? "two" : "one";
                UserConfig.CommentShowNumber = checks[0].Checked; UserConfig.CommentShowName = checks[1].Checked;
                UserConfig.CommentShowDate = checks[2].Checked; UserConfig.CommentShowTime = checks[3].Checked; UserConfig.CommentShowID = checks[4].Checked;
                UserConfig.CommentFontName = family; UserConfig.CommentFontSize = (int)size.Value;
                UserConfig.CaptionFont = pendingCaption; UserConfig.CaptionFont.ClearBrushCache();
                UserConfig.HideCaptionTitle = !title.Checked;
                UserConfig.CommentSeparators = separators.Checked;
                UserConfig.StreamFontName = streamName; UserConfig.StreamFontSize = streamSize;
                ApplyReaderAppearance(); FormCaption.Instance.Refresh();
            }
        }

        // GDI+ rejects non-TrueType selections (e.g. CFF-based .otf) when the dialog
        // converts its result; report that instead of letting the exception escape.
        private static bool ShowFontDialog(FontDialog chooser, IWin32Window owner)
        {
            try { return chooser.ShowDialog(owner) == DialogResult.OK; }
            catch (ArgumentException)
            {
                MessageBox.Show(owner, "このフォントは使用できません。TrueType形式（.ttf / .ttc）のフォントを選んでください。", "フォント", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
        }
    }
}
