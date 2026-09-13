# MedalClone

Medal.tv や GeForce Experience（ShadowPlay）の「インスタントリプレイ」のように、バックグラウンドで常にデスクトップ画面を録画し、ホットキーを押した瞬間に直近数分間の動画を保存できる軽量なWindows向けアプリケーションです。
メモリ使用率は25~35MBほどです。

##  特徴

- **インスタントリプレイ機能**
  - バックグラウンドで常時録画を行い、指定した時間（例：直近5分間）だけを保持。
  - セグメントごとに分割録画し、古いファイルは自動削除するためストレージを圧迫しません。
- **高速・無劣化クリップ保存**
  - ホットキーを押すと、保持しているセグメントを FFmpeg の `concat` デマルチプレクサを使って再エンコード無しで結合。一瞬で動画ファイル（MP4）を書き出します。
- **マイク音声のミックス**
  - システム音声とマイク音声を同時にキャプチャし、1つの動画にミックスして保存可能です。
- **邪魔にならないタスクトレイ常駐型**
  - メインウィンドウを持たずタスクトレイに常駐。ゲーム中などでも設定したグローバルホットキー（既定: F9）でいつでもクリップを保存できます。

##  動作環境

- **OS**: Windows 10 / Windows 11
- **ランタイム**: .NET 8.0 Desktop Runtime
- **依存ソフト**: [FFmpeg](https://ffmpeg.org/download.html) (`ffmpeg.exe`)


##  使い方

1. `MedalClone.exe` を起動すると、タスクトレイ（画面右下の通知領域）にアイコンが表示され、自動的にバックグラウンド録画（待機）が開始されます。
2. **クリップの保存**: 
   - 録画を残したい場面で **ホットキー（初期設定は F9）** を押すか、タスクトレイアイコンをダブルクリックします。
   - 保存が完了すると、Windows の通知（バルーンチップ）でお知らせします。
3. **設定の変更**:
   - タスクトレイアイコンを右クリックし、「設定...」を選択します。
   - 以下の項目をカスタマイズ可能です。
     - 動画の保存先フォルダ
     - ffmpeg.exe のパス
     - バッファ秒数（何秒さかのぼって保存するか）
     - セグメント秒数（何秒ごとにファイルを区切るか / 基本は変更不要）
     - フレームレート
     - システム音声・マイク音声の録音有無（およびデバイス名）
     - クリップ保存用のグローバルホットキー

## スタック

- C# / .NET 8.0 (Windows Forms)
- Win32 API (`RegisterHotKey`) - グローバルホットキーの処理
- [FFmpeg](https://ffmpeg.org/)
  - `gdigrab` (デスクトップ映像キャプチャ)
  - `wasapi` (システム音声キャプチャ)
  - `dshow` (マイク音声キャプチャ)
  - `segment` muxer / `concat` demuxer (分割録画と無劣化結合)

##  ライセンス

[MIT License](LICENSE)
# MedalClone

MedalClone is a lightweight Windows application—similar to the "Instant Replay" features found in Medal.tv or GeForce Experience (ShadowPlay)—that continuously records your desktop screen in the background, allowing you to save a video of the last few minutes the moment you press a hotkey.
It consumes approximately 25–35 MB of memory.

## Features

- **Instant Replay Functionality**
- Continuously records in the background, retaining only a specified duration (e.g., the last 5 minutes). 
- Records in segments and automatically deletes old files to prevent storage clutter.
- **Fast, Lossless Clip Saving**
- Pressing the hotkey merges the retained segments using FFmpeg's `concat` demuxer without re-encoding, instantly exporting a video file (MP4).
- **Microphone Audio Mixing**
- Captures system audio and microphone audio simultaneously, mixing them into a single saved video file.
- **Unobtrusive Task Tray Operation**
- Runs in the task tray without a main window. You can save clips at any time—even while gaming—using the configured global hotkey (default: F9).

## System Requirements

- **OS**: Windows 10 / Windows 11
- **Runtime**: .NET 8.0 Desktop Runtime
- **Dependencies**: [FFmpeg](https://ffmpeg.org/download.html) (`ffmpeg.exe`)

## Usage

1. Launch `MedalClone.exe`; an icon will appear in the task tray (notification area at the bottom right of the screen), and background recording (standby mode) will start automatically.
2. **Saving Clips**:
- When you want to save a recording, press the **hotkey (default: F9)** or double-click the task tray icon. 
- A Windows notification (balloon tip) will appear once the save is complete. 3. **Changing Settings**:
- Right-click the task tray icon and select "Settings...". 
- The following items can be customized:
- Video save folder
- Path to `ffmpeg.exe`
- Buffer duration (seconds to look back and save)
- Segment duration (interval for splitting files / usually no need to change)
- Frame rate
- System audio/microphone audio recording (and device names)
- Global hotkey for saving clips

## Stack

- C# / .NET 8.0 (Windows Forms)
- Win32 API (`RegisterHotKey`) - Global hotkey handling
- [FFmpeg](https://ffmpeg.org/)
- `gdigrab` (Desktop video capture)
- `wasapi` (System audio capture)
- `dshow` (Microphone audio capture)
- `segment` muxer / `concat` demuxer (Split recording and lossless merging)

## License

[MIT License](LICENSE)
