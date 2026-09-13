using System.Diagnostics;

namespace MedalClone
{
    /// <summary>
    /// SegmentRecorderが吐き出したセグメント群を、ffmpegのconcat demuxerで
    /// 1本の動画ファイルへ無劣化(-c copy)で結合し、保存フォルダに書き出す。
    /// </summary>
    public static class ClipExporter
    {
        /// <summary>
        /// segmentPaths(古い順)を結合し、outputFolderへ日時ベースのファイル名で保存する。
        /// 成功したら保存先のフルパスを返す。
        /// </summary>
        public static async Task<string?> ExportAsync(
            AppSettings settings,
            List<string> segmentPaths,
            Action<string>? onLog = null)
        {
            if (segmentPaths.Count == 0)
            {
                onLog?.Invoke("保存できるセグメントがありません(録画開始直後の可能性があります)。");
                return null;
            }

            Directory.CreateDirectory(settings.SaveFolder);

            var listFile = Path.Combine(Path.GetTempPath(), $"medalclone_concat_{Guid.NewGuid():N}.txt");
            var fileName = $"Clip_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.mp4";
            var outputPath = Path.Combine(settings.SaveFolder, fileName);

            try
            {
                // concat demuxer用のリストファイルを作成
                var lines = segmentPaths.Select(p => $"file '{p.Replace("\\", "/").Replace("'", "'\\''")}'");
                await File.WriteAllLinesAsync(listFile, lines);

                var args = $"-hide_banner -loglevel warning -y -f concat -safe 0 -i \"{listFile}\" -c copy \"{outputPath}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = settings.FfmpegPath,
                    Arguments = args,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = new Process { StartInfo = psi };
                process.Start();
                var stderr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    onLog?.Invoke($"ffmpegの結合に失敗しました (exit {process.ExitCode}):\n{stderr}");
                    return null;
                }

                onLog?.Invoke($"クリップを保存しました: {outputPath}");
                return outputPath;
            }
            finally
            {
                try { File.Delete(listFile); } catch { /* ignore */ }
            }
        }
    }
}
