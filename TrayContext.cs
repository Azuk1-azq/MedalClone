using System.Diagnostics;
using System.Windows.Forms;

namespace MedalClone
{
    /// <summary>
    /// メインウィンドウを持たず、タスクトレイのアイコンとしてだけ常駐するアプリケーション本体。
    /// </summary>
    public class TrayContext : ApplicationContext
    {
        private readonly AppSettings _settings;
        private readonly NotifyIcon _trayIcon;
        private readonly SegmentRecorder _recorder;
        private readonly HotkeyWindow _hotkeyWindow;

        private ToolStripMenuItem _toggleBufferingItem = null!;
        private ToolStripMenuItem _saveClipItem = null!;

        public TrayContext()
        {
            _settings = AppSettings.Load();
            Directory.CreateDirectory(_settings.SaveFolder);

            _recorder = new SegmentRecorder(_settings);
            _recorder.LogReceived += msg => Debug.WriteLine($"[ffmpeg] {msg}");

            _hotkeyWindow = new HotkeyWindow();
            _hotkeyWindow.HotkeyPressed += () => _ = SaveClipAsync();
            RegisterHotkeyFromSettings();

            var menu = BuildContextMenu();

            _trayIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application, // 独自アイコンに差し替え可能
                Text = "MedalClone - 待機中",
                Visible = true,
                ContextMenuStrip = menu,
            };
            _trayIcon.DoubleClick += (_, _) => _ = SaveClipAsync();

            // 起動時に自動でバックグラウンド録画(インスタントリプレイ)を開始する
            StartBuffering();
        }

        private ContextMenuStrip BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            _saveClipItem = new ToolStripMenuItem("クリップを保存 (ホットキー)", null, async (_, _) => await SaveClipAsync());
            menu.Items.Add(_saveClipItem);

            _toggleBufferingItem = new ToolStripMenuItem("バックグラウンド録画を停止", null, (_, _) => ToggleBuffering());
            menu.Items.Add(_toggleBufferingItem);

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(new ToolStripMenuItem("クリップ保存フォルダを開く", null, (_, _) =>
            {
                Directory.CreateDirectory(_settings.SaveFolder);
                Process.Start(new ProcessStartInfo(_settings.SaveFolder) { UseShellExecute = true });
            }));

            menu.Items.Add(new ToolStripMenuItem("設定...", null, (_, _) => OpenSettings()));

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(new ToolStripMenuItem("終了", null, (_, _) => ExitApp()));

            return menu;
        }

        private void StartBuffering()
        {
            try
            {
                _recorder.Start();
                _toggleBufferingItem.Text = "バックグラウンド録画を停止";
                _trayIcon.Text = "MedalClone - 録画中(バッファリング)";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"録画の開始に失敗しました。ffmpegのパスを確認してください。\n\n{ex.Message}",
                    "MedalClone", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleBuffering()
        {
            if (_recorder.IsRunning)
            {
                _recorder.Stop();
                _toggleBufferingItem.Text = "バックグラウンド録画を開始";
                _trayIcon.Text = "MedalClone - 待機中";
            }
            else
            {
                StartBuffering();
            }
        }

        private async Task SaveClipAsync()
        {
            if (!_recorder.IsRunning)
            {
                _trayIcon.ShowBalloonTip(3000, "MedalClone", "バックグラウンド録画が停止中です。", ToolTipIcon.Warning);
                return;
            }

            _trayIcon.Text = "MedalClone - クリップ保存中...";
            var segments = _recorder.GetRecentSegments(_settings.BufferSeconds);

            var savedPath = await ClipExporter.ExportAsync(_settings, segments, msg => Debug.WriteLine(msg));

            _trayIcon.Text = "MedalClone - 録画中(バッファリング)";

            if (savedPath != null)
            {
                _trayIcon.ShowBalloonTip(3000, "クリップを保存しました", Path.GetFileName(savedPath), ToolTipIcon.Info);
            }
            else
            {
                _trayIcon.ShowBalloonTip(3000, "MedalClone", "クリップの保存に失敗しました。", ToolTipIcon.Error);
            }
        }

        private void OpenSettings()
        {
            using var form = new SettingsForm(_settings);
            var wasRunning = _recorder.IsRunning;

            if (form.ShowDialog() == DialogResult.OK)
            {
                // 設定変更を反映するため、録画中だった場合は再起動する
                RegisterHotkeyFromSettings();

                if (wasRunning)
                {
                    _recorder.Stop();
                    StartBuffering();
                }
            }
        }

        private void RegisterHotkeyFromSettings()
        {
            var ok = _hotkeyWindow.Register(
                _settings.HotkeyKey, _settings.HotkeyCtrl, _settings.HotkeyAlt, _settings.HotkeyShift);

            if (!ok)
            {
                Debug.WriteLine("ホットキーの登録に失敗しました(他アプリと重複している可能性があります)。");
            }
        }

        private void ExitApp()
        {
            _recorder.Stop();
            _hotkeyWindow.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Exit();
        }
    }
}
