using System.Windows.Forms;

namespace MedalClone
{
    public class SettingsForm : Form
    {
        private readonly AppSettings _settings;

        private TextBox _saveFolderBox = null!;
        private TextBox _ffmpegPathBox = null!;
        private NumericUpDown _bufferSecondsBox = null!;
        private NumericUpDown _segmentSecondsBox = null!;
        private NumericUpDown _frameRateBox = null!;
        private CheckBox _systemAudioBox = null!;
        private CheckBox _micBox = null!;
        private TextBox _micDeviceBox = null!;
        private TextBox _hotkeyDisplayBox = null!;

        private Keys _pendingKey;
        private bool _pendingCtrl, _pendingAlt, _pendingShift;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;
            Text = "MedalClone - 設定";
            Width = 520;
            Height = 560;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            BuildUi();
            LoadFromSettings();
        }

        private void BuildUi()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                Padding = new Padding(12),
                AutoSize = true,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));

            int row = 0;

            layout.Controls.Add(new Label { Text = "保存フォルダ:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            _saveFolderBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_saveFolderBox, 1, row);
            var browseBtn = new Button { Text = "参照...", Dock = DockStyle.Fill };
            browseBtn.Click += (_, _) => BrowseFolder();
            layout.Controls.Add(browseBtn, 2, row);
            row++;

            layout.Controls.Add(new Label { Text = "ffmpegパス:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            _ffmpegPathBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_ffmpegPathBox, 1, row);
            var browseFfmpegBtn = new Button { Text = "参照...", Dock = DockStyle.Fill };
            browseFfmpegBtn.Click += (_, _) => BrowseFfmpeg();
            layout.Controls.Add(browseFfmpegBtn, 2, row);
            row++;

            layout.Controls.Add(new Label { Text = "バッファ秒数:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            _bufferSecondsBox = new NumericUpDown { Minimum = 10, Maximum = 3600, Width = 100 };
            layout.Controls.Add(_bufferSecondsBox, 1, row);
            row++;

            layout.Controls.Add(new Label { Text = "セグメント秒数:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            _segmentSecondsBox = new NumericUpDown { Minimum = 2, Maximum = 60, Width = 100 };
            layout.Controls.Add(_segmentSecondsBox, 1, row);
            row++;

            layout.Controls.Add(new Label { Text = "フレームレート:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            _frameRateBox = new NumericUpDown { Minimum = 10, Maximum = 144, Width = 100 };
            layout.Controls.Add(_frameRateBox, 1, row);
            row++;

            _systemAudioBox = new CheckBox
            {
                Text = "システム音声(デスクトップ音)を録音する ※WASAPIループバック、追加設定不要",
                AutoSize = true,
            };
            layout.Controls.Add(_systemAudioBox, 1, row);
            row++;

            _micBox = new CheckBox { Text = "マイク音声も録音する", AutoSize = true };
            layout.Controls.Add(_micBox, 1, row);
            row++;

            layout.Controls.Add(new Label { Text = "  └ マイクデバイス名:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            _micDeviceBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_micDeviceBox, 1, row);
            row++;

            var listDevicesBtn = new Button { Text = "利用可能なマイクデバイス名を確認...", AutoSize = true };
            listDevicesBtn.Click += (_, _) => ShowAvailableDevices();
            layout.Controls.Add(listDevicesBtn, 1, row);
            row++;

            layout.Controls.Add(new Label { Text = "保存ホットキー:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            _hotkeyDisplayBox = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
            _hotkeyDisplayBox.KeyDown += HotkeyDisplayBox_KeyDown;
            layout.Controls.Add(_hotkeyDisplayBox, 1, row);
            var hint = new Label { Text = "クリックしてキー入力", AutoSize = true, ForeColor = SystemColors.GrayText };
            layout.Controls.Add(hint, 2, row);
            row++;

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
            };
            var okBtn = new Button { Text = "保存", DialogResult = DialogResult.OK, Width = 90 };
            okBtn.Click += (_, _) => SaveToSettings();
            var cancelBtn = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Width = 90 };
            buttonPanel.Controls.Add(okBtn);
            buttonPanel.Controls.Add(cancelBtn);
            layout.Controls.Add(buttonPanel, 1, row);

            AcceptButton = okBtn;
            CancelButton = cancelBtn;

            Controls.Add(layout);
        }

        private void HotkeyDisplayBox_KeyDown(object? sender, KeyEventArgs e)
        {
            // 修飾キー単体は無視
            if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu) return;

            _pendingKey = e.KeyCode;
            _pendingCtrl = e.Control;
            _pendingAlt = e.Alt;
            _pendingShift = e.Shift;

            _hotkeyDisplayBox.Text = FormatHotkey(_pendingKey, _pendingCtrl, _pendingAlt, _pendingShift);
            e.SuppressKeyPress = true;
            e.Handled = true;
        }

        private static string FormatHotkey(Keys key, bool ctrl, bool alt, bool shift)
        {
            var parts = new List<string>();
            if (ctrl) parts.Add("Ctrl");
            if (alt) parts.Add("Alt");
            if (shift) parts.Add("Shift");
            parts.Add(key.ToString());
            return string.Join(" + ", parts);
        }

        private void ShowAvailableDevices()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = string.IsNullOrWhiteSpace(_ffmpegPathBox.Text) ? "ffmpeg.exe" : _ffmpegPathBox.Text,
                    Arguments = "-hide_banner -list_devices true -f dshow -i dummy",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var process = System.Diagnostics.Process.Start(psi)!;
                var output = process.StandardError.ReadToEnd();
                process.WaitForExit(5000);

                using var resultForm = new Form
                {
                    Text = "利用可能な音声/映像デバイス",
                    Width = 640,
                    Height = 480,
                    StartPosition = FormStartPosition.CenterParent,
                };
                var box = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    Dock = DockStyle.Fill,
                    ScrollBars = ScrollBars.Both,
                    Text = output,
                    Font = new System.Drawing.Font("Consolas", 9),
                };
                resultForm.Controls.Add(box);
                resultForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"デバイス一覧の取得に失敗しました。\n\n{ex.Message}", "MedalClone",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BrowseFolder()
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = _saveFolderBox.Text };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _saveFolderBox.Text = dialog.SelectedPath;
            }
        }

        private void BrowseFfmpeg()
        {
            using var dialog = new OpenFileDialog { Filter = "ffmpeg.exe|ffmpeg.exe|すべてのファイル|*.*" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _ffmpegPathBox.Text = dialog.FileName;
            }
        }

        private void LoadFromSettings()
        {
            _saveFolderBox.Text = _settings.SaveFolder;
            _ffmpegPathBox.Text = _settings.FfmpegPath;
            _bufferSecondsBox.Value = Math.Clamp(_settings.BufferSeconds, 10, 3600);
            _segmentSecondsBox.Value = Math.Clamp(_settings.SegmentSeconds, 2, 60);
            _frameRateBox.Value = Math.Clamp(_settings.FrameRate, 10, 144);
            _systemAudioBox.Checked = _settings.RecordSystemAudio;
            _micBox.Checked = _settings.RecordMicrophone;
            _micDeviceBox.Text = _settings.MicrophoneDeviceName;

            _pendingKey = _settings.HotkeyKey;
            _pendingCtrl = _settings.HotkeyCtrl;
            _pendingAlt = _settings.HotkeyAlt;
            _pendingShift = _settings.HotkeyShift;
            _hotkeyDisplayBox.Text = FormatHotkey(_pendingKey, _pendingCtrl, _pendingAlt, _pendingShift);
        }

        private void SaveToSettings()
        {
            _settings.SaveFolder = _saveFolderBox.Text;
            _settings.FfmpegPath = _ffmpegPathBox.Text;
            _settings.BufferSeconds = (int)_bufferSecondsBox.Value;
            _settings.SegmentSeconds = (int)_segmentSecondsBox.Value;
            _settings.FrameRate = (int)_frameRateBox.Value;
            _settings.RecordSystemAudio = _systemAudioBox.Checked;
            _settings.RecordMicrophone = _micBox.Checked;
            _settings.MicrophoneDeviceName = _micDeviceBox.Text;
            _settings.HotkeyKey = _pendingKey;
            _settings.HotkeyCtrl = _pendingCtrl;
            _settings.HotkeyAlt = _pendingAlt;
            _settings.HotkeyShift = _pendingShift;
            _settings.Save();
        }
    }
}
