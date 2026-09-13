using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MedalClone
{
    /// <summary>
    /// Win32のRegisterHotKeyを使ってアプリがフォアグラウンドでなくても
    /// 反応するグローバルホットキーを提供する隠しウィンドウ。
    /// </summary>
    public class HotkeyWindow : NativeWindow, IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 0x5A11; // 適当な一意ID

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // MOD_* フラグ
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;

        public event Action? HotkeyPressed;

        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
        }

        /// <summary>
        /// 現在の登録を解除して、指定のキーで再登録する。
        /// </summary>
        public bool Register(Keys key, bool ctrl, bool alt, bool shift)
        {
            Unregister();

            uint modifiers = 0;
            if (ctrl) modifiers |= MOD_CONTROL;
            if (alt) modifiers |= MOD_ALT;
            if (shift) modifiers |= MOD_SHIFT;

            return RegisterHotKey(Handle, HOTKEY_ID, modifiers, (uint)key);
        }

        public void Unregister()
        {
            UnregisterHotKey(Handle, HOTKEY_ID);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke();
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            Unregister();
            DestroyHandle();
        }
    }
}
