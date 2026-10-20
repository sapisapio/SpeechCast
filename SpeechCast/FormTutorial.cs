// The in-app guide: one screenshot, a heading and a few lines per page, paged with ← n/N →, 2026-10-20. GPLv2.
// Pages are the files in the Tutorial folder next to the exe: 01.png + 01.txt, 02.png + 02.txt, ...
// The first line of a .txt is the heading, the rest is the description.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SpeechCastMe
{
    internal sealed class FormTutorial : Form
    {
        private sealed class Page { public string Image, Title, Body; }

        private readonly List<Page> pages = new List<Page>();
        private readonly PictureBox picture = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(58, 62, 70) };
        private readonly Label title = new Label { Dock = DockStyle.Top, Height = 40, Font = new Font("Yu Gothic UI", 15F, FontStyle.Bold), Padding = new Padding(20, 10, 20, 0) };
        private readonly Label body = new Label { Dock = DockStyle.Fill, Font = new Font("Yu Gothic UI", 10.5F), Padding = new Padding(22, 4, 22, 0) };
        private readonly Button prev = new Button { Text = "←", Width = 64, Height = 32 };
        private readonly Button next = new Button { Text = "→", Width = 64, Height = 32 };
        private readonly Label count = new Label { AutoSize = false, Width = 80, Height = 32, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Yu Gothic UI", 10.5F) };
        private int index;

        internal static string Folder { get { return Path.Combine(Application.StartupPath, "Tutorial"); } }

        internal FormTutorial()
        {
            Text = "Speech Cast SP の使い方";
            ClientSize = new Size(820, 640);
            MinimumSize = new Size(560, 460);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            KeyPreview = true;
            ShowInTaskbar = false;

            var text = new Panel { Dock = DockStyle.Bottom, Height = 150 };
            text.Controls.Add(body);
            text.Controls.Add(title);
            var nav = new Panel { Dock = DockStyle.Bottom, Height = 52 };
            nav.Controls.AddRange(new Control[] { prev, count, next });
            nav.Resize += delegate {
                int x = (nav.Width - prev.Width - count.Width - next.Width) / 2, y = (nav.Height - 32) / 2;
                prev.Location = new Point(x, y); count.Location = new Point(x + prev.Width, y); next.Location = new Point(x + prev.Width + count.Width, y);
            };
            Controls.Add(picture);
            Controls.Add(text);
            Controls.Add(nav);

            prev.Click += delegate { Show(index - 1); };
            next.Click += delegate { if (index == pages.Count - 1) Close(); else Show(index + 1); };
            KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Left) { Show(index - 1); e.Handled = true; }
                else if (e.KeyCode == Keys.Right) { Show(index + 1); e.Handled = true; }
                else if (e.KeyCode == Keys.Escape) Close();
            };
            Load();
            Show(0);
        }

        // Arrow keys would move focus between the buttons; page instead.
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Right) { Show(index + (keyData == Keys.Left ? -1 : 1)); return true; }
            return base.ProcessDialogKey(keyData);
        }

        private new void Load()
        {
            if (!Directory.Exists(Folder)) return;
            foreach (string file in Directory.GetFiles(Folder, "*.txt").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                string[] lines = File.ReadAllLines(file);
                string image = Path.ChangeExtension(file, ".png");
                pages.Add(new Page {
                    Title = lines.Length > 0 ? lines[0] : "",
                    Body = string.Join(Environment.NewLine, lines.Skip(1)).Trim(),
                    Image = File.Exists(image) ? image : null,
                });
            }
        }

        private void Show(int page)
        {
            if (pages.Count == 0)
            {
                title.Text = "使い方のページがありません";
                body.Text = Folder + " に 01.png / 01.txt … を置いてください。";
                prev.Enabled = next.Enabled = false;
                return;
            }
            index = Math.Max(0, Math.Min(pages.Count - 1, page));
            var p = pages[index];
            var old = picture.Image;
            picture.Image = null;
            if (p.Image != null)
                using (var stream = new MemoryStream(File.ReadAllBytes(p.Image))) picture.Image = new Bitmap(stream); // no file lock, so pages can be swapped while open
            if (old != null) old.Dispose();
            title.Text = p.Title;
            body.Text = p.Body;
            count.Text = (index + 1) + " / " + pages.Count;
            prev.Enabled = index > 0;
            next.Text = index == pages.Count - 1 ? "閉じる" : "→";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && picture.Image != null) picture.Image.Dispose();
            base.Dispose(disposing);
        }
    }
}
