# プロジェクト概要

## 1. プロダクト概要
- **目的**: 強震ネットワーク（強震モニタ）の観測データ、気象庁の緊急地震速報（EEW）および各種地震・津波・火山・気象情報をリアルタイムに統合監視・可視化するクロスプラットフォームデスクトップアプリケーション。
- **主要ターゲット**: 地震観測者、配信者、防災に関心を持つ一般ユーザー。
- **提供価値**:
  - 地震波のリアルタイム到達予測・震度分布の美麗な地図上プロット。
  - 複数データソース（DM-D.S.S、気象庁XML、強震モニタ、QZSS等）の冗長受信と高速処理。
  - VoiceVox音声読み上げやカスタム通知音、柔軟なワークフロー自動化。

## 2. コア仕様・主要機能
- **リアルタイム地震監視**:
  - 強震モニタのリアルタイム色球表示・P波/S波到達円・最大震度予測表示。
  - 緊急地震速報（予報・警報）のリアルタイム受信・ポップアップ・音声通知。
- **マルチデータソース連携**:
  - 気象庁防災情報XML、DM-D.S.S、強震モニタAPI、QZSS（準天頂衛星）災害危機管理通報の受信。
- **地図・可視化エンジン**:
  - SkiaSharpを用いたベクター地図投影・タイルキャッシュ・震央位置および震度色分け描画。
- **通知・自動化システム**:
  - ワークフローエンジンによるイベント駆動アクション実行（外部コマンド呼び出し、Webhook等）。
  - VoiceVox連携による震源・震度情報の自動音声アナウンス。
- **クロスプラットフォーム & 自己更新**:
  - Windows、Linux、macOS対応。
  - v0.20以降のビルトイン自動更新機能。

## 3. 技術スタック
- **UIフレームワーク**: Avalonia UI (v11系), ReactiveUI
- **グラフィックス**: SkiaSharp
- **言語・ランタイム**: C# (.NET 10.0)
- **依存ライブラリ**:
  - MessagePack, System.Text.Json
  - LibVLCSharp / ManagedBass (メディア・音声再生)
  - Sentry (エラートラッキング)
- **構成管理・外部連携**:
  - Git Submodule: `jma-code-dictionary` (気象庁コード辞書)
  - リモート構成:
    - `origin`: `sakurakun999/KyoshinEewViewerIngen`（フォーク先）
    - `upstream`: `ingen084/KyoshinEewViewerIngen`（本家）

## 4. 環境構成・起動コマンド
- **必要SDK**: .NET SDK 10.0（※developブランチ現行。ローカルの .NET 9.0環境では `net10.0` ターゲットのため要SDK更新または一時的なターゲット変更）
- **開発実行コマンド**:
  ```bash
  # 依存パッケージ復元
  dotnet restore

  # 開発起動
  dotnet run --project src/KyoshinEewViewer.Desktop/KyoshinEewViewer.Desktop.csproj

  # 監視ビルド起動
  dotnet watch run --project src/KyoshinEewViewer.Desktop/KyoshinEewViewer.Desktop.csproj
  ```
- **ビルド・配布パッケージング**:
  ```bash
  # Windows x64 単一バイナリ出力
  dotnet publish src/KyoshinEewViewer.Desktop/KyoshinEewViewer.Desktop.csproj -c Release -r win-x64 -o publish/win-x64 -p:PublishSingleFile=true --self-contained true
  ```

## 5. 変更履歴（Changelog）
- 2026-10-24: [リポジトリ移行] フォーク先リポジトリ `sakurakun999/KyoshinEewViewerIngen`（サブモジュール同期済）へ再クローンし、本家を `upstream` に設定。PROJECT_OVERVIEW.md を作成。
- 2026-10-24: [総合修正・品質強化] 敵対的コード監査に基づき、コマンドインジェクション脆弱性修正、自動更新fail-open修正およびロールバック機能追加、音声再生デッドロック防止(TaskCompletionSource化)、強震モニタ描画ループのGCアロケーション削減、Webhookタイムアウト・サイズ制限、NotImplementedExceptionフォールバック、XAML StringFormat Mode=OneWay適用、極小フォント是正を実施。
