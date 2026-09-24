# Phase 4 — validated core checkpoint, acceptance still open

2026-09-23, Unity 6000.3.12f1, branch feature/phase-04-enemies-noise.

## Current implementation

Host-controlled NavMesh enemies and replicated state, separate audible cues and gameplay noise, light exposure/Freeze/Stagger, collector stealing and returning items, damage/down/rescue/death, and four real curse items are present in the greybox. The NetworkStage now lives separately from NetworkManager. Client item rigidbodies are kinematic; theft and sale state are replicated, and duplicate sales are rejected. Online walking, sprint/breath, throw and Ping emit Host-owned noise. Enemy detection HUD supports HearingLoss and black-block revelation.

## Validation evidence

- Logs/phase4-acceptance-edit.xml: 20/20 EditMode tests passed against the current source.
- Logs/phase4-acceptance-play.xml: 2/2 existing PlayMode regressions passed against current source. These do not cover all new functionality.
- Logs/phase4-acceptance-build.log: Windows build succeeded, 99,422,662 bytes. The previous NetworkManager/NetworkObject warning is absent.
- Logs/phase4-resume-host-2.log: 16 checks passed on the preceding build. Freeze movement, six-second cap, immunity, theft, replicated claim, shelf release, two-light Stagger, real enemy attack/down, rescue, Host-only one-time sale, downed lifetime and no-survivors failure. Timer test explicitly invokes the same server downing entry point; it is not a second enemy-attack test.
- Logs/phase4-resume-client-2.log: Client items were kinematic; enemy states and theft/sale changes were received.
- Final confirmation: Logs/phase4-acceptance-host.log repeated all 16 main scenario checks successfully on the acceptance build; Logs/phase4-acceptance-client.log recorded Client-side state replication and kinematic-item verification.
- Logs/phase4-curse-host.log: 12 checks passed on the acceptance build. Real mirror pickup/immediate Freeze/four-second cap; shared black blocks/revelation/release cleanup; clown 20-second/10m noise; doll 12-second noise and stopping after release.
- Logs/phase4-solo.log: both solo checks passed on the acceptance build: Freeze and collector stealing following the $500 trigger.
- Automated game runs used headless Windows processes. They do not prove rendering, audible quality or mouse/keyboard usability.

## Still required before Phase 4 completion

- Target selection now has coverage for carrying-player preference, nearest-player fallback after release and visible-player precedence over noise; extend combined moving-player/light cases during manual acceptance.
- Black-block speed increase and restoration, plus shared clown 5m noise, are verified. Further held-item visual/input acceptance remains.
- Verify remaining safe-zone edge cases and moving-light scenarios. InspectItem-to-Investigate, Idle noise Detect, near-range downed repeat attack and startup floor-stock noise have dedicated checks.
- Complete manual input and held-door input flows. Support/rescue E-ray selection and disconnect recovery pass game-side synthetic input checks; Host support/rescue HUD images were inspected. Client-side HUD visual placement and held-item visuals remain to be checked. Proxy player appearance and enemy models remain placeholders.
- Resolve inherited Phase 3 debt separately: actual map/beacon/emergency-light/insulation interactions and complete online debuff integration. Earlier Phase 3 completion claims overstate those features.

This checkpoint is suitable for code review and continuation, not a claim that all Phase 4 acceptance criteria passed. Do not begin Phase 5 based on this document.

## Earlier usage-limit handoff

The five-hour usage window reached 80% at this checkpoint. All source changes remain uncommitted; no stash/reset/discard/commit was performed at the cutoff. Generated Addressables changes are also preserved for review. The next action is to review the remaining acceptance items above, not to treat this phase as completed.

The external Gemma prompt still names the old path E:\\project beta, whereas its launcher is passed the actual current root E:\\project-beta. Verify the reviewer used the current repository before relying on its findings.

## Shared carry checkpoint (2026-09-24)

- Ordinary items now accept a second carrier; occupied secondary slots cannot be overwritten. The existing proximity and alive-player checks still apply.
- A two-carrier item waiting for its partner releases its reservation if the first player moves beyond 2m.
- Transfers now reject recipients already carrying a different item. This guard compiled successfully; a dedicated transfer scenario remains outstanding.
- Windows build passed: Logs/phase4-shared-build.log (99,423,174 bytes).
- Host/client headless scenario passed all 17 checks: Logs/phase4-shared-host.log, GAME1_CURSE_SMOKE_COMPLETE success=True. Includes real solo clown 10m and shared clown 5m emission, both at the 20-second interval, rejection of repeated secondary acquisition, and distant reservation release. Existing mirror, blocks and doll checks also passed.
- No manual visual/input acceptance was performed. Phase 4 remains incomplete; the remaining acceptance work above still applies. Changes remain uncommitted and have not been pushed.

## Rescue and AI transition checkpoint (2026-09-24)

- Rescue now requires continuous E input, refreshed at the input send rate; Host expires the input lease after 0.5 seconds without refresh. Releasing E, exceeding 1.5m, or no longer being Alive cancels the operation. One rescue coroutine per rescuer prevents overlapping progress. Progress and target are NetworkVariables for subsequent HUD integration.
- Mannequin Idle hearing ordinary noise now enters Detect. Sight during Investigate enters Chase directly. Collector sight during InspectItem now enters Investigate before Chase on subsequent perception.
- Build passed: Logs/phase4-rescue-build.log (99,426,246 bytes).
- Two-instance headless run passed all 20 Host checks: Logs/phase4-rescue-host.log, GAME1_ENEMY_SMOKE_COMPLETE success=True. Added progress start, key-release cancellation, missing-input timeout and distance cancellation checks; continuous hold rescue, theft, attack and 60-second death checks also passed. Client item authority check passed and enemy states arrived.
- This scenario drives rescue through the Host player's RPC entry points. Client-originated input, rescuer damage interruption, displayed rescue gauges and exact new AI transition assertions still need dedicated acceptance.
- Client log has two startup NavMesh agent creation failures and one socket recovery message before successful connection (Logs/phase4-rescue-client.log). Do not claim Console error 0; startup cleanup remains required.
- Phase 4 is still incomplete. No commit/push was made at this checkpoint.
## Stability and acceptance checkpoint (2026-09-24)

Implemented:
- Downed/dead HUD, remaining downed seconds, replicated rescue progress display, and online stage/time/sold/stamina values. Added an actual LocalizationSettings asset and build preload registration; localized Japanese text is visible in the rendered capture.
- Enemy NavMeshAgent starts disabled in the saved scene and is enabled after NavMesh data registration. No startup NavMesh creation failures in the new game logs.
- Floor stock starts resting on the floor instead of falling from y=1 and prematurely waking enemies. The first boundary run exposed this; the final run passes Idle noise -> Detect.
- Nearby downed players are eligible enemy targets; repeat attacks kill instead of resetting the down timer. Safe-zone targets cannot receive attack damage. Network down/death drops held items, turns off the light, and clears rescue input/support.
- Support expires past 1.8m or when either player is not Alive; self-support is rejected. Local/network door rotation no longer competes when offline/online. Door hold ownership expires on invalid distance/life state.

Evidence:
- Logs/phase4-stability-build.log: Windows build passed (99,436,443 bytes).
- Logs/phase4-stability-edit.xml: 20/20 EditMode tests passed.
- Logs/phase4-boundary-final-host.log: all 12 boundary checks passed, GAME1_BOUNDARY_COMPLETE success=True. Tests cover Idle sound Detect; no-sight Investigate; block speed 3.2 -> 3.52 -> 3.2; mirror freeze; immunity observed through 3.95s and expiration thereafter; Stagger no repeat through 11.8s and expiration thereafter; actual repeated enemy attack causing death. Stagger cooldown fixture disables navigation to isolate perception timing, so it is not navigation coverage.
- Logs/phase4-remote-rescue-host.log: all 7 checks passed on the preceding boundary build. Actual remote Client RPC initiates rescue, rescuer Downed cancels it, subsequent continuous hold rescues Host. Support expiry and self-support rejection also pass. Client log records received progress. The latest build additionally changes initial stock placement and HUD capture support; no rescue logic changed.
- Logs/phase4-stability-solo.log: solo Freeze and $500-triggered theft both pass.
- Logs/phase4-downed-hud.png: game-rendered screenshot inspected; Japanese downed reason/countdown is readable. This does not substitute for full mouse/keyboard acceptance or multiplayer rescue-gauge visual QA.

Remaining: close the acceptance items above, then review and commit/push Phase 4. Do not infer phase completion from individual passing scenarios. Existing Phase 3 debt remains separately tracked.
Final checkpoint: Logs/phase4-stability-host.log passed all 20 Host integration checks on the latest build, GAME1_ENEMY_SMOKE_COMPLETE success=True; Client authority/state replication checks also passed. The new Host/Client/solo/render logs have none of the previous NavMesh startup failures, socket recovery messages or missing-localization-settings messages. Five-hour Codex usage reached 82%; preserve all changes uncommitted at this cutoff. No stash/reset/discard/commit/push was performed. Resume with interrupted inspection/theft, navigation/priority acceptance and manual cooperative controls.

## Interaction/navigation checkpoint (2026-09-24 evening)

- Added EnemyInteractionScenario (-game1-interaction-smoke). Host/Client test verifies sight interrupting InspectItem before ownership, loss-of-sight return, noise interrupting Steal with both local/network claim release, and ordinary sound taking precedence over Ping/curse.
- Door panels are excluded from the static bake and use moving NavMeshObstacle carving. A door-sized integration fixture on the actual aisle proves direct-path blocking, a complete route around it, and direct-path recovery after moving the obstacle. The current entrance door itself is outside the enemy navigation volume; this fixture is not a claim of a manual entrance pursuit test.
- Online E interaction now dispatches to the door using the actual avatar owner id and a 3m ray. A held door cannot be toggled or have its holder replaced by another client. Offline E interaction is consumed once until release; downed/dead players cannot interact.
- Initial Unity build failed with license exit 198; after the user confirmed Personal was active, retry succeeded. Latest Windows build: Logs/phase4-door-build.log, 99,442,707 bytes.
- Logs/phase4-door-host.log: all 14 checks passed, GAME1_INTERACTION_COMPLETE success=True. Client log contains no Exception/Failed/error matches. Door authority checks cover distant rejection, nearby opening, held closing rejection and closing after release.
- The keyboard binding and offline press-consumption changes compiled, but their real keyboard/mouse acceptance is still outstanding. Hold/support full input flows and multiplayer HUD placement remain incomplete.
- Publish this as a Phase 4 implementation/acceptance checkpoint, not Phase 4 completion. Earlier no-commit statements describe the corresponding past usage-limit cutoffs. Generated Addressables state remains outside this source checkpoint.
## Cooperation input checkpoint (2026-09-24)

- E on an Alive teammate starts support from behind within 1.2m; E again or C cancels. Host moves the supporter toward a point 0.85m behind the target, capped at 4.6m/s. Distance over 1.8m and invalid life states cancel. Start rejects self, front approach, carrying supporters, duplicate supporters and support chains/cycles. Support status has localized HUD text. Full crouch/pose mechanics are still separate work.
- Esc unlocks/shows the cursor and exposes the Host/Join menu; closing relocks it. Local/network movement, looking, interaction and Ping are suppressed while the menu is open; online support is canceled. Menu does not pause enemies or the session.
- On network despawn, the local controller and camera position are restored to the network avatar location. This code path compiled but needs disconnect visual acceptance.
- Project runInBackground is enabled for switching between Host/Client windows. The earlier report of movement stopping is not conclusively attributed to focus loss.
- Logs/phase4-controls-build.log: successful Windows build (99,445,103 bytes).
- Logs/phase4-controls-host.log: 11/11 support/rescue checks pass, GAME1_CLIENT_RESCUE_COMPLETE success=True. New front rejection, follow movement, cycle rejection, explicit cancel; previous distance/self rejection and real Client rescue/damage interruption checks remain green. Client progress reception is logged.
- Logs/phase4-menu-build.log: latest Windows build passed (99,447,423 bytes).
- Logs/phase4-menu-input.log: four game-side synthetic keyboard checks passed (opens, blocks_movement, closes, resumes_movement), GAME1_MENU_COMPLETE success=True. This is in-engine input acceptance, not manual desktop keyboard testing. Support E ray selection and multiplayer support HUD still need input/visual acceptance.
- Codex five-hour usage reached 85% after validation. Preserve this checkpoint uncommitted: no commit/push/stash/reset/discard at cutoff. Last pushed source checkpoint remains e3c89a8. Resume by reviewing and committing these tested changes, then completing remaining cooperation input/HUD acceptance. The external reviewer prompt still contains the old repository spelling; verify its review target before trusting its findings.

## Cooperative E input and HUD checkpoint (2026-09-25)

- Previous support/Esc implementation was reviewed and pushed as 9e68a52 after its saved validation results were checked.
- Added opt-in CoopInputScenario: graphic Host plus headless Client, synthetic E key events through the real input/raycast/RPC path, support start/cancel, held rescue progress/completion, then shutdown/local camera recovery. No desktop key events are injected.
- Visual inspection exposed the online HUD showing the local solo debuff. HUD now uses the owner avatar's replicated Debuff and shared DebuffDefinition.KeyFor mapping. Solo-only tape/drop/debug overlays are not drawn for the disabled solo controller during networking. This is a HUD correction, not completion of all online debuff mechanics.
- Added collector target-priority checks: farther visible carrier is preferred, visible carrier outranks ordinary noise, nearest player is selected after item release. Fixture disables agent transform updates during target selection to isolate perception; actual navigation is checked separately.
- Logs/phase4-hud-priority-build.log: Windows build successful, 99,451,859 bytes.
- Logs/phase4-coop-input-fixed-host.log: all 5 checks pass, GAME1_COOP_INPUT_COMPLETE success=True. The previous run also passed before the HUD fix.
- Logs/phase4-priority-host.log: all 18 checks pass, GAME1_INTERACTION_COMPLETE success=True, including prior interruption/door checks plus target priority.
- Logs/phase4-coop-hud-support.png and phase4-coop-hud-rescue.png were inspected for layout. Logs/phase4-coop-hud-fixed-rescue.png confirms the localized HeavyBreath name matches the Host assignment and rescue progress is readable at the top center. Player meshes remain placeholders.
- This closes synthetic cooperative E-input, Host HUD placement, disconnect-camera and basic target-priority gaps. Phase 4 remains in progress; do not claim manual mouse/keyboard acceptance or Phase 3 debuff completion.- Final startup-order check: waiting for GAME1_NET_CONNECTED client=0 before launching Client produces a clean connection (Logs/phase4-coop-ready-client.log has no socket recovery/exception messages). Logs/phase4-coop-ready-host.log repeats all five cooperative input checks successfully. The preceding simultaneous-start run did recover from two socket messages; do not erase that evidence.
