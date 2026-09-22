# Phase 3 — デバフと協力補完

## 実装

- [x] 10種類の DebuffDefinition と仕様値をScriptableObject化
- [x] MVP 4種をランダムに各プレイヤー1個、Host権威で割り当て
- [x] 禁止組み合わせの再抽選と重複可能な抽選規約
- [x] Tremorの保持揺れ、耐久倍率、床安定化、共同運搬軽減
- [x] TunnelVisionの画面端暗化、Ping制限、地図停止、味方誘導軽減
- [x] HeavyBreathのスタミナ値、ノイズ範囲、しゃがみ回復、支える軽減
- [x] FragileGripの落下率、0.75秒予告、2秒テープ、45秒保護
- [x] 残り6種の仕様値、ソロ代替、協力補完API
- [x] HUD、4言語Localization、開発ビルドのF6デバフ切替
- [x] 2人共同運搬、救助、支える、切断とのネットワーク統合

## 操作

- M: TunnelVisionの手持ち地図を3秒表示
- Tを2秒長押し: FragileGripのテープ箱を使用
- B: LostVoiceの設置ビーコンを使用
- G長押し: BackPainで8kg超の品を引きずる
- F6: 開発ビルドで10種類のデバフを順番に切り替える

## 検証記録

2026-09-23、Unity 6000.3.12f1で検証。

- C#コンパイル / Scene生成: PASS。
- Edit Mode: 15件中15件PASS。
- Play Mode回帰: 2件中2件PASS。
- Windows Standalone: PASS。Builds/Windows/GAME1.exe、99,347,123 bytes。
- 2インスタンス: Host client=0、Client client=1 の接続を確認。
- Host割り当て: GAME1_COOP_SMOKE_DEBUFF_ASSIGNED=True。
- 共同運搬: GAME1_COOP_SMOKE_SHARED=True。
- 救助: GAME1_COOP_SMOKE_RESCUED=True。
- 支える軽減: GAME1_COOP_SMOKE_DEBUFF_ASSISTED=True。
- 切断: GAME1_SHARED_CARRY_RELEASED item=giant_block。
- Host / Clientログ: Exception、Error、NullReference、Socket errorなし。

## 後続Phaseとの接続

- HearingLossは敵距離8mの危険振動HUD、StaticFearは0.7秒ごとのGameplayNoiseイベントを公開する。Phase 4の敵知覚へ接続する。
- 音声波形とGameplayNoiseは分離し、HeavyBreathは通常8m、支える間3mを公開する。
- 3D手元表現、肩を支えるポーズ、デバフアイコン本制作はアート工程で差し替える。
