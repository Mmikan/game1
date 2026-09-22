# CHANGELOG

## Unreleased — Phase 1

### Added
- Unity向けGit除外設定と大容量アセットのLFS属性。
- 実装・受入チェックリスト。
- Unity 6 URPプロジェクトとNGO、Input System、Localizationの基盤。
- ローカルFPS操作、Interact、保持・投擲・破損、ライト、Ping。
- 固定灰箱マップ、Door、売却カート、出口、通常品6種。
- 12分ラン、売却目標、脱出・時間切れResult、ローカライズHUD。
- Data JSON exportとRunConfig override、安全起動対応のGraphics設定。
- Edit Mode / Play ModeテストとWindowsビルド処理。

### Changed
- 仕様書をDocs/Specsへ移動（本文変更なし）。
- URP Pipeline Assetを明示設定し、未使用シェーダーバリアントをビルド時に除外。

### Validated
- Unity 6000.3.12f1: コンパイル成功。
- Edit Mode 5/5、Play Mode 2/2成功。
- Windows Standaloneビルド成功、起動時Playerログの例外・Errorなし。
