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

## Door hold WIP cutoff (2026-09-25)

Uncommitted and NOT acceptance-complete. Added pending E door interaction: tap toggles; >=0.25s hold opens/claims; release, input lease expiry, invalid life/distance release the claim. Added localized holding label and server-side TrySetHeldOnServer helper.

Latest build Logs/phase4-door-hold-retry-build.log succeeds (99,454,066 bytes), but its expanded cooperative input scenario FAILS. Logs/phase4-door-hold-retry-host.log shows support start/cancel and rescue progress/completion passing; e_hold_opens_and_holds_door=False, door_rehold_started=False and disconnect_restores_local_controller_camera=False. The release/downed checks are vacuous while acquisition fails; do not count them as validated door behavior. Last confirmed pushed version is 18cb422.

Retry isolates synthetic keyboard/mouse from physical input inside the test process and moves the Client away from the door fixture. Failure persists. Pose evidence: Host=(0,0,-3.5), look=(0,0,1), door pivot=(-1.5,0,-2). After disconnect local player=(0.99,0.08,-1.97), camera=(0.99,1.70,-1.97), versus last Host=(0,0,-3.5). Investigate collider bounds/raycast hits, relocated-door geometry and local controller collision resolution before changing production behavior further. Earlier support/rescue-only disconnect acceptance passed; this fixture relocates the scene door.

Five-hour and weekly usage both reached 87%. Preserve changes: no commit/push/reset/stash/discard at this cutoff. Launch local Gemma reviewer with current root E:\project-beta; its prompt still contains old spelling E:\project beta, so confirm the actual review target. Resume with the failed door-hold acceptance and do not claim Phase 4 complete.

## Door lifecycle repair checkpoint (2026-09-25)

Moved ongoing door-hold processing from OnNetworkSpawn into server Update. Preserve the replicated open/closed state in InteractableDoor on network despawn. Logs/phase4-door-lifecycle-build.log confirms a successful Windows build (99,454,066 bytes).

Logs/phase4-door-lifecycle-host.log passes 8 of 9 synthetic cooperative input checks: support start/cancel, rescue progress/completion, door hold acquisition/release/reacquisition and downed release. Acquisition now succeeds, so release assertions are no longer vacuous. Disconnect restoration still fails: last=(0,0,-3.5), local=(0.29,0.08,-3.25), camera=(0.29,1.70,-3.25). Camera offset is correct, but position drift remains; investigate collider overlap during shutdown. Client log has no Exception/error/Failed matches. Do not claim full acceptance or Phase 4 completion.

Weekly Codex usage reached 90% (five-hour 13%). Stop at this test checkpoint with all modifications preserved uncommitted; no push/commit/reset/stash/discard. Last pushed source remains 18cb422. Launch local Gemma reviewer with the actual repository root. Its prompt's old path spelling remains a known review-target caveat.

## Disconnect collider handoff (2026-09-27)

Disable the outgoing network CharacterController in OnNetworkDespawn before enabling the solo controller at the same location. Guard LateUpdate with IsSpawned so a despawned avatar cannot overwrite the restored camera. This addresses a potential overlapping-controller handoff, but is NOT yet verified as the cause of the remaining position drift.

Both Logs/phase4-despawn-collider-build.log and Logs/phase4-despawn-collider-retry-build.log stop with license exit 198 (No valid Unity Editor license found; access token unavailable). No new executable or runtime acceptance result was produced. User license confirmation requested. Preserve modifications; no commit/push. Resume by building and rerunning all nine cooperative input checks after licensing is restored. Usage at turn start: five-hour 2%, weekly 0%; this interruption is a licensing blocker, not a usage cutoff.

## Unity startup workflow (2026-09-27)

User instruction: start or confirm Unity Hub before Unity Editor builds at the beginning of work. This machine uses the Microsoft Store installation (UnityTechnologies.UnityHub), not the standard Program Files/Unity Hub path. Discover the current running process path or installed app identity rather than hardcoding the versioned WindowsApps directory. User launched Hub and refreshed license information after exit 198; the subsequent build passed licensing and began compilation. This observation does not establish a universal Hub requirement.

## Verified door and disconnect checkpoint (2026-09-27)

After the user launched Unity Hub and refreshed licensing, Logs/phase4-despawn-hub-build.log completed successfully (99,454,066 bytes). Logs/phase4-despawn-hub-host.log passed all 9 synthetic cooperative input checks, GAME1_COOP_INPUT_COMPLETE success=True. Door hold/release/reacquisition/downed release and previous support/rescue checks pass. Disabling the outgoing network controller before restoring the solo controller eliminates lateral displacement: last=(0,0,-3.5), local=(0,0.08,-3.5), camera=(0,1.70,-3.5). The 0.08m vertical settling stays within the existing 0.1m acceptance tolerance. Client log has no Exception/error/Failed matches.

Publish this tested source checkpoint. Manual input acceptance, Client HUD visual checks, short door taps and input-lease expiration coverage remain open; Phase 4 is not complete. Generated Addressables state stays outside the source commit.

## Door input boundaries (2026-09-27)

Removed the short-tap door toggle: specification section 4 requires 0.25s interaction holds. Key release now sends cancellation immediately rather than relying only on the 20Hz refresh. Menu cancellation no longer acts as a tap. Existing hold behavior opens and retains the door; closing interaction and holding-side restriction remain to be designed/verified against the specification, so full door UX is not complete.

Logs/phase4-door-boundaries-build.log: Windows build passed (99,456,114 bytes). Logs/phase4-door-boundaries-host.log: all 15 checks passed, GAME1_COOP_INPUT_COMPLETE success=True. Adds short press rejection, pending interaction menu cancellation, lease expiry and distance release, each release test preceded by verified acquisition. Lease fixture postpones outbound refresh through the private timer while server Update continues; it is not a real transport-outage test. Client log has no Exception/error/Failed matches. Phase 4 remains incomplete.

## Safe-zone attack boundary acceptance (2026-09-27)

Logs/phase4-safe-edge-build.log: Windows build passed (99,457,142 bytes). Logs/phase4-safe-edge-host.log passes all 17 enemy boundary checks, GAME1_BOUNDARY_COMPLETE success=True. Five new checks verify a close safe-zone target survives, an attack windup starts after entry, crossing z=0 cancels its damage while still within attack range, reentry allows damage, and a downed target in the safe zone survives follow-up attacks. Enemy navigation is disabled only for this boundary fixture to isolate attack eligibility; it is not pursuit/navigation coverage. Client log has no Exception/error/Failed matches. No production AI change was needed for these checks.

## Client rescue HUD visual check (2026-09-27)

Added opt-in -game1-coop-client-capture <path> to capture a real Client frame after replicated rescue progress exceeds 25%. Logs/phase4-client-hud-build.log passed (99,458,666 bytes). Graphic Host/Client run repeats all 15 cooperative input checks successfully. Logs/phase4-client-hud-client.log records progress=0.2611814 and Debuff=Tremor. Logs/phase4-client-rescue-hud.png was visually inspected at 1920x1080: Japanese Tremor label, rescue 26%, downed countdown and stamina are legible without overlap. This verifies the rescued Client HUD, not every support/item display or manual input. Client remains connected until Host finishes; capture does not change gameplay state.

## E-key handoff implementation (2026-09-27)

Connected the previously RPC-only transfer action to the actual E ray while carrying a single-carrier item. The ray ignores the source's carried item, selects the teammate, and keeps both inventories unchanged if transfer is rejected. No teammate target retains the existing drop action; shared items and two-carrier reservations keep E-to-release. Server validation now rejects non-owners, self transfer, invalid source/recipient life states, occupied recipients, shared/two-carrier items, sold/stolen items, distance over 2m and intervening solid geometry. No drop/physics transition occurs on success. Recipient-name prompts and handover poses remain outstanding.

Logs/phase4-transfer-final-build.log: Windows build passed (99,461,746 bytes). Logs/phase4-transfer-final-host.log: all 26 cooperative input checks passed, GAME1_COOP_INPUT_COMPLETE success=True. Added actual E handoff, occupied-recipient E rejection without dropping, non-owner/downed/distant/occluded rejection, successful reverse handoff via server helper, and preserved shared-item E release. Other guard tests use server helpers rather than remote Client RPCs. Client log has no Exception/error/Failed matches. This closes the basic handoff input gap; it does not complete inherited Phase 3 debuff work or Phase 4 manual acceptance.

## Usage checkpoint (2026-09-27)

After successful push of 1afd35b, five-hour usage reached 77% (weekly 12%). Stop before another implementation/build cycle and launch the requested local Gemma reviewer. Preserve remaining generated Addressables changes and this handoff note without stash/reset/discard or another commit. Review completion is not yet verified. Next work: door closing/holding-side UX, remaining moving-light/manual acceptance, recipient prompts and held-item visuals; inherited online debuff debt remains. All important 3D model/art work requires notifying the user before starting so they can select Astra.

## Door holding-side and closing implementation (2026-09-28)

Started the installed Microsoft Store Unity Hub before building, per user workflow. Defined the negative-Z side of the closed doorway as its holding side, cached before door rotation. Holding-side E >=0.25s opens/claims the door; opposite-side E >=0.25s toggles open/closed without claiming. Server rejects hold requests from the opposite side. Crossing away from the holding side releases an existing claim without closing. Other players cannot toggle a held door. This resolves the missing online close action with an explicit side-based interaction convention; the holding-side visual marker, progress gauge and full manual UX acceptance remain outstanding.

Logs/phase4-door-sides-final-build.log: Windows build passed (99,463,282 bytes). Logs/phase4-door-sides-final-host.log: 31/31 cooperative input checks passed, GAME1_COOP_INPUT_COMPLETE success=True. New cases cover opposite-side hold rejection, actual E close/open and side crossing release without closing. Avatar yaw is positioned explicitly for the fixture; this is synthetic E input rather than manual mouse acceptance. Logs/phase4-door-sides-interaction-host.log: all 18 existing interruption/navigation/door/target-priority checks passed, GAME1_INTERACTION_COMPLETE success=True. Both Client logs have no Exception/error/Failed matches. Runtime diff whitespace check passed.

The first graphic Host startup exceeded the shell's 25-second readiness wait; starting its Client after checking the process/log allowed that run to finish successfully. The final run waited up to 50 seconds and completed normally. No gameplay failure was observed from that startup delay. Phase 4 remains in progress; no Phase 5 work or important 3D art was started.

## Door interaction hints (2026-09-29)

Confirmed Unity Hub running before build. Added online door hints selected by current side, open state and holder: open-and-hold/hold on the holding side, open/close on the opposite side, and teammate-holding status. The hint follows the nearest 3m camera ray target, skips the local avatar, and is suppressed while carrying, supporting, downed or in the menu. Existing own-hold status takes precedence to avoid overlapping labels. Added Japanese/English/Chinese/Korean localization entries; no gameplay authority change.

Logs/phase4-door-hints-build.log: Windows build passed (99,464,808 bytes). Logs/phase4-door-hints-host.log: all 31 cooperative input checks pass, GAME1_COOP_INPUT_COMPLETE success=True. Client log has no Exception/error/Failed matches. Visually inspected Logs/phase4-door-hints-door-open-hint.png, -door-held.png and -door-close-hint.png at 1920x1080: Japanese hints are readable without clipping or overlap. Other locales and teammate-holding hint have not had visual acceptance. Runtime/editor whitespace checks pass. Holding progress gauge, remaining item prompts and manual acceptance remain open; Phase 4 is not complete.

Usage handoff: after pushing 0470bfe, five-hour usage reached 81% (weekly 25%). Stop at this validated checkpoint and launch the local Gemma reviewer. Preserve generated Addressables changes and this note uncommitted. Review completion is not verified; launcher receives actual root E:\project-beta, while the review prompt's older path spelling remains a known caveat. Resume with remaining progress/item HUD and manual acceptance.

## Door progress and item handoff HUD (2026-10-02)

Started the Microsoft Store Unity Hub before the build. The local owner now shows a 0.25s E input progress bar while aiming at an online door, with a full bar while holding it. The bar follows local input timing; Host still decides whether the interaction succeeds. Online carried-item HUD now displays E to put down by default, E to hand to the aimed available Player N, or an unavailable message for downed/occupied/out-of-range recipients. Unheld nearby stock shows E to pick up. Four-language localization entries were added. This is a UI change; actual transfer authority remains on the Host.

Logs/phase4-hud-progress-build.log: Windows build passed (99,466,735 bytes). Logs/phase4-hud-progress-host.log: all 31 cooperative input checks pass, GAME1_COOP_INPUT_COMPLETE success=True; Client log has no Exception/error/Failed matches. Visually inspected Logs/phase4-hud-progress-door-progress.png (bar approximately half full at 0.12s) and Logs/phase4-hud-progress-transfer-prompt.png (Player 2 handoff hint), both 1920x1080 Japanese captures. Other locales, unavailable state and manual input have not received visual acceptance. Existing generated Addressables state remains outside the source checkpoint.

## Moving light exposure acceptance (2026-10-02)

Added Host/Client integration cases for a moving player illuminating the mannequin. A moving beam held under two seconds does not Freeze; switching it off for 0.4s resets accumulated exposure; another uninterrupted moving beam freezes after the threshold. The fixture disables enemy navigation and moves the player ±0.35m while aiming at a stationary enemy, isolating light perception from pursuit. This is game-side movement and authority testing, not manual player input or audiovisual quality acceptance.

Logs/phase4-moving-light-build.log: Windows build passed (99,468,283 bytes). Logs/phase4-moving-light-host.log: all 20 boundary checks passed, GAME1_BOUNDARY_COMPLETE success=True. Client log has no Exception/error/Failed matches. No production enemy code changed. Remaining moving-player/collector interaction should be considered during later manual acceptance.

## Ping material and Collector inspection boundary (2026-10-02)

The fallback local Ping sphere and network Ping sphere now share a cyan URP/Lit material; the previous built-in primitive material rendered magenta under URP. Collector switches from Hunt to InspectItem only within the specified 1m boundary instead of 1.4m. The interaction fixture now checks the 1.2m rejection boundary; removing a duplicate Curse noise from its setup restored the intended theft path. Logs/phase4-ping-inspect-qa-retry-build.log passed, and Logs/phase4-ping-inspect-qa-retry-host.log passed all 19 interaction checks, GAME1_INTERACTION_COMPLETE success=True.

The editor BuildWindows command supports an optional GAME1_BUILD_OUTPUT_PATH so QA builds can use Builds/QA/GAME1.exe while the user's regular GAME1 process stays open. Logs/phase4-ping-visual-retry-build.log passed (99,469,295 bytes). Logs/phase4-ping-visual-retry-host.log passed all 32 cooperative input checks, GAME1_COOP_INPUT_COMPLETE success=True, including synthetic Q creating a marker with a URP shader. The Client log shows a transient socket recovery message followed by connect/disconnect and no failing scenario check. Logs/phase4-ping-visual-retry-ping.png was captured, but the marker is not discernible in the frame; visual confirmation of its cyan appearance remains open. The regular Windows build still has the prior version while the user is playing it on the LG monitor. Phase 4 and manual input acceptance remain open.

## Development noise radius display (2026-10-02)

Added the specification's world-space GameplayNoise radius circles for the Editor and Development builds only. F8 toggles the display; the circles expire after 1.5 seconds. This subscribes to Host-authoritative GameplayNoise events and does not feed renderer data back into enemy perception. BuildWindows accepts GAME1_DEVELOPMENT_BUILD=1; the generated Resources material forces URP/Unlit into the player so circles can remain legible in dark scenes.

Logs/phase4-noise-debug-unlit-build.log passed (169,017,898 bytes). The graphical Host log confirms debug-view installation and URP/Unlit materials for Ping, Drop and Door events, then all 32 cooperative input checks pass; its headless Client records a clean connect/disconnect. Logs/phase4-noise-debug-release-build.log passed (99,489,147 bytes), and Logs/phase4-noise-debug-release-solo.log passes both solo enemy checks with no debug-view installation. The camera capture does not frame the circles, so visual appearance and the F8 toggle still need manual acceptance. Generated scene and Addressables changes remain outside this source checkpoint.
