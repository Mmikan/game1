# Phase 2 — ネットワーク権威と共同運搬

## 実装

- [x] NGO + Unity TransportのHost / Client接続
- [x] Client入力要求とHost側の距離・状態検証
- [x] Player状態、ライト、Stage ResultのNetworkVariable同期
- [x] Host物理による単独保持・2人共同運搬
- [x] 2人の中点追従、3m超過時の解除
- [x] 保持アイテムの受け渡し
- [x] 2秒救助と支える状態
- [x] Door開度とOpenHeld状態の同期
- [x] Client切断時のDead化、0.2秒後の共同運搬解除・重力復帰
- [x] Windows Standaloneと2インスタンス検証

## 検証記録

2026-09-23、Unity 6000.3.12f1で検証。

- C#コンパイル / Scene生成: PASS、return code 0。
- Edit Mode: 7件中7件PASS。Host Authorityの生存状態・距離・共同運搬条件を含む。
- Play Mode回帰: 2件中2件PASS。
- Windows Standalone: PASS。`Builds/Windows/GAME1.exe`、99,330,580 bytes。
- 2インスタンス: Host `client=0`、Client `client=1` の接続を確認。
- 共同運搬: `giant_block first=0 second=1`、`GAME1_COOP_SMOKE_SHARED=True`。
- 救助: Downedから2秒後に `GAME1_COOP_SMOKE_RESCUED=True`。
- 切断: Client停止後に `GAME1_SHARED_CARRY_RELEASED item=giant_block`。
- Host / Clientログ: Exception、Error、NullReference、Socket errorなし。

## 既知の範囲

- Host migrationと途中参加・再接続は仕様どおりFuture。
- 敵、デバフ、呪物は後続Phaseで実装する。
