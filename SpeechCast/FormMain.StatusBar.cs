﻿// A slim status bar: plain text instead of sunken fields, and the telop input toggle
// as a toolbar-style button, 2026-10-19. GPLv2.
using System.Drawing;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        private ToolStripButton telopInputButton;

        private void InitializeStatusBar()
        {
            statusStrip1.SizingGrip = false;
            statusStrip1.Font = new Font("Yu Gothic UI", 9F);
            statusStrip1.Padding = new Padding(6, 1, 6, 1);
            foreach (ToolStripItem item in statusStrip1.Items)
            {
                var label = item as ToolStripStatusLabel;
                if (label == null) continue;
                label.BorderSides = ToolStripStatusLabelBorderSides.None;
                label.Margin = new Padding(0, 2, 14, 2);
                label.Padding = Padding.Empty;
            }
            toolStripStatusLabelResNumber.AutoSize = true;
            toolStripStatusLabelCommunication.Margin = new Padding(0, 2, 6, 2);

            // The old text link stays only as the target of existing code; the button replaces it.
            toolStripStatusLabelDefaultCaptionButton.Available = false;
            telopInputButton = new ToolStripButton { Name = "telopInputButton" };
            SetIconButton(telopInputButton, "font", "自由テロップの入力欄を開く / 閉じる");
            SetLabelledIcon(telopInputButton, "テロップ入力");
            telopInputButton.Margin = new Padding(0, 1, 0, 1);
            telopInputButton.Click += delegate { openResCaptionForm(); };
            toolStripButtonOpenResCaptionForm.CheckedChanged += delegate { telopInputButton.Checked = toolStripButtonOpenResCaptionForm.Checked; };
            statusStrip1.Items.Add(telopInputButton);
        }
    }
}
