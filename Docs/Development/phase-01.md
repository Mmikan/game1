# Phase 1 — 基盤とローカルMVP

## 範囲

仕様書25章のPhase 1のみ。敵、デバフ、ネットワークプレイは含めない。

## 作業項目

- [x] リポジトリ取得、既存変更確認、Phaseブランチ作成
- [x] Git除外・LFS属性・仕様書配置
- [x] Unity 6 LTS Editorとライセンスの準備
- [x] URP / Input System / Localization / NGOの導入、バージョン固定
- [x] Force Text / Visible Meta Files、Packages lock、Windowsビルド設定
- [x] RunConfig / ItemDefinition、JSON overrideと検証
- [x] FPS移動・しゃがみ・ジャンプ・スタミナ・状態機械
- [x] Interact / 保持・投擲・落下・破損 / ライト / Ping
- [x] 固定灰箱マップ / Door / カート / 出口 / 通常品6種
- [x] 回収・売却・目標達成・タイマー・脱出・Result
- [x] 日本語String TableとHUD
- [x] 設定保存・破損時安全起動・Graphics Reset / Apply / Revert
- [x] Edit Mode / Play Modeテスト
- [x] Windows Standalone Build / Console error 0
- [x] 1人で回収→売却→目標→脱出、タイムアウト失敗を実機確認
- [x] CHANGELOG / 検証記録更新、commit、push、レビュワーへ報告

## レビューで確認する仕様の曖昧さ

後続Phaseの実装前に解消する。追加機能として先回り実装しない。

- 3章の成功（1人以上脱出）と失敗（制限時間0/生存者0）の優先順位。脱出済みの仲間がいる状態で残り全員が死亡/時間切れになった場合の扱い。
- 5章TunnelVisionのソロ代替「手持ち地図」と10章「HUD地図は作らない」の関係。
- 通常はrequiredCarriers=1のglass_unicornを2人で運搬する19章の受入と、6章の共同運搬条件の関係。
- 2章Future欄の「複数敵の同時出現」と、Vertical Slice/10章の敵2体の関係。
- 13章のプリセット別Physics Hzと、21章の物理Hz固定・ゲーム判定一致の関係。

## 現時点の検証記録

2026-09-23、Unity 6000.3.12f1で検証。

- コンテンツ生成とC#コンパイル: PASS、return code 0。
- Edit Mode: 5件中5件PASS。
- Play Mode: 2件中2件PASS。売却目標達成から脱出成功までと、時間切れ失敗を検証。
- Windows Standalone: PASS。`Builds/Windows/GAME1.exe`、ビルド出力99,290,189 bytes。
- Windows実行スモーク: LG側モニター2で起動し、15秒間のPlayerログで例外・Errorなし。
- ローカルGemmaレビュー: 使用量区切りで実行。未コミット変更を保持して引き継ぎ済み。
