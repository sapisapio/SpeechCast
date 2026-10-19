// A number field for the timer settings: type the value directly, or step it by one
// with the ▲ ▼ buttons underneath, the arrow keys or the mouse wheel, 2026-10-19. GPLv2.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpeechCastMe.Controls
{
    internal sealed class SpinField : UserControl
    {
        private readonly TextBox box;
        private int value;

        public int Minimum { get; set; }
        public int Maximum { get; set; }

        public SpinField(int minimum, int maximum, int initial)
        {
            Minimum = minimum;
            Maximum = maximum;
            Size = new Size(64, 62);
            box = new TextBox { Font = new Font("Segoe UI", 16F, FontStyle.Bold), TextAlign = HorizontalAlignment.Center,
                Left = 0, Top = 0, Width = 64, MaxLength = 3 };
            var up = Arrow("▲", 0);
            var down = Arrow("▼", 32);
            up.Click += delegate { Step(1); };
            down.Click += delegate { Step(-1); };
            box.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Up) { Step(1); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Down) { Step(-1); e.SuppressKeyPress = true; }
            };
            box.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            box.Leave += delegate { Commit(); };
            box.MouseWheel += (s, e) => Step(e.Delta > 0 ? 1 : -1);
            Controls.Add(box);
            Controls.Add(up);
            Controls.Add(down);
            Value = initial;
        }

        private Button Arrow(string text, int left)
        {
            return new Button { Text = text, Left = left, Top = box.Bottom + 2, Width = 32, Height = 24, TabStop = false,
                Font = new Font("Segoe UI", 7F), FlatStyle = FlatStyle.System };
        }

        public int Value
        {
            get { Commit(); return value; }
            set { this.value = Math.Max(Minimum, Math.Min(Maximum, value)); box.Text = this.value.ToString("00"); }
        }

        private void Step(int delta) { Commit(); Value = value + delta; box.SelectAll(); }

        // Typed text takes effect when it is a number; anything else restores the last value.
        private void Commit()
        {
            int typed;
            if (int.TryParse(box.Text, out typed)) value = Math.Max(Minimum, Math.Min(Maximum, typed));
            box.Text = value.ToString("00");
        }
    }
}
