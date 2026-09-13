using System.Diagnostics;

namespace MedalClone
{
    /// <summary>
    /// FFmpegを子プロセスとして起動し、デスクトップ映像(+音声)を
    /// 「SegmentSeconds秒ごと」のファイルに分割しながら常時録画し続ける。
    /// バッファに必要な秒数を超えた古いセグメントは自動で削除する。
    /// これにより「直近○分」だけを常に保持する、いわゆるインスタントリプレイを実現する。
    ///
    /// システム音声は「ステレオミキサー」や仮想オーディオケーブルを使わず、
    /// WASAPIループバック(SystemAudioPipe)でC#側から直接キャプチャし、
    /// ffmpegの標準入力(pipe:0)へ生PCMとして流し込む方式を採る。
    /// </summary>
    public class SegmentRecorder : IDisposable
    {
        private readonly AppSettings _settings;
        private Process? _ffmpegProcess;
        private System.Windows.Forms.Timer? _cleanupTimer;
        private SystemAudioPipe? _audioPipe;

        public string SegmentFolder { get; }
        public bool IsRunning => _ffmpegProcess != null && !_ffmpegProcess.HasExited;

        public event Action<string>? LogReceived;

        public SegmentRecorder(AppSettings settings)
        {
            _settings = settings;
            SegmentFolder = Path.Combine(Path.GetTempPath(), "MedalClone_Segments");
            Directory.CreateDirectory(SegmentFolder);
        }

        public void Start()
        {
            if (IsRunning) return;

            // 前回の残骸をクリア
            foreach (var f in Directory.GetFiles(SegmentFolder, "seg_*.mp4"))
            {
                TryDelete(f);
            }

            var args = BuildFfmpegArgs();

            var psi = new ProcessStartInfo
            {
                FileName = _settings.FfmpegPath,
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardInput = true, // "q"での安全停止に使用
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            _ffmpegProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _ffmpegProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) LogReceived?.Invoke(e.Data);
            };

            _ffmpegProcess.Start();
            _ffmpegProcess.BeginErrorReadLine();

            _cleanupTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _cleanupTimer.Tick += (_, _) => CleanupOldSegments();
            _cleanupTimer.Start();
        }

        public void Stop()
        {
            _cleanupTimer?.Stop();
            _cleanupTimer = null;

            if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
            {
                try
                {
                    // 標準入力から "q" を送ってFFmpegを安全に終了させる
                    _ffmpegProcess.StandardInput.Write("q");
                    _ffmpegProcess.StandardInput.Flush();

                    if (!_ffmpegProcess.WaitForExit(3000))
                    {
                        _ffmpegProcess.Kill();
                    }
                }
                catch
                {
                    try { _ffmpegProcess.Kill(); } catch { /* ignore */ }
                }
            }
            _ffmpegProcess = null;
        }
        /// <summary>
        /// 直近 lookbackSeconds 秒分に該当するセグメントファイルのパス一覧を、
        /// 古い順に返す(結合用)。
        /// </summary>
        public List<string> GetRecentSegments(int lookbackSeconds)
        {
            var files = Directory.GetFiles(SegmentFolder, "seg_*.mp4")
                .Select(f => new FileInfo(f))
                .OrderBy(f => f.LastWriteTimeUtc)
                .ToList();

            if (files.Count == 0) return new List<string>();

            var cutoff = DateTime.UtcNow.AddSeconds(-lookbackSeconds);
            var recent = files.Where(f => f.LastWriteTimeUtc >= cutoff).ToList();

            // ファイルがまだ書き込み中の場合があるため、末尾(最新)の1つは除外する
            if (recent.Count > 1)
            {
                recent.RemoveAt(recent.Count - 1);
            }

            return recent.Select(f => f.FullName).ToList();
        }

        private void CleanupOldSegments()
        {
            var keepSeconds = _settings.BufferSeconds + _settings.SegmentSeconds * 2; // 少し余裕を持たせる
            var cutoff = DateTime.UtcNow.AddSeconds(-keepSeconds);

            foreach (var f in Directory.GetFiles(SegmentFolder, "seg_*.mp4"))
            {
                var info = new FileInfo(f);
                if (info.LastWriteTimeUtc < cutoff)
                {
                    TryDelete(f);
                }
            }
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch { /* 使用中なら次回に回す */ }
        }

        /// <summary>
        private string BuildFfmpegArgs()
        {
            var inputs = new List<string> { $"-f gdigrab -framerate {_settings.FrameRate} -i desktop" };
            var audioInputIndices = new List<int>();

            if (_settings.RecordSystemAudio)
            {
                // FFmpeg内蔵のWASAPI機能で既定の再生デバイス(スピーカー等)をループバック録音する
                inputs.Add("-f wasapi -loop 1 -i default");
                audioInputIndices.Add(inputs.Count - 1);
            }

            if (_settings.RecordMicrophone)
            {
                var micName = string.IsNullOrWhiteSpace(_settings.MicrophoneDeviceName)
                    ? "Microphone"
                    : _settings.MicrophoneDeviceName;
                inputs.Add($"-f dshow -i audio=\"{micName}\"");
                audioInputIndices.Add(inputs.Count - 1);
            }

            var inputArgs = string.Join(" ", inputs);

            string mapArgs;
            string filterArgs = "";

            if (audioInputIndices.Count == 0)
            {
                mapArgs = "-map 0:v";
            }
            else if (audioInputIndices.Count == 1)
            {
                mapArgs = $"-map 0:v -map {audioInputIndices[0]}:a";
            }
            else
            {
                var labels = string.Join("", audioInputIndices.Select(i => $"[{i}:a]"));
                filterArgs = $"-filter_complex \"{labels}amix=inputs={audioInputIndices.Count}:duration=first:dropout_transition=0[aout]\"";
                mapArgs = "-map 0:v -map [aout]";
            }

            var audioCodec = audioInputIndices.Count > 0 ? "-c:a aac -b:a 160k" : "";
            var segmentPattern = Path.Combine(SegmentFolder, "seg_%06d.mp4").Replace("\\", "/");

            return
                $"-hide_banner -loglevel warning " +
                $"{inputArgs} " +
                $"{filterArgs} " +
                $"{mapArgs} " +
                $"-c:v libx264 -preset veryfast -pix_fmt yuv420p " +
                $"-g {_settings.FrameRate * _settings.SegmentSeconds} " +
                $"-force_key_frames \"expr:gte(t,n_forced*{_settings.SegmentSeconds})\" " +
                $"{audioCodec} " +
                $"-f segment -segment_time {_settings.SegmentSeconds} -reset_timestamps 1 " +
                $"\"{segmentPattern}\"";
        }
        public void Dispose()
        {
            Stop();
        }
    }
}
