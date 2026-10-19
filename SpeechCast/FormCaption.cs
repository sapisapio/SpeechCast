using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormCaption : Form
    {
        private string titleText;
        public FormCaption()
        {
            InitializeComponent();
            // Same icon as the application (shown in the taskbar and OBS window list).
            Icon = AppIcon.Value;
            this.titleText = "SpeechcastSP";
        }

        public void setTitle(string title){
            string next = string.IsNullOrEmpty(title) ? "SpeechcastSP" : title;
            if (titleText == next) return;
            this.titleText = next;
            this.Text = "Speech Cast SP — " + this.titleText;
            Refresh();
        }
        public static FormCaption Instance;

        private bool borderVisible = false;
        public bool BorderVisible
        {
            set
            {
                borderVisible = value;
                this.Refresh();
            }
            get
            {
                return this.borderVisible;
            }
        }
        private void PaintCaptionContent(object sender, PaintEventArgs e)
        {
            if (borderVisible)            
            {
                //枠描画
                Rectangle rect = this.ClientRectangle;

                rect.Inflate(new Size(-2, -2));
                e.Graphics.DrawRectangle(new Pen(Brushes.YellowGreen, 4), rect);
            }


            float fontSize = 0.0f;
            if (CaptionText != "")
            {
                //字幕描画   

                Rectangle captionRect = this.ClientRectangle;

                int offsetY = borderVisible || !FormMain.UserConfig.HideCaptionTitle ? 25 : 7;
                captionRect.Y = offsetY;
                captionRect.Height -= offsetY;

                captionRect.Inflate(-10, -10);

                CaptionFont captionFont = FormMain.UserConfig.CaptionFont;

                if (isAAMode_)
                {
                    const float minSize = 6.0f;

                    captionFont = new CaptionFont(FormMain.UserConfig.AACaptionFont);
                    captionFont.Bold = true;
                    float highSize = captionFont.Size;
                    float lowSize = highSize / 2;

                    while (true)
                    {
                        captionFont.Size = highSize;
                        if (IsCaptionInRect(captionFont))
                        {
                            break;
                        }

                        captionFont.Size = lowSize;
                        if (IsCaptionInRect(captionFont))
                        {
                            float size = (highSize + lowSize) / 2;
                            captionFont.Size = size;

                            if (IsCaptionInRect(captionFont))
                            {
                                lowSize = size;
                            }
                            else
                            {
                                highSize = size;
                            }
                        }
                        else
                        {
                            highSize = lowSize;
                            lowSize = highSize / 2;
                        }

                        if (lowSize < minSize)
                        {
                            lowSize = minSize;
                        }

                        if (System.Math.Abs(highSize - lowSize) < 1.0f)
                        {
                            captionFont.Size = lowSize;
                            break;
                        }
                    }
                }
                fontSize = captionFont.Size;
                DrawCaption(e.Graphics, captionRect, CaptionText, captionFont, isAAMode_);

            }
            DrawTitle(e);
        }

        bool IsCaptionInRect(CaptionFont captionFont)
        {

            SizeF size;
            using (var graphics = this.CreateGraphics())
            using (var font = captionFont.CreateFont()) size = graphics.MeasureString(CaptionText, font, 10000);
            
            if (FormMain.UserConfig.ReduceFontSizeByWindowWidth && size.Width > (float)Width - 20.0f)
            {
                return false;
            }

            if (FormMain.UserConfig.ReduceFontSizeByWindowHeight && size.Height > (float)Height - 27.0f)
            {
                return false;
            }

            return true;


        }

        bool isAAMode_ = false;

        public bool IsAAMode
        {
            set
            {
                if (isAAMode_ != value)
                {
                    isAAMode_ = value;
                    if (isAAMode_)
                    {
                        //this.BackColor = FormMain.UserConfig.AACaptionFont.Colors.TransparentColor;
                    }
                    else
                    {
                        //this.BackColor = FormMain.UserConfig.AACaptionFont.Colors.TransparentColor;
                    }
                    //this.BackColor = System.Drawing.Color.FromKnownColor(KnownColor.Control);
                    //this.TransparencyKey = this.BackColor;
                }
            }

            get
            {
                return isAAMode_;
            }
        }

        //static Brush captionBrush = null;


        public static void DrawTitle(PaintEventArgs e)
        {
            var caption = FormCaption.Instance;
            if (!caption.borderVisible && FormMain.UserConfig.HideCaptionTitle) { caption.drawRect = Rectangle.Empty; return; }
            using (var font = new Font("Yu Gothic UI", 9F, FontStyle.Regular))
            using (var format = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            using (var background = new SolidBrush(Color.FromArgb(235, 31, 41, 55)))
            using (var foreground = new SolidBrush(Color.FromArgb(238, 242, 247)))
            {
                string label = caption.titleText;
                int available = Math.Max(1, caption.ClientSize.Width - 8);
                int width = Math.Min(available, (int)Math.Ceiling(e.Graphics.MeasureString(label, font, new SizeF(100000, 1000), format).Width) + 8);
                caption.drawRect = new Rectangle(4, 3, width, (int)Math.Ceiling(font.GetHeight(e.Graphics)) + 4);
                e.Graphics.FillRectangle(background, caption.drawRect);
                var textRect = caption.drawRect;
                textRect.Inflate(-4, 0);
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.DrawString(label, font, foreground, textRect, format);
            }
        }

        public static void DrawCaption(Graphics g, Rectangle captionRect, string captionText, CaptionFont captionFont, bool isAAMode)
        {
            CaptionRasterizer.Draw(g, captionRect, captionText, captionFont, isAAMode);
        }


        public Rectangle drawRect = new Rectangle(0, 0, 60, 14);

        private string captionText = "";
        public string CaptionText
        {
            get
            {
                return this.captionText;
            }
            set
            {
                if (captionText != value)
                {
                    captionText = value;
                    this.Refresh();
                }
            }

        }

#if true
        #region WindowProc

        const int WM_NCHITTEST = 0x84;
        const int HTCAPTION = 2;
        const int HTTOP = 12;
        const int HTTOPLEFT = 13;
        const int HTTOPRIGHT = 14;
        const int HTLEFT = 10;
        const int HTRIGHT = 11;
        const int HTBOTTOM = 15;
        const int HTBOTTOMLEFT = 16;
        const int HTBOTTOMRIGHT = 17;
        const int HTCLIENT = 1;
        const int HTNOWHERE = 0;
        const int HTTRANSPARENT = (-1);     
        //HTBORDER          18        可変枠を持たない境界線上にある
        //HTBOTTOM          15        可変枠の下辺境界線上にある
        //HTBOTTOMLEFT      16        同、左下隅にある
        //HTBOTTOMRIGHT     17        同、右下隅にある
        //HTCAPTION          2        キャプションバー上にある
        //HTCLIENT           1        クライアント領域内にある
        //HTERROR          (-2)       デスクトップ上にあり、警告音を鳴らす
        //HTGROWBOX          4        = HTSIZE
        //HTHSCROOL          6        水平スクロールバーないある
        //HTLEFT            10        可変枠の左辺境界線上にある
        //HTMENU             5        メニューバー内にある
        //HTMINBUTTON        8        アイコン化ボタン上にある
        //HTMAXBUTTON        9        最大化ボタン上にある
        //HTNOWHERE          0        デスクトップ上にある
        //HTREDUCE                    = HTMINBUTTON
        //HTRIGHT           11        可変枠の右辺境界線上にある
        //HTSIZE             4        サイズボックス内にある
        //HTSYSMENU          3        システムメニュー内にある
        //HTTOP             12        可変枠の上辺境界線上にある
        //HTTOPLEFT         13        可変枠の左上隅にある
        //HTTOPRIGHT        14        可変枠の右上隅にある
        //HTTRANSPARENT    (-1)       同じスレッドの別のウィンドウの下にある
        //HTVSCROLL          7        垂直スクロールバー内にある
        //HTZOOM                      = HTMAXBUTTON
        const int HitBorderSize = 8; //実際の表示より大きめに

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCHITTEST:
                    {
                        short x = (short)(((uint)m.LParam) & 0xffff);
                        short y = (short)(((uint)m.LParam) >> 16);
                        Point pt = new Point(x, y);
                        pt = this.PointToClient(pt);

                        if (drawRect.Contains(pt))
                        {
                            m.Result = (IntPtr)HTCAPTION;
                            break;;
                        }

                        int hit = HTTRANSPARENT;
                        int hitY = HTTRANSPARENT;

                        if (pt.Y <= HitBorderSize)
                        {
                            hitY = HTTOP;
                        }
                        else if (pt.Y >= Height - HitBorderSize)
                        {
                            hitY= HTBOTTOM;
                        }

                        if (pt.X <= HitBorderSize)
                        {
                            if (hitY == HTBOTTOM)
                            {
                                hit = HTBOTTOMLEFT;
                            }
                            else if (hitY == HTTOP)
                            {
                                hit = HTTOPLEFT;
                            }
                            else
                            {
                                hit = HTLEFT;
                            }
                        }
                        else if (pt.X >= Width - HitBorderSize)
                        {
                            if (hitY == HTBOTTOM)
                            {
                                hit = HTBOTTOMRIGHT;
                            }
                            else if (hitY == HTTOP)
                            {
                                hit = HTTOPRIGHT;
                            }
                            else
                            {
                                hit = HTRIGHT;
                            }
                        }
                        else
                        {
                            hit = hitY;
                        }

                        m.Result = (IntPtr)hit;
                    }
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }
        #endregion
#endif
        private void FormCaption_SizeChanged(object sender, EventArgs e)
        {
            FormMain.UserConfig.AACaptionFont.ClearBrushCache();
            FormMain.UserConfig.CaptionFont.ClearBrushCache();
        }

        private void FormCaption_Resize(object sender, EventArgs e)
        {
            Refresh();
        }

        private void FormCaption_Move(object sender, EventArgs e)
        {
            Refresh();
        }

        private void FormCaption_BackColorChanged(object sender, EventArgs e)
        {
            FormMain.Instance.AddLog("字幕塗りつぶし調査用：" + this.BackColor);
            //FormMain.Instance.AddLog("字幕塗りつぶし調査用：" + this.TransparencyKey);
        }


    }
}
