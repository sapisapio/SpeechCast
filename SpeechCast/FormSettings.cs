using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace SpeechCastMe
{
    public partial class FormSettings : Form
    {
        public int VoiceVolme;
        public FormSettings()
        {
            InitializeComponent();
            System.Drawing.Text.InstalledFontCollection ifc =
                 new System.Drawing.Text.InstalledFontCollection();

            FontFamily[] ffs = ifc.Families;
            int i = 0;
            int index = 0;
            foreach (FontFamily ff in ffs)
            {
                ComboBoxFontName.Items.Add(ff.Name);
                comboBoxAAFontName.Items.Add(ff.Name);
                if (ff.Name == "MS UI Gothic")
                {
                    index = i;
                }
                i++;
            }

            ComboBoxFontName.SelectedIndex = index;
            comboBoxAAFontName.SelectedIndex = index;
        }

        public static string StringsToText(IEnumerable<string> strings)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string line in strings)
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        public void SetUserConfig(UserConfig userConfig)
        {
            try
            {
                this.NumericUpDownMaxSpeakingCharacterCount.Value = userConfig.MaxSpeakingCharacterCount;
            }
            catch //(Exception ex)
            {
            }
            try
            {
                this.numericUpDownTurboThreshold.Value = userConfig.TurboThreshold;
            }
            catch //(Exception ex)
            {
            }
            try
            {
                this.NumericUpDownAutoGettingWebInverval.Value = userConfig.AutoGettingWebInvervalMillsec;
            }
            catch //(Exception ex)
            {
            }

            try
            {
                this.numericUpDownAAModeTextLength.Value = userConfig.AAModeTextLength;
            }
            catch //(Exception ex)
            {
            }

            try
            {
                this.numericUpDownAAModeInvervalMillsec.Value = userConfig.AAModeInvervalMillsec;
            }
            catch //(Exception ex)
            {
            }

            foreach(string NewResponseSoundFilePath in userConfig.NewResponseSoundFilePathes){
                this.listBoxNewResponseSoundFilePathes.Items.Add(NewResponseSoundFilePath);
            }
            this.NumericUpDownSpeakingInvervalMillsec.Value = userConfig.SpeakingInvervalMillsec;
            this.NumericUpDownTurboSpeakingInvervalMillsec.Value = userConfig.TurboSpeakingInvervalMillsec;
            this.numericUpDownDefaultCaptinoDispInvervalMillsec.Value = userConfig.DefaultCaptinoDispInvervalMillsec;
            this.TrackBarSpeakingRate.Value = userConfig.SpeakingRate;
            this.TrackBarTurboSpeakingRate.Value = userConfig.TurboSpeakingRate;
            this.trackBarAutoScrollSpeed.Value = userConfig.AutoScrollSpeed;
            this.labelAutoScrollSpeed.Text = userConfig.AutoScrollSpeed.ToString();
            this.labelTurboSpeakingRate.Text = userConfig.TurboSpeakingRate.ToString();
            this.TextBoxNGWords.Text = StringsToText(userConfig.NGWords);
            this.TextBoxPronounciations.Text = StringsToText(userConfig.Pronounciations);
            this.textBoxAAModeConditions.Text = StringsToText(userConfig.AAModeConditions);
            this.checkBoxSpeaksResNumber.Checked = userConfig.SpeaksResNumber;
            this.checkBoxGZipCompression.Checked = userConfig.GZipCompressionEnabled;
            this.checkBoxUseProxy.Checked = userConfig.UseDefaultProxy;
            this.checkBoxShowCaptionImmediately.Checked = userConfig.ShowCaptionImmediately;
            this.CaptionFont = userConfig.CaptionFont;
            this.AACaptionFont = userConfig.AACaptionFont;
            this.checkBoxReduceFontSizeByWindowWidth.Checked = userConfig.ReduceFontSizeByWindowWidth;
            this.checkBoxReduceFontSizeByWindowHeight.Checked = userConfig.ReduceFontSizeByWindowHeight;
            this.checkBoxSpeakTextBetweenBraces.Checked = userConfig.SpeakTextBetweenBracesEvenIfAAMode;
            this.checkBoxPlaySoundNewResponse.Checked = userConfig.PlaySoundNewResponse;
            this.checkBoxPlaySoundNewResevery.Checked = userConfig.PlaySoundNewResevery;
            this.checkBoxPlaySoundSync.Checked = userConfig.PlaySoundNewResponseSync;
            //this.textBoxSoundFilePath.Text = userConfig.NewResponseSoundFilePathes;
            this.textBoxEditorFilePath.Text = userConfig.EditorFilePath;
            this.checkBoxSpeaksResNumberWhenAAMode.Checked = userConfig.SpeaksResNumberWhenAAMode;
            this.textBoxSpeakingTextWhenAAMode.Text = userConfig.SpeakingTextWhenAAMode;
            this.checkBoxHideCaptionTitle.Checked = userConfig.HideCaptionTitle;
            this.VoiceVolme = userConfig.SpeakingVolume;
            this.checkBoxDebug.Checked = userConfig.OutputDebugLog;
            this.checkBoxAutoReloadAlertCaption.Checked = userConfig.AutoReloadAlertCaption;
            this.checkBoxAutoReloadAlertVoice.Checked = userConfig.AutoReloadAlertVoice;
            this.numericUpDownAutoReloadAlertTime.Value = userConfig.AutoReloadAlertInvervalMinutes;
            this.textBoxAutoReloadAlertMessage.Text = userConfig.AutoReloadAlertMessage;
            this.checkBoxEnableMoveBottomFromBookmarks.Checked = userConfig.enableMoveBottomFromBookmarks;
            this.numericUpDownEndThreadWarningResCount.Value = userConfig.endThreadWarningResCount;
            this.textBoxOutputLogPath.Text = userConfig.OutputLogPath;
            this.textBoxOutputLogFormat.Text = userConfig.OutputLogFormat.Replace("\r\n","\n").Replace("\n","\r\n");
            this.checkOutputLog.Checked = userConfig.OutputLog;
            this.checkCopyLog.Checked = userConfig.CopyLog;
            this.comboBoxOutputEncode.SelectedIndex = userConfig.LogEncode;
            this.comboBoxLogReturnCode.SelectedIndex = userConfig.LogReturnCode;
            this.comboBoxSpiltMethod.SelectedIndex = userConfig.SpiltMethod;
            this.numericUpDownResLength.Value = userConfig.ResLength;
            this.numericUpDownLogOutputInterval.Value = userConfig.LogOutputInterval;
            this.textBoxCommandLineTargetPath.Text = userConfig.CommandLineTargetPath;
            this.textBoxCommandLineParam.Text = userConfig.CommandLineParam;

            if (userConfig.LogAppendMode)
            {
                this.comboBoxLogAppendMode.SelectedIndex = 1;
            }
            else
            {
                this.comboBoxLogAppendMode.SelectedIndex = 0;
            }

            numericUpDownViewerYoutubePlayerHeight.Value = userConfig.ViewerYoutubePlayerHeight;
            numericUpDownViewerYoutubePlayerWidth.Value = userConfig.ViewerYoutubePlayerWidth;
            UpdateUI();
        }

        private CaptionFont.CaptionColors fontColors;
        private CaptionFont.CaptionColors aAFontColors;

        private Regex regexAllSpaces = new Regex(@"^\s*$");

        public void GetUserConfig(UserConfig userConfig)
        {
            userConfig.SpeakingInvervalMillsec = System.Convert.ToInt32(this.NumericUpDownSpeakingInvervalMillsec.Value);
            userConfig.TurboSpeakingInvervalMillsec = System.Convert.ToInt32(this.NumericUpDownTurboSpeakingInvervalMillsec.Value);
            userConfig.DefaultCaptinoDispInvervalMillsec = System.Convert.ToInt32(this.numericUpDownDefaultCaptinoDispInvervalMillsec.Value);
            userConfig.SpeakingRate = this.TrackBarSpeakingRate.Value;
            userConfig.TurboSpeakingRate = this.TrackBarTurboSpeakingRate.Value;
            userConfig.AutoScrollSpeed = this.trackBarAutoScrollSpeed.Value;
            userConfig.TurboThreshold = System.Convert.ToInt32(this.numericUpDownTurboThreshold.Value);
            userConfig.MaxSpeakingCharacterCount = System.Convert.ToInt32(this.NumericUpDownMaxSpeakingCharacterCount.Value);
            userConfig.CaptionFont = this.CaptionFont;
            userConfig.AACaptionFont = this.AACaptionFont;
            userConfig.AutoGettingWebInvervalMillsec = System.Convert.ToInt32(this.NumericUpDownAutoGettingWebInverval.Value);
            userConfig.AAModeTextLength = System.Convert.ToInt32(this.numericUpDownAAModeTextLength.Value);
            userConfig.SpeaksResNumber = this.checkBoxSpeaksResNumber.Checked;
            userConfig.NGWords.Clear();
            foreach (string ngword in this.TextBoxNGWords.Lines)
            {
                if (!regexAllSpaces.IsMatch(ngword))
                {
                    userConfig.NGWords.Add(ngword);
                }
            }
            userConfig.Pronounciations.Clear();
            foreach (string pron in this.TextBoxPronounciations.Lines)
            {
                if (!regexAllSpaces.IsMatch(pron))
                {
                    userConfig.Pronounciations.Add(pron);
                }
            }
            userConfig.AAModeConditions.Clear();
            foreach (string cond in this.textBoxAAModeConditions.Lines)
            {
                if (!regexAllSpaces.IsMatch(cond))
                {
                    userConfig.AAModeConditions.Add(cond);
                }
            }
            userConfig.NewResponseSoundFilePathes.Clear();
            foreach (string NewResponseSoundFilePath in listBoxNewResponseSoundFilePathes.Items)
            {
                if (!regexAllSpaces.IsMatch(NewResponseSoundFilePath))
                {
                    userConfig.NewResponseSoundFilePathes.Add(NewResponseSoundFilePath);
                }
            }

            userConfig.UpdatePronounciations();
            userConfig.GZipCompressionEnabled = checkBoxGZipCompression.Checked;
            userConfig.UseDefaultProxy = checkBoxUseProxy.Checked;
            userConfig.ShowCaptionImmediately = checkBoxShowCaptionImmediately.Checked;
            FormMain.Instance.toolStripButtonShowCaptionImmediately.Checked = checkBoxShowCaptionImmediately.Checked;
            userConfig.ReduceFontSizeByWindowWidth = checkBoxReduceFontSizeByWindowWidth.Checked;
            userConfig.ReduceFontSizeByWindowHeight = checkBoxReduceFontSizeByWindowHeight.Checked;
            userConfig.SpeakTextBetweenBracesEvenIfAAMode = checkBoxSpeakTextBetweenBraces.Checked;
            userConfig.AAModeInvervalMillsec = System.Convert.ToInt32(this.numericUpDownAAModeInvervalMillsec.Value);
            userConfig.PlaySoundNewResponse = this.checkBoxPlaySoundNewResponse.Checked;
            //ミミック勝手に改造　レスごとに着信音鳴らす設定項目追加
            userConfig.PlaySoundNewResevery = this.checkBoxPlaySoundNewResevery.Checked;
            //ミミック勝手に改造　ここまで
            FormMain.Instance.toolStripButtonPlaySoundNewResponse.Checked = checkBoxPlaySoundNewResponse.Checked;
            userConfig.PlaySoundNewResponseSync = this.checkBoxPlaySoundSync.Checked;
            //userConfig.NewResponseSoundFilePath = this.textBoxSoundFilePath.Text;
            userConfig.EditorFilePath = this.textBoxEditorFilePath.Text;
            userConfig.SpeaksResNumberWhenAAMode = this.checkBoxSpeaksResNumberWhenAAMode.Checked;
            userConfig.SpeakingTextWhenAAMode = this.textBoxSpeakingTextWhenAAMode.Text;
            userConfig.AACaptionFont.Colors = this.aAFontColors;
            userConfig.HideCaptionTitle = this.checkBoxHideCaptionTitle.Checked;
            userConfig.OutputDebugLog = this.checkBoxDebug.Checked;
            userConfig.ViewerYoutubePlayerHeight = (int)numericUpDownViewerYoutubePlayerHeight.Value;
            userConfig.ViewerYoutubePlayerWidth = (int)numericUpDownViewerYoutubePlayerWidth.Value;
            userConfig.AutoReloadAlertCaption = this.checkBoxAutoReloadAlertCaption.Checked;
            userConfig.AutoReloadAlertVoice = this.checkBoxAutoReloadAlertVoice.Checked;
            userConfig.AutoReloadAlertInvervalMinutes = (int)this.numericUpDownAutoReloadAlertTime.Value;
            userConfig.AutoReloadAlertMessage = this.textBoxAutoReloadAlertMessage.Text;
            userConfig.endThreadWarningResCount = (int)this.numericUpDownEndThreadWarningResCount.Value;
            userConfig.enableMoveBottomFromBookmarks = this.checkBoxEnableMoveBottomFromBookmarks.Checked;
            userConfig.OutputLogPath = this.textBoxOutputLogPath.Text;
            userConfig.OutputLogFormat = this.textBoxOutputLogFormat.Text.Replace("\r\n", "\n").Replace("\n", "\r\n");
            userConfig.OutputLog = this.checkOutputLog.Checked;
            userConfig.CopyLog = this.checkCopyLog.Checked;

            userConfig.LogEncode = this.comboBoxOutputEncode.SelectedIndex;
            userConfig.LogReturnCode = this.comboBoxLogReturnCode.SelectedIndex;

            userConfig.SpiltMethod = this.comboBoxSpiltMethod.SelectedIndex;
            userConfig.ResLength = (int)this.numericUpDownResLength.Value;

            userConfig.LogOutputInterval = (int)this.numericUpDownLogOutputInterval.Value;

            userConfig.CommandLineTargetPath = this.textBoxCommandLineTargetPath.Text;
            userConfig.CommandLineParam = this.textBoxCommandLineParam.Text;


            if (this.comboBoxLogAppendMode.SelectedIndex == 1)
            {
                userConfig.LogAppendMode = true;
            }
            else
            {
                userConfig.LogAppendMode = false;
            }

        }
        

        public CaptionFont CaptionFont
        {
            get
            {
                CaptionFont captionFont = new CaptionFont();

                captionFont.Name = ComboBoxFontName.Text;

                try
                {
                    captionFont.Size = System.Convert.ToSingle(ComboBoxFontSize.Text);
                }
                catch //(Exception e)
                {
                }
                captionFont.Bold = CheckBoxFontBold.Checked;
                captionFont.Italic = CheckBoxFontItalic.Checked;
                captionFont.BorderSize = System.Convert.ToInt32(NumericUpDownFontBorderSize.Value);
                captionFont.IsDirectionVertical = checkBoxFontVertical.Checked;
                captionFont.Colors = this.fontColors;

                return captionFont;
            }

            set
            {
                int idx = ComboBoxFontName.Items.IndexOf(value.Name);

                if (idx >= 0)
                {
                    ComboBoxFontName.SelectedIndex = idx;
                }

                ComboBoxFontSize.Text = value.Size.ToString();

                idx = ComboBoxFontSize.Items.IndexOf(value.Size.ToString());
                if (idx >= 0)
                {
                    ComboBoxFontSize.SelectedIndex = idx;
                }
                CheckBoxFontBold.Checked = value.Bold;
                CheckBoxFontItalic.Checked = value.Italic;
                checkBoxFontVertical.Checked = value.IsDirectionVertical;
                NumericUpDownFontBorderSize.Value = value.BorderSize;
                fontColors = value.Colors;
            }
        }

        public CaptionFont AACaptionFont
        {
            get
            {
                CaptionFont captionFont = new CaptionFont();

                captionFont.Name = comboBoxAAFontName.Text;

                try
                {
                    captionFont.Size = System.Convert.ToSingle(comboBoxAAFontSize.Text);
                }
                catch //(Exception e)
                {
                }
                captionFont.BorderSize = System.Convert.ToInt32(numericUpDownAAFontBorderSize.Value);
                captionFont.Colors = aAFontColors;
                return captionFont;
            }

            set
            {
                int idx = comboBoxAAFontName.Items.IndexOf(value.Name);

                if (idx >= 0)
                {
                    comboBoxAAFontName.SelectedIndex = idx;
                }

                comboBoxAAFontSize.Text = value.Size.ToString();

                idx = comboBoxAAFontSize.Items.IndexOf(value.Size.ToString());
                if (idx >= 0)
                {
                    comboBoxAAFontSize.SelectedIndex = idx;
                }
                numericUpDownAAFontBorderSize.Value = value.BorderSize;
                aAFontColors = value.Colors;
            }
        }


        private void buttonOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void trackBarSpeakingRate_ValueChanged(object sender, EventArgs e)
        {
            labelSpeakingRate.Text = TrackBarSpeakingRate.Value.ToString();
        }

        private void TrackBarTurboSpeakingRate_Scroll(object sender, EventArgs e)
        {
            labelTurboSpeakingRate.Text = TrackBarTurboSpeakingRate.Value.ToString();
        }

        private void panelPreview_Paint(object sender, PaintEventArgs e)
        {
            Rectangle rect = panelPreview.ClientRectangle;

            rect.Inflate(-10, -10);

            FormCaption.DrawCaption(e.Graphics, rect, "プレビュー 123 ABC", CaptionFont, false);
        }

        private void ComboBoxFontName_SelectedIndexChanged(object sender, EventArgs e)
        {
            panelPreview.Refresh();
        }


        private void UpdateUI()
        {
            panelPlaySound.Enabled = checkBoxPlaySoundNewResponse.Checked;

            //foreach (Control control in panelPlaySound.Controls)
            //{
            //    control.Enabled = checkBoxPlaySoundNewResponse.Checked;
            //}
        }

        private void checkBoxPlaySoundNewResponse_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void buttonRefernceEditor_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Title = "エディターのファイルパスの指定";
            dialog.DefaultExt = "exe";
            dialog.Filter = "プログラム(*.exe)|*.exe";
            dialog.CheckFileExists = true;
            dialog.FileName = textBoxEditorFilePath.Text;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBoxEditorFilePath.Text = dialog.FileName;
            }

        }

        private void buttonRunEditor_GetEditorFilePath(object sender, SpeechCastMe.Controls.EventEditorArgs e)
        {
            e.EditorFilePath = textBoxEditorFilePath.Text;
        }

        private void buttonFontColor_Click(object sender, EventArgs e)
        {
            FormEditFontColors form = new FormEditFontColors();

            form.Colors = fontColors;

            if (form.ShowDialog() == DialogResult.OK)
            {
                fontColors = form.Colors;
                panelPreview.Refresh();
            }
        }

        private void buttonAAFontColor_Click(object sender, EventArgs e)
        {
            FormEditFontColors form = new FormEditFontColors();

            form.Colors = aAFontColors;

            if (form.ShowDialog() == DialogResult.OK)
            {
                aAFontColors = form.Colors;
            }
        }

        private void buttonVoiceTest_Click(object sender, EventArgs e)
        {
            FormMain.Instance.synthesizer.Volume = this.VoiceVolme;
            FormMain.Instance.synthesizer.Rate = TrackBarSpeakingRate.Value;
            FormMain.Instance.synthesizer.SpeakAsync(textTestSentence.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FormMain.Instance.synthesizer.Volume = this.VoiceVolme;
            FormMain.Instance.synthesizer.Rate = TrackBarTurboSpeakingRate.Value;
            FormMain.Instance.synthesizer.SpeakAsync(textTestSentence.Text);
        }

        //ミミック勝手に改造 レス着信音ファイルパスを複数一括指定出来るように
        private void buttonNewResponseSoundFilePathesAdd_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            //ミミック勝手に改造 MP3も選べるようにする
            dialog.Title = "サウンドファイルパスの指定";
            dialog.Filter = "サウンドファイル(*.wav,*.mp3)|*.wav;*.mp3";
            //dialog.Filter = "サウンドファイル(*.wav)|*.wav";
            dialog.CheckFileExists = true;
            dialog.Multiselect = true;
            //dialog.FileName = textBoxSoundFilePath.Text;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string[] fileNames = dialog.FileNames;
                foreach (string item in fileNames)
                {
                    listBoxNewResponseSoundFilePathes.Items.Add(item);
                }

            }
        }
        

        private void buttonNewResponseSoundFilePathesRemove_Click(object sender, EventArgs e)
        {

            while (listBoxNewResponseSoundFilePathes.SelectedItems.Count != 0)
            {
                listBoxNewResponseSoundFilePathes.Items.RemoveAt(listBoxNewResponseSoundFilePathes.SelectedIndices[0]);
            }


        }

        //ミミック勝手に改造 ここまで

        private void buttonCaptionIndentPaddingLeft_Click(object sender, EventArgs e)
        {
            FormMain.UserConfig.CaptionIndentLeftPadding--;
            FormCaption.Instance.Refresh();
        }

        private void buttonCaptionIndentPaddingRight_Click(object sender, EventArgs e)
        {
            FormMain.UserConfig.CaptionIndentLeftPadding++;
            FormCaption.Instance.Refresh();
        }

        private void buttonCaptionIndentPaddingUp_Click(object sender, EventArgs e)
        {
            FormMain.UserConfig.CaptionIndentTopPadding--;
            FormCaption.Instance.Refresh();
        }

        private void buttonCaptionIndentPaddingDown_Click(object sender, EventArgs e)
        {
            FormMain.UserConfig.CaptionIndentTopPadding++;
            FormCaption.Instance.Refresh();
        }

        private void buttonResSoundTest_Click(object sender, EventArgs e)
        {
            if (listBoxNewResponseSoundFilePathes.Items.Count > 0 && listBoxNewResponseSoundFilePathes.SelectedIndex >= 0)
            {
                var NewResponseSoundFilePath = listBoxNewResponseSoundFilePathes.SelectedItem.ToString();
                //System.Console.WriteLine(NewResponseSoundFilePath);
                //System.Console.WriteLine(soundIdx);
                if (File.Exists(NewResponseSoundFilePath))
                {
                    try
                    {
                        //ミミック勝手に改造　テスト再生側もMP3対応
                        //var newplaysound = new Microsoft.DirectX.AudioVideoPlayback.Audio(NewResponseSoundFilePath);
                        //newplaysound.Volume = (-100 * (100 - FormMain.UserConfig.PlayVolume)) / 2;

                        System.Windows.Media.MediaPlayer newplaysound;
                        newplaysound = new System.Windows.Media.MediaPlayer();
                        newplaysound.Open(new System.Uri(NewResponseSoundFilePath));
                        newplaysound.Volume = FormMain.UserConfig.PlayVolume *0.01;
                        newplaysound.Play();
                        //ミミック勝手に改造　ここまで
                    }
                    catch (Exception ex)
                    {
                        FormMain.Instance.AddLog("PlaySoundNewResponse:{0}", ex.Message);
                    }
                }
            }
        }

        private void trackBarAutoScrollSpeed_ValueChanged(object sender, EventArgs e)
        {
            labelAutoScrollSpeed.Text = trackBarAutoScrollSpeed.Value.ToString();
        }

        private void buttonRefernceLogFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Title = "エディターのファイルパスの指定";
            dialog.Filter = "データファイル(*.txt,*.dat,*.log)|*.txt;*.dat;*.log|All Files (*.*)|*.*";
            dialog.CheckFileExists = false;
            dialog.FileName = textBoxOutputLogPath.Text;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBoxOutputLogPath.Text = dialog.FileName;
            }

        }

        private void buttonInsertRes_Click(object sender, EventArgs e)
        {
            FormMain.Instance.insertTextBox(this.textBoxOutputLogFormat, "#Res#");
        }

        private void buttonInsertNo_Click(object sender, EventArgs e)
        {
            FormMain.Instance.insertTextBox(this.textBoxOutputLogFormat, "#No#");
        }

        private void buttonInsertName_Click(object sender, EventArgs e)
        {
            FormMain.Instance.insertTextBox(this.textBoxOutputLogFormat, "#Name#");
        }

        private void buttonInsertTime_Click(object sender, EventArgs e)
        {
            FormMain.Instance.insertTextBox(this.textBoxOutputLogFormat, "#Time#");
        }

        private void buttonRefernceCommandLineTarget_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Title = "エディターのファイルパスの指定";
            dialog.Filter = "All Files (*.*)|*.*";
            dialog.CheckFileExists = false;
            dialog.FileName = textBoxCommandLineTargetPath.Text;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBoxCommandLineTargetPath.Text = dialog.FileName;
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            FormMain.Instance.insertTextBox(this.textBoxCommandLineParam, "#Res#");

        }

        private void button3_Click(object sender, EventArgs e)
        {
            FormMain.Instance.insertTextBox(this.textBoxCommandLineParam, "#No#");

        }

        private void button4_Click(object sender, EventArgs e)
        {
            FormMain.Instance.insertTextBox(this.textBoxCommandLineParam, "#Name#");

        }

        private void button5_Click(object sender, EventArgs e)
        {
            FormMain.Instance.insertTextBox(this.textBoxCommandLineParam, "#Time#");

        }

        private void textBoxOutputLogFormat_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox6_Enter(object sender, EventArgs e)
        {

        }

        //listBoxNewResponseSoundFilePathesのDragEnterイベントハンドラ
        private void listBoxNewResponseSoundFilePathes_DragEnter(object sender,
            System.Windows.Forms.DragEventArgs e)
        {
            //コントロール内にドラッグされたとき実行される
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                //ドラッグされたデータ形式を調べ、ファイルのときはコピーとする
                e.Effect = DragDropEffects.Copy;
            else
                //ファイル以外は受け付けない
                e.Effect = DragDropEffects.None;
        }

        //listBoxNewResponseSoundFilePathesのDragDropイベントハンドラ
        private void listBoxNewResponseSoundFilePathes_DragDrop(object sender,
            System.Windows.Forms.DragEventArgs e)
        {
            //コントロール内にドロップされたとき実行される
            //ドロップされたすべてのファイル名を取得する
            string[] fileName =
                (string[])e.Data.GetData(DataFormats.FileDrop, false);
            //string nanfedeya = "dasdaw";
            int fileNamenum = 0;
            bool checkok = true;
            //string fileNamechecked = "gaedada";
            while(fileNamenum < fileName.Length)
            {
                string extcheck = Path.GetExtension(fileName[fileNamenum]);
                if(extcheck == ".mp3"|| extcheck == ".wav")
                    fileNamenum = fileNamenum + 1;
                else
                {
                    checkok = false;
                    fileNamenum = fileNamenum + 1;
                }
            }
            if (fileNamenum == fileName.Length)
            {
                if (checkok)
                {
                    //listBoxNewResponseSoundFilePathesに追加する
                    listBoxNewResponseSoundFilePathes.Items.AddRange(fileName);
                    fileNamenum = 0;
                }
                else
                {
                    fileNamenum = 0;
                }
            }

                // fileName.Length;



                //listBoxNewResponseSoundFilePathesに追加する
                //listBoxNewResponseSoundFilePathes.Items.AddRange(fileName);
        }

        private void label20_Click(object sender, EventArgs e)
        {

        }

        private void listBoxNewResponseSoundFilePathes_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
