// Shortcut keys: off by default; when enabled they are Windows global hotkeys, so they work while
// a game or another app has focus (and Windows hands those keys to this app only), 2026-10-20. GPLv2.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpeechCastMe
{
    public partial class FormMain
    {
        // id, menu text, default key; the order of the settings list.
        internal static readonly object[][] HotkeyActions = {
            new object[] { "stream", "配信コメント窓の表示切替", Keys.F2 },
            new object[] { "stop", "読み上げをやめる", Keys.F8 },
            new object[] { "first", "最初のレス", Keys.F9 },
            new object[] { "prev", "前のレス", Keys.F10 },
            new object[] { "next", "次のレス", Keys.F11 },
            // F12 alone is reserved by Windows (the debugger key) and cannot be a global hotkey.
            new object[] { "last", "最後のレス", Keys.Control | Keys.F12 } };

        private const int WM_HOTKEY = 0x0312, MOD_ALT = 1, MOD_CONTROL = 2, MOD_SHIFT = 4, MOD_NOREPEAT = 0x4000;
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hwnd, int id, int modifiers, int key);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);

        private readonly List<int> registeredHotkeys = new List<int>();
        // Keys another program already holds, for the settings page to show.
        internal readonly List<string> HotkeyFailures = new List<string>();

        internal static Keys DefaultHotkey(string id)
        {
            foreach (var action in HotkeyActions) if ((string)action[0] == id) return (Keys)action[2];
            return Keys.None;
        }

        // The configured key for an action: "id=keys" entries; a missing entry means the default.
        internal static Keys HotkeyFor(UserConfig config, string id)
        {
            if (config != null && config.Hotkeys != null)
                foreach (string entry in config.Hotkeys)
                {
                    int split = entry.IndexOf('=');
                    int value;
                    if (split > 0 && entry.Substring(0, split) == id && int.TryParse(entry.Substring(split + 1), out value)) return (Keys)value;
                }
            return DefaultHotkey(id);
        }

        internal static string HotkeyText(Keys keys)
        {
            if (keys == Keys.None) return "なし";
            var parts = new List<string>();
            if ((keys & Keys.Control) != 0) parts.Add("Ctrl");
            if ((keys & Keys.Alt) != 0) parts.Add("Alt");
            if ((keys & Keys.Shift) != 0) parts.Add("Shift");
            parts.Add((keys & Keys.KeyCode).ToString());
            return string.Join("+", parts);
        }

        // Called at start-up and after the settings change.
        internal void ApplyHotkeys()
        {
            foreach (int id in registeredHotkeys) UnregisterHotKey(Handle, id);
            registeredHotkeys.Clear();
            HotkeyFailures.Clear();
            bool on = UserConfig != null && UserConfig.HotkeysEnabled && !IsDesignPreview;
            for (int i = 0; i < HotkeyActions.Length; i++)
            {
                string action = (string)HotkeyActions[i][0];
                Keys keys = HotkeyFor(UserConfig, action);
                if (!on || (keys & Keys.KeyCode) == Keys.None) continue;
                int modifiers = MOD_NOREPEAT | ((keys & Keys.Control) != 0 ? MOD_CONTROL : 0) | ((keys & Keys.Alt) != 0 ? MOD_ALT : 0) | ((keys & Keys.Shift) != 0 ? MOD_SHIFT : 0);
                int id = 0x5C00 + i;
                if (RegisterHotKey(Handle, id, modifiers, (int)(keys & Keys.KeyCode))) registeredHotkeys.Add(id);
                else HotkeyFailures.Add(HotkeyText(keys) + "（" + HotkeyActions[i][1] + "）");
            }
            if (HotkeyFailures.Count > 0) AddLog("Hotkeys in use elsewhere: {0}", string.Join(", ", HotkeyFailures));
        }

        // The reading keys press the toolbar buttons (the 読み上げ menu is gone).
        private ToolStripButton HotkeyButton(string action)
        {
            switch (action)
            {
                case "stop": return toolStripButtonStop;
                case "first": return toolStripButtonFirst;
                case "prev": return toolStripButtonPrev;
                case "next": return toolStripButtonNext;
                case "last": return toolStripButtonLast;
                default: return null;
            }
        }

        private void RunHotkey(int index)
        {
            if (index < 0 || index >= HotkeyActions.Length) return;
            string action = (string)HotkeyActions[index][0];
            if (action == "stream") { ToggleStreamMode(); return; }
            var button = HotkeyButton(action);
            if (button != null && button.Enabled) button.PerformClick();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY) { RunHotkey(m.WParam.ToInt32() - 0x5C00); return; }
            base.WndProc(ref m);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            foreach (int id in registeredHotkeys) UnregisterHotKey(Handle, id);
            registeredHotkeys.Clear();
            base.OnHandleDestroyed(e);
        }
    }
}
