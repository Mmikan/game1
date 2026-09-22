# CHANGELOG

## Unreleased — Phase 1

### Phase 2 Added
- NGO + Unity TransportによるHost / Client接続画面とコマンドライン起動。
- Host検証付きPlayer入力、ライト、Stage Result同期。
- 2人共同運搬、受け渡し、救助、支える、Door保持。
- Client切断時の共同運搬解除とアイテム落下。
- 2インスタンス自動スモーク検証。

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
