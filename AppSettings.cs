using System.Text.Json;
using System.Windows.Forms;

namespace MedalClone
{
    /// <summary>
    /// アプリの設定。JSONファイル(settings.json)にexeと同じフォルダへ保存する。
    /// </summary>
    public class AppSettings
    {
        // 録画関連
        public string SaveFolder { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "MedalClone");

        public string FfmpegPath { get; set; } = "ffmpeg.exe"; // PATHが通っていればこのままでOK

        // インスタントリプレイ(常時バックグラウンド録画)関連
        public int BufferSeconds { get; set; } = 300;   // 保持しておく秒数(デフォルト5分)
        public int SegmentSeconds { get; set; } = 10;    // 何秒ごとにセグメントを切るか

        // 映像/音声ソース
        public int FrameRate { get; set; } = 30;

        public bool RecordSystemAudio { get; set; } = true;
        // 以下の行を追加（Windowsの環境に合わせてデフォルト値を設定）
        public string SystemAudioDeviceName { get; set; } = "ステレオ ミキサー";

        // システム音声(スピーカーから出る音)はWASAPIループバックで直接キャプチャするため、
        // 「ステレオミキサー」等のデバイス設定は不要。既定の再生デバイスが自動的に対象になる。
        public bool RecordSystemAudio { get; set; } = true;

        public bool RecordMicrophone { get; set; } = false;
        public string MicrophoneDeviceName { get; set; } = ""; // 空なら "Microphone" を試みる(dshow経由)

        // ホットキー(既定 F9、修飾キーなし)
        public Keys HotkeyKey { get; set; } = Keys.F9;
        public bool HotkeyCtrl { get; set; } = false;
        public bool HotkeyAlt { get; set; } = false;
        public bool HotkeyShift { get; set; } = false;

        private static string ConfigPath =>
            Path.Combine(AppContext.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null) return loaded;
                }
            }
            catch
            {
                // 設定ファイルが壊れている場合はデフォルトにフォールバック
            }
            return new AppSettings();
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
    }
}
