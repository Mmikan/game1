# GAME1

閉店した玩具店で、仲間とデバフを補いながら品物を回収・売却して脱出する、Windows向け協力型ホラーのプロトタイプ。

## 仕様

[実装仕様書 v1.1](Docs/Specs/3D協力型ホラーゲーム_実装仕様書_v1.1_Git管理版.md) を唯一の仕様とする。未記載機能とFutureは実装しない。

## 開発状況

Phase 1の環境準備中。プレイ可能なUnityプロジェクトや検証済みビルドはまだありません。

- Unity 6.3 LTS / URP / C#
- Unity Input System / Unity Localization
- NGO + Unity Transportは基盤のみ。ネットワーク動作はPhase 2。
- 作業ブランチ: `feature/phase-01-foundation`
- [Phase 1の作業・検証項目](Docs/Development/phase-01.md)

各Phaseはコンパイル、テスト、Windowsビルド、受入確認後に完了とし、commit / pushして結果を報告する。未検証の変更はmainに統合しない。
