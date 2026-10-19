// The timer gadget's clock work: countdown, count-up, pomodoro and alarm, 2026-10-19. GPLv2.
// Kept apart from drawing so it keeps time while its window is closed.
using System;

namespace SpeechCastMe
{
    internal sealed class GadgetTimer
    {
        public const string CountdownMode = "countdown", CountUpMode = "countup", PomodoroMode = "pomodoro", AlarmMode = "alarm";

        public string Mode = CountdownMode;
        public TimeSpan Countdown = TimeSpan.FromMinutes(15);
        public TimeSpan Focus = TimeSpan.FromMinutes(25);
        public TimeSpan Break = TimeSpan.FromMinutes(5);
        public TimeSpan LongBreak = TimeSpan.FromMinutes(15);
        public const int PomodorosPerSet = 4; // a long break after every fourth focus

        public const string ChimeSound = "chime", BeepSound = "beep";
        // Raised from Read(): the pomodoro changed phase (chime) or a countdown / alarm ran out (beep).
        public event Action<string> Alert;
        public TimeSpan AlarmAt = new TimeSpan(22, 0, 0); // time of day

        // Replaceable for tests.
        public Func<DateTime> Now = () => DateTime.Now;

        private DateTime? started;      // set while running
        private TimeSpan banked;        // time counted before the last pause
        private bool onBreak;           // pomodoro phase
        private int focusDone;          // focus sessions finished since the reset
        private DateTime alarmTarget;
        private DateTime? finishedAt;

        public bool Running { get { return started != null; } }
        public bool OnBreak { get { return onBreak; } }

        private TimeSpan Elapsed { get { return banked + (started != null ? Now() - started.Value : TimeSpan.Zero); } }

        public void Start()
        {
            if (Running) return;
            if (finishedAt != null) Reset();
            if (Mode == AlarmMode) alarmTarget = NextAlarm();
            started = Now();
        }

        public void Pause()
        {
            if (!Running) return;
            banked = Elapsed;
            started = null;
        }

        public void Toggle() { if (Running) Pause(); else Start(); }

        public void Reset()
        {
            started = null;
            banked = TimeSpan.Zero;
            onBreak = false;
            focusDone = 0;
            finishedAt = null;
        }

        public struct Reading
        {
            public string Label;
            public TimeSpan Value;
            public bool Finished;   // countdown / alarm reached zero
            public bool Idle;       // an alarm that is not set: shown as --:--
            public DateTime? FinishedAt;
        }

        public Reading Read()
        {
            var now = Now();
            switch (Mode)
            {
                case CountUpMode:
                    return new Reading { Label = "STOPWATCH", Value = Elapsed };
                case PomodoroMode:
                    {
                        // Phases follow each other without stopping: focus, break, focus, ...
                        // with a long break after every fourth focus.
                        bool changed = false;
                        var phase = PhaseLength();
                        while (Elapsed >= phase)
                        {
                            banked -= phase;
                            if (!onBreak) focusDone++;
                            onBreak = !onBreak;
                            phase = PhaseLength();
                            changed = true;
                        }
                        if (changed) Raise(ChimeSound);
                        string label = !onBreak ? "FOCUS " + (focusDone % PomodorosPerSet + 1) + "/" + PomodorosPerSet
                            : IsLongBreak ? "LONG BREAK" : "BREAK";
                        return new Reading { Label = label, Value = phase - Elapsed };
                    }
                case AlarmMode:
                    {
                        string label = "ALARM " + FormatTimeOfDay(AlarmAt);
                        if (!Running && finishedAt == null) return new Reading { Label = label, Idle = true };
                        var left = alarmTarget - now;
                        if (Running && left <= TimeSpan.Zero && finishedAt == null) { finishedAt = alarmTarget; started = null; Raise(BeepSound); }
                        if (finishedAt != null) left = TimeSpan.Zero;
                        return new Reading { Label = label, Value = left, Finished = finishedAt != null, FinishedAt = finishedAt };
                    }
                default:
                    {
                        var left = Countdown - Elapsed;
                        if (Running && left <= TimeSpan.Zero && finishedAt == null) { finishedAt = now + left; banked = Countdown; started = null; Raise(BeepSound); }
                        if (left < TimeSpan.Zero || finishedAt != null) left = TimeSpan.Zero;
                        return new Reading { Label = "TIMER", Value = left, Finished = finishedAt != null, FinishedAt = finishedAt };
                    }
            }
        }

        // A break that follows the fourth, eighth, ... focus session is the long one.
        private bool IsLongBreak { get { return onBreak && focusDone > 0 && focusDone % PomodorosPerSet == 0; } }

        private TimeSpan PhaseLength()
        {
            var length = !onBreak ? Focus : IsLongBreak ? LongBreak : Break;
            return length > TimeSpan.Zero ? length : TimeSpan.FromMinutes(1);
        }

        private void Raise(string sound) { if (Alert != null) Alert(sound); }

        // The next time the clock shows AlarmAt; a time already past today means tomorrow.
        private DateTime NextAlarm()
        {
            var now = Now();
            var today = now.Date + AlarmAt;
            return today > now ? today : today.AddDays(1);
        }

        internal static string FormatTimeOfDay(TimeSpan time)
        {
            return ((int)time.TotalHours % 24).ToString("00") + ":" + time.Minutes.ToString("00");
        }
    }
}
