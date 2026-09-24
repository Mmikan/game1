# Phase 4 checkpoint — incomplete

Date: 2026-09-23. Branch: feature/phase-04-enemies-noise.
This is a work-in-progress checkpoint, not Phase 4 acceptance. Do not start Phase 5 yet.

Continuation update: see phase-04.md for newer implementation and validation. The items below describe the earlier handoff and several are now resolved; keep this file as the original checkpoint record.

## Implemented in this working tree

- Authority-gated GameplayNoise events, separated from audible enemy footstep cues.
- NavMesh bake and two greybox enemies in the prototype scene.
- Mannequin and collector state machines, line-of-sight tests, hearing limits and curse filtering.
- Continuous light exposure, mannequin Freeze, collector two-light Stagger, synchronized enemy state.
- Collector item claiming, carrying and release at its home shelf.
- Enemy attack windup, player Downed state, 60-second death deadline and existing rescue integration.
- Online first-person camera linked to the authoritative avatar; look direction and light input sent to Host.
- Basic online pickup/release/rescue input and a server CharacterController.
- Fixed localization SharedData persistence (the previous builder saved tables but not newly added shared keys).

## Evidence

- Logs/phase4-edit.xml: 20/20 EditMode tests passed before the last integration edits. Includes sight range, occlusion, hearing caps, curse filtering and audio mute independence.
- Logs/phase4-play.xml: 2/2 existing PlayMode regression tests passed before the last audio/curse additions. These are not full enemy acceptance tests.
- Logs/phase4-build.log: first Windows build passed (99,393,344 bytes).
- Logs/phase4-host.log: actual two-client scenario passed assignment, theft, Stagger, attack/down and rescue, but failed Freeze movement tolerance.
- Freeze fix explicitly stops NavMesh movement/velocity. AI authority is also disabled on network despawn so a disconnected Client cannot start simulating the enemies locally.
- Logs/phase4-build-2.log: revised Windows build passed (99,397,368 bytes).
- Revised scenario logs: Logs/phase4-host-2.log and Logs/phase4-client-2.log. Inspect GAME1_ENEMY_CHECK and GAME1_ENEMY_SMOKE_COMPLETE for current results.
- Revised scenario result: all seven GAME1_ENEMY_CHECK checks passed; GAME1_ENEMY_SMOKE_COMPLETE success=True. Client independently logged Freeze, InspectItem, Steal, Stagger, Chase and Attack replication.

## Required before declaring Phase 4 complete

- Complete and test item-holder priority, noise/curse/ping priority, mirror-box interactions, black-block enemy ping/speed effects, and all state timeouts.
- Verify full six-second Freeze cap and four-second immunity, twelve-second Stagger cooldown, interrupted inspection/theft, shelf return, 60-second death and no-survivors failure.
- Verify solo enemy play and actual mouse/keyboard Host/Client play; batch scenarios do not prove the complete player experience.
- Finish hearing-loss danger HUD and complete gameplay-noise producers, including network sprint/breath, throwing and Ping. Current online movement only emits walking noise.
- Fix existing NGO warning: NetworkManager and NetworkObject currently share the Systems object. Move NetworkStageState and its NetworkObject to a separate scene object.
- Prevent Client-side item physics and local pickup/sale paths from competing with Host authority; verify stolen-item claims and releases across both peers.
- Audit the Phase 3 completion claims: the map currently only blocks movement, beacon is a timer, emergency-light/insulation fallbacks are absent, and the online debuff controller is not fully connected.
- The collector's curse wakeup and mirror lookup are implemented, but the four real curse items are not yet present in the greybox. The smoke emits a synthetic curse event; this is not a real curse-item acceptance test.
- Review and retain/discard only proven generated Addressables changes after resume. No stash, reset or cleanup was performed at the handoff cutoff.
- Run final tests/build against the final source revision, update accurate acceptance notes, then commit and push when this phase is complete.

## Reproduction

Build with Game1.Editor.PhaseOneContentBuilder.BuildWindows.
Start two GAME1.exe processes with -batchmode -nographics -game1-enemy-smoke -game1-port 7784.
Use -game1-host on the first and -game1-client on the second; provide separate -logFile paths.
The Host exits with 0/1 after the scenario. The Client must be stopped separately.

The smoke only changes player positions, flashlight poses and a noise event. Enemy AI states are not forced.
