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
                ClientSize = new Size(440, 400), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
            using (var listFont = new Font(UserConfig.CommentFontName ?? "Meiryo", 10F))
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
                string family = listFont.FontFamily.Name;
                var selectFont = new Button { Text = "本文フォント：" + family, Left = 22, Top = 202, Width = 396, Height = 36 };
                selectFont.Click += delegate {
                    using (var chooser = new FontDialog { Font = new Font(family, (float)size.Value, FontStyle.Regular, GraphicsUnit.Pixel), FontMustExist = true, ShowEffects = false })
                        if (chooser.ShowDialog(dialog) == DialogResult.OK) { family = chooser.Font.FontFamily.Name; size.Value = Math.Max(10, Math.Min(40, (decimal)(chooser.Font.SizeInPoints * 96F / 72F))); selectFont.Text = "本文フォント：" + family; }
                };
                dialog.Controls.Add(selectFont);
                CaptionFont pendingCaption = new CaptionFont(UserConfig.CaptionFont);
                var caption = new Button { Text = "テロップのフォント・サイズ…", Left = 22, Top = 250, Width = 396, Height = 36 };
                caption.Click += delegate {
                    using (var chooser = new FontDialog { Font = pendingCaption.CreateFont(), FontMustExist = true, ShowEffects = false })
                        if (chooser.ShowDialog(dialog) == DialogResult.OK) {
                            pendingCaption.Name = chooser.Font.FontFamily.Name; pendingCaption.Size = chooser.Font.SizeInPoints;
                            pendingCaption.Bold = chooser.Font.Bold; pendingCaption.Italic = chooser.Font.Italic;
                            caption.Text = pendingCaption.Name + " / " + pendingCaption.Size.ToString("0.#") + " pt";
                        }
                };
                dialog.Controls.Add(caption);
                var title = new CheckBox { Text = "テロップ左上にスレッド名を表示", Checked = !UserConfig.HideCaptionTitle, Left = 22, Top = 302, AutoSize = true };
                dialog.Controls.Add(title);
                var ok = new Button { Text = "適用", DialogResult = DialogResult.OK, Left = 238, Top = 352, Width = 86 };
                var cancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Left = 332, Top = 352, Width = 86 };
                dialog.Controls.Add(ok); dialog.Controls.Add(cancel); dialog.AcceptButton = ok; dialog.CancelButton = cancel;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                UserConfig.CommentLayout = layout.SelectedIndex == 1 ? "two" : "one";
                UserConfig.CommentShowNumber = checks[0].Checked; UserConfig.CommentShowName = checks[1].Checked;
                UserConfig.CommentShowDate = checks[2].Checked; UserConfig.CommentShowTime = checks[3].Checked; UserConfig.CommentShowID = checks[4].Checked;
                UserConfig.CommentFontName = family; UserConfig.CommentFontSize = (int)size.Value;
                UserConfig.CaptionFont = pendingCaption; UserConfig.CaptionFont.ClearBrushCache();
                UserConfig.HideCaptionTitle = !title.Checked;
                ApplyReaderAppearance(); FormCaption.Instance.Refresh();
            }
        }
    }
}
