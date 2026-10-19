// Gadgets: the clock and four timers (countdown, count-up, pomodoro, alarm), each in
// its own overlay window, opened from the ガジェット button on the status bar, 2026-10-19. GPLv2.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        // One timer type: its own clock work, window and hover bar; several can be open at once.
        private sealed class TimerGadget
        {
            public string Mode, Title;
            public GadgetTimer Timer;
            public FormTimerGadget Window;
            public OverlayHoverBar Bar;
            public ToolStripMenuItem MenuItem;
        }

        private ToolStripDropDownButton gadgetButton;
        private FormClockGadget clockWindow;
        private OverlayHoverBar clockBar;
        private readonly List<TimerGadget> timerGadgets = new List<TimerGadget>();
        // Keeps time (and rings) while a timer window is closed.
        private readonly Timer gadgetTick = new Timer { Interval = 250 };
        private readonly NotificationSoundPlayer gadgetSound = new NotificationSoundPlayer();
        private TimerGadget ringingGadget;
        private string ringingSound;

        // Called by the status bar setup: the button sits left of テロップ入力 and opens upward.
        private void InitializeGadgetUi()
        {
            gadgetButton = new ToolStripDropDownButton { Name = "gadgetButton", DropDownDirection = ToolStripDropDownDirection.AboveLeft,
                ImageScaling = ToolStripItemImageScaling.None, DisplayStyle = ToolStripItemDisplayStyle.ImageAndText, Text = "ガジェット",
                TextImageRelation = TextImageRelation.ImageBeforeText, ToolTipText = "時計・タイマーの窓（OBS のウィンドウキャプチャ用）", Margin = new Padding(0, 1, 4, 1) };
            gadgetButton.Image = CreateThemedIcon(gadgetButton, "clock");
            var clock = new ToolStripMenuItem("時計", null, delegate { ToggleClockGadget(); });
            gadgetButton.DropDownItems.Add(clock);
            gadgetButton.DropDownItems.Add(new ToolStripSeparator());
            foreach (var mode in new[] { new[] { GadgetTimer.CountdownMode, "カウントダウン" }, new[] { GadgetTimer.CountUpMode, "カウントアップ" },
                new[] { GadgetTimer.PomodoroMode, "ポモドーロ" }, new[] { GadgetTimer.AlarmMode, "アラーム" } })
            {
                var gadget = new TimerGadget { Mode = mode[0], Title = mode[1], Timer = new GadgetTimer { Mode = mode[0] } };
                gadget.MenuItem = new ToolStripMenuItem(mode[1], null, delegate { ToggleTimerGadget(gadget); });
                gadget.Timer.Alert += sound => PlayGadgetSound(gadget, sound);
                timerGadgets.Add(gadget);
                gadgetButton.DropDownItems.Add(gadget.MenuItem);
            }
            gadgetButton.DropDownItems.Add(new ToolStripSeparator());
            var colors = new ToolStripMenuItem("ガジェットの色");
            var dark = new ToolStripMenuItem("ダーク", null, delegate { SetGadgetTheme("dark"); });
            var light = new ToolStripMenuItem("ライト", null, delegate { SetGadgetTheme("light"); });
            colors.DropDownItems.Add(dark);
            colors.DropDownItems.Add(light);
            colors.DropDownDirection = ToolStripDropDownDirection.Left;
            gadgetButton.DropDownItems.Add(colors);
            gadgetButton.DropDownItems.Add(new ToolStripMenuItem("音の設定…", null, delegate { ShowSettings("ガジェット"); }));
            gadgetButton.DropDownOpening += delegate {
                clock.Checked = clockWindow != null;
                foreach (var gadget in timerGadgets) gadget.MenuItem.Checked = gadget.Window != null;
                bool isDark = UserConfig == null || UserConfig.GadgetTheme != "light";
                dark.Checked = isDark;
                light.Checked = !isDark;
            };
            statusStrip1.Items.Add(gadgetButton);
            Shown += delegate {
                LoadGadgetSettings();
                // The clock and the stream comments window come back if they were showing when the app last closed.
                if (UserConfig != null && UserConfig.ClockOpen && !IsDesignPreview) ToggleClockGadget();
                if (UserConfig != null && UserConfig.StreamOverlayOpen && !IsDesignPreview) EnterStreamMode();
            };
            gadgetTick.Tick += delegate { foreach (var gadget in timerGadgets) if (gadget.Timer.Running) gadget.Timer.Read(); };
            gadgetTick.Start();
            gadgetSound.Ended += delegate {
                // The alarm sound is a few seconds long: it repeats until stopped. The chime plays once.
                if (ringingGadget != null && ringingSound == GadgetTimer.BeepSound && UserConfig != null) PlayGadgetSoundOnce();
                else ringingGadget = null;
            };
        }

        private TimerGadget Gadget(string mode) { return timerGadgets.Find(g => g.Mode == mode); }

        private void LoadGadgetSettings()
        {
            if (UserConfig == null) return;
            Gadget(GadgetTimer.CountdownMode).Timer.Countdown = TimeSpan.FromSeconds(Math.Max(1, UserConfig.TimerCountdownSeconds));
            var pomodoro = Gadget(GadgetTimer.PomodoroMode).Timer;
            pomodoro.Focus = TimeSpan.FromMinutes(Math.Max(1, UserConfig.PomodoroFocusMinutes));
            pomodoro.Break = TimeSpan.FromMinutes(Math.Max(1, UserConfig.PomodoroBreakMinutes));
            pomodoro.LongBreak = TimeSpan.FromMinutes(Math.Max(15, Math.Min(30, UserConfig.PomodoroLongBreakMinutes)));
            TimeSpan alarm;
            Gadget(GadgetTimer.AlarmMode).Timer.AlarmAt = TimeSpan.TryParseExact(UserConfig.AlarmTime ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out alarm)
                ? alarm : new TimeSpan(DateTime.Now.Hour, 0, 0).Add(TimeSpan.FromHours(1));
        }

        private ref Rectangle SavedBounds(string mode)
        {
            switch (mode)
            {
                case GadgetTimer.CountUpMode: return ref UserConfig.CountUpBounds;
                case GadgetTimer.PomodoroMode: return ref UserConfig.PomodoroBounds;
                case GadgetTimer.AlarmMode: return ref UserConfig.AlarmBounds;
                case GadgetTimer.CountdownMode: return ref UserConfig.CountdownBounds;
                default: return ref UserConfig.ClockBounds;
            }
        }

        private void SetGadgetTheme(string theme)
        {
            if (UserConfig != null) UserConfig.GadgetTheme = theme;
            RefreshGadgets();
        }

        private void RefreshGadgets()
        {
            if (clockWindow != null) clockWindow.RenderIfChanged();
            foreach (var gadget in timerGadgets) if (gadget.Window != null) gadget.Window.RenderIfChanged();
        }

        // A first-time window opens over the top-right of the comment view, one row per gadget.
        private Rectangle GadgetBounds(Rectangle saved, Size design, int row)
        {
            if (saved.Width >= 80 && saved.Height >= 40 && OnAnyScreen(saved)) return saved;
            var area = webBrowser.RectangleToScreen(webBrowser.ClientRectangle);
            var size = new Size(design.Width * 2 / 5, design.Height * 2 / 5);
            return new Rectangle(area.Right - size.Width - 24, area.Top + 16 + row * 130, size.Width, size.Height);
        }

        private void ToggleClockGadget()
        {
            if (clockWindow != null) { CloseClockGadget(); return; }
            if (UserConfig == null) return;
            clockWindow = new FormClockGadget(GadgetBounds(UserConfig.ClockBounds, GadgetArt.ClockDesign, 0));
            clockWindow.FormClosed += delegate { if (clockWindow != null) CloseClockGadget(); };
            clockBar = new OverlayHoverBar(clockWindow, CloseClockGadget);
            clockWindow.Show();
        }

        private void CloseClockGadget()
        {
            if (clockWindow == null) return;
            var window = clockWindow;
            clockWindow = null;
            UserConfig.ClockBounds = window.Bounds;
            clockBar.Dispose(); clockBar = null;
            if (!window.IsDisposed) { window.Close(); window.Dispose(); }
        }

        private void ToggleTimerGadget(TimerGadget gadget)
        {
            if (gadget.Window != null) { CloseTimerGadget(gadget); return; }
            if (UserConfig == null) return;
            int row = timerGadgets.IndexOf(gadget) + 1;
            gadget.Window = new FormTimerGadget(gadget.Timer, GadgetBounds(SavedBounds(gadget.Mode), GadgetArt.TimerDesign, row));
            gadget.Window.FormClosed += delegate { if (gadget.Window != null) CloseTimerGadget(gadget); };
            gadget.Bar = new OverlayHoverBar(gadget.Window, () => CloseTimerGadget(gadget));
            gadget.Bar.AddButton("開始/停止", () => ToggleTimerRunning(gadget));
            gadget.Bar.AddButton("リセット", () => ResetTimer(gadget));
            if (gadget.Mode != GadgetTimer.CountUpMode) gadget.Bar.AddButton("時間", () => ShowTimerSettings(gadget));
            gadget.Window.Show();
            // A countdown or alarm needs its time first; the dialog starts it.
            if (!gadget.Timer.Running && (gadget.Mode == GadgetTimer.CountdownMode || gadget.Mode == GadgetTimer.AlarmMode))
                BeginInvoke(new Action(() => ShowTimerSettings(gadget)));
        }

        private void CloseTimerGadget(TimerGadget gadget)
        {
            if (gadget.Window == null) return;
            var window = gadget.Window;
            gadget.Window = null;
            SavedBounds(gadget.Mode) = window.Bounds;
            gadget.Bar.Dispose(); gadget.Bar = null;
            if (!window.IsDisposed) { window.Close(); window.Dispose(); }
        }

        private void CloseGadgets()
        {
            if (UserConfig != null) UserConfig.ClockOpen = clockWindow != null;
            CloseClockGadget();
            foreach (var gadget in timerGadgets) CloseTimerGadget(gadget);
            gadgetTick.Stop();
            gadgetSound.Dispose();
        }

        // Either control also silences that timer's ringing alarm.
        private void ToggleTimerRunning(TimerGadget gadget)
        {
            StopGadgetSound(gadget);
            gadget.Timer.Toggle();
            RefreshGadgets();
        }

        private void ResetTimer(TimerGadget gadget)
        {
            StopGadgetSound(gadget);
            gadget.Timer.Reset();
            RefreshGadgets();
        }

        private void PlayGadgetSound(TimerGadget gadget, string sound)
        {
            if (UserConfig == null) return;
            ringingGadget = gadget;
            ringingSound = sound;
            PlayGadgetSoundOnce();
        }

        private void PlayGadgetSoundOnce()
        {
            try { gadgetSound.Play(GadgetSounds.PathFor(ringingSound), UserConfig.GadgetVolume); }
            catch (Exception ex) { AddLog("Gadget sound: {0}", ex.Message); }
        }

        // null stops whatever is ringing (the sound settings' 止める).
        private void StopGadgetSound(TimerGadget gadget)
        {
            if (gadget != null && ringingGadget != gadget) return;
            ringingGadget = null;
            gadgetSound.Stop();
        }

        // One small dialog whose fields follow the timer type: typed numbers, or ▲ ▼ steps of one.
        // 開始 applies the time and starts the timer.
        private void ShowTimerSettings(TimerGadget gadget)
        {
            if (UserConfig == null || gadget.Mode == GadgetTimer.CountUpMode) return;
            var timer = gadget.Timer;
            using (var dialog = new Form { Text = gadget.Title + "の時間", Font = Font, StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(332, 184), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, TopMost = true })
            {
                int left = 32;
                Func<string, int, int, int, Controls.SpinField> field = (label, min, max, value) => {
                    dialog.Controls.Add(new Label { Text = label, Left = left - 14, Top = 14, Width = 92, TextAlign = ContentAlignment.MiddleCenter });
                    var spin = new Controls.SpinField(min, max, value) { Left = left, Top = 38 };
                    dialog.Controls.Add(spin);
                    left += 100;
                    return spin;
                };
                Controls.SpinField hours = null, minutes = null, seconds = null, focus = null, rest = null, longRest = null, alarmHour = null, alarmMinute = null;
                if (gadget.Mode == GadgetTimer.CountdownMode)
                {
                    var t = timer.Countdown;
                    hours = field("時間", 0, 99, (int)t.TotalHours);
                    minutes = field("分", 0, 59, t.Minutes);
                    seconds = field("秒", 0, 59, t.Seconds);
                }
                else if (gadget.Mode == GadgetTimer.PomodoroMode)
                {
                    focus = field("作業（分）", 1, 180, (int)timer.Focus.TotalMinutes);
                    rest = field("休憩（分）", 1, 60, (int)timer.Break.TotalMinutes);
                    longRest = field("長い休憩", 15, 30, (int)timer.LongBreak.TotalMinutes);
                    dialog.Controls.Add(new Label { Text = "4ポモドーロごとに長い休憩（15〜30分）。切り替わりでチャイムが鳴ります。", Left = 18, Top = 106, Width = 300, Height = 34, ForeColor = SystemColors.GrayText });
                }
                else
                {
                    alarmHour = field("時", 0, 23, timer.AlarmAt.Hours);
                    alarmMinute = field("分", 0, 59, timer.AlarmAt.Minutes);
                }
                var ok = new Button { Text = "開始", DialogResult = DialogResult.OK, Left = 148, Top = 146, Width = 80 };
                var cancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Left = 236, Top = 146, Width = 80 };
                dialog.Controls.Add(ok); dialog.Controls.Add(cancel); dialog.AcceptButton = ok; dialog.CancelButton = cancel;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                if (hours != null)
                {
                    var total = new TimeSpan(hours.Value, minutes.Value, seconds.Value);
                    timer.Countdown = total > TimeSpan.Zero ? total : TimeSpan.FromSeconds(1);
                    UserConfig.TimerCountdownSeconds = (int)timer.Countdown.TotalSeconds;
                }
                if (focus != null)
                {
                    timer.Focus = TimeSpan.FromMinutes(focus.Value);
                    timer.Break = TimeSpan.FromMinutes(rest.Value);
                    timer.LongBreak = TimeSpan.FromMinutes(longRest.Value);
                    UserConfig.PomodoroFocusMinutes = focus.Value;
                    UserConfig.PomodoroBreakMinutes = rest.Value;
                    UserConfig.PomodoroLongBreakMinutes = longRest.Value;
                }
                if (alarmHour != null)
                {
                    timer.AlarmAt = new TimeSpan(alarmHour.Value, alarmMinute.Value, 0);
                    UserConfig.AlarmTime = GadgetTimer.FormatTimeOfDay(timer.AlarmAt);
                }
                StopGadgetSound(gadget);
                timer.Reset();
                timer.Start();
                RefreshGadgets();
            }
        }

        // Settings → ガジェット: hear a sound at the volume being chosen.
        internal void TestGadgetSound(string sound, int volume)
        {
            ringingGadget = null;
            ringingSound = sound;
            try { gadgetSound.Play(GadgetSounds.PathFor(sound), volume); }
            catch (Exception ex) { AddLog("Gadget sound: {0}", ex.Message); }
        }

        internal void StopGadgetTest() { StopGadgetSound(null); }
    }
}
