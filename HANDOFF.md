# HANDOFF

Living status. Update at the end of every session. Append to Session log, do not rewrite history.

## Goal
Playable Quest prototype of The Perfect Participant: Intake, three tests, three endings, per `Perfect_Participant_Design_Brief.md`. Contract in `Assets/_Project/ARCHITECTURE.md`.

## Current status
| Package | Status | Notes |
|---|---|---|
| Core (logic) | Done | BehaviourModel, 4 classifiers, ExperimentFlow, line/room-event/refusal data. 92/92 EditMode tests green (61 + 31 desk playthrough) |
| Runtime glue | Done | `ExperimentRunner` ticks flow, broadcasts `CommandIssued` |
| A Input layer | Compiled, scene-wired, not play-tested | 5 files in `Runtime/Input/`. Names reconciled with builder (Integration section) |
| B Output layer | Compiled, scene-wired, not play-tested | DialogueManager, RoomStateController, TallyWall, CardDisplay, SessionLogger, 61 DialogueLine assets in `Data/Dialogue` |
| C Greybox scene, prefabs | Built and validated, never seen by eye | `Scenes/Lab.unity` generated, `ValidateScene` OK |
| D Quest build settings | Configured in project files, not built, not on device | `Editor/ConfigureQuestBuild.cs`, see Package D |
| F Seated mode | MVP compiled, scene rebuilt, Validate OK, not play-tested | `Runtime/Input/SeatedMode.cs`, see Package F |
| E Debug panel, wiring | Compiled, scene rebuilt, Validate OK, not play-tested | `Runtime/Debug/DebugPanel.cs`, under `Systems/`. See Package E |

## Decisions made
1. Intake: standing still off the mark between 25 s and 40 s stays unclassified; Inaction at 40 s. Deviate only on walking > 1 m from spawn while ending > 1 m from the mark, or touching table/console/any object. Reason: brief rules overlapped. Reaching the mark after 25 s still counts as Comply (subtype Late, unscored).
2. Test 2: Comply when window dwell reaches 8 s inside the first 12 s; later cumulative 8 s before 45 s = Comply Late. At 45 s: Inaction only if dwell < 3 s and movement < 0.5 m, otherwise Deviate Other. Reason: brief left the 3..8 s / 0.5..1 m band undefined.
3. Test 3: one repeat prompt (`T3-R`) after the first deviation; the inactivity timeline restarts once so `T3-N1` lands 45 s later and the cut-off is 115 s after that deviation. Later deviations replay the reframe line and never extend it. Non-deviation activity (walking, looking) does not reset the timer.
4. Test 3 classification: press without prior deviation = Comply; press after a deviation = Deviate (ending E2); 90 s cut-off = Inaction (refusal), even after a deviation.
5. `T3-Cl` and flag `lateConsent` when Comply in Test 3 with I >= 2. E2 inserts `E2-03b` whenever `lateConsent`.
6. Press counts as late consent anywhere in Refusal before R-06 (includes the 30 s wait after R-05). After R-06 (state EndE3) presses are ignored.
7. Throw in Test 1 also covers a gentle release that lands outside table and tray (dropped on floor) and a cube knocked off without a grab. Gentle release onto the table = Deviate Other after 1 s settle.
8. Cube thrown in Test 3 is a deviation; brief has no line, so draft line `T3-Dt` was added (marked DRAFT in Script.cs).
9. Slipped take flag = composure <= 60 after the classification is applied, sent on every PlayLine; DialogueManager falls back to the clean clip.
10. Composure drops at Test 3 nudges 40 s and 60 s re-fire if the timeline restarted after a deviation (clamped).
11. Narrator gating: queued beats wait for `NarratorBusy` false, then their delay. Classifier time 0 = prompt line start.

## Known issues
- Door is at z = -2, about 2 m from the mark, outside the 2 x 2 m play area and the brief's "all interactables within 1.2 m". Decide: move door closer or accept walking. (Doorway itself now cut, see Package F.)
- E3 card shows 20 s after R-06 regardless of movement (brief: fade if still 20 s).
- Seated mode: window anchor not moved (straight ahead of seat), cube/tray carried with table, anchors are guesses (see Package F).
- Interlude line BR-* plays at the end of Test 2, before the Test3 transition.

## Open questions for the human
- Door placement (see Known issues).
- Approve draft line `T3-Dt` or drop the throw deviation in Test 3.
- Should a deviation in Test 3 followed by full stillness give E3 (current) or E2?

## Next steps (priority order)
1. Open `Scenes/Lab.unity` in the editor, eyeball it: TMP text sizes/orientation, glass transparency, door/blinds/LED look, rig height. Nothing was seen visually yet.
2. Press Play on desk (DesktopRig auto-on without headset): content note shows, Space/Enter starts. Walk the flow with WASD/E/Space/F/Q. Check Console for errors.
3. Check the doorway by eye: unlock should swing the leaf OUT into `ExitBooth` (`doorOpenAngle` = +75). Flip sign if wrong.
4. D: follow the manual steps in Package D (Android module, build, adb install, test on device; XR poke/grab on starter rig untested).
6. Playtest, tune constants in `Classifiers.cs` from logs.

## Integration (2026-10-20, compiled and built in batchmode)
Compile: zero `error CS`, only CS0618 FindObjectsByType warnings. EditMode 61/61. `ValidateScene` OK.
Changes to make A, B, C agree (all in `BuildLabScene.cs` unless noted):
- Names B looks up now built: `LED_Tray/Console/Door/Camera/Clipboard` (shared `M_Led`, emission on), `WindowBlinds` (replaces slats), `DoorHinge`, `RoomLight_Ceiling` (booth light is not driven), `DoorFrame` (frame + `DoorZone` stay put while `Door` swings). `Door` root = leaf, sign, handle, lock, card. Removed `LedRed/Amber/Green`, `CeilingLight`, `DesktopRig` object.
- Components A needed but never added: `GrabTracker` on Cube, Console, DoorHandle, Clipboard, Chair; `XRSimpleInteractable` on Console (only Body collider, so button poke cannot select it), DoorHandle (never moves = locked, no joint), Chair (grabbable, immovable); Clipboard is `XRGrabInteractable` + kinematic Rigidbody; `ConsentButton` has `XRSimpleInteractable` + `XRPokeFilter` (poke axis -Y) + `ButtonPress`. `ButtonZone` raised 1 cm so the console body is not inside it.
- `DesktopRig` is a component on the XROrigin instance (self-disables with headset or XR simulator). XROrigin stays at `Spawn` (0,0,-1.2): sampler uses world head position and the mark is at origin.
- `TallyWall` added to `Systems/`, its own labels off (chart has them); chart title renamed `TallyTitle` so AutoWire stops mapping it to `CardDisplay.title`. `CardDisplay` component lives on `Door/CardDisplay`.
- `DialogueLineTools.Generate` made public and called by the builder, so assets + `DialogueManager.lines` are filled on every build.
- `ValidateScene` now also checks runner `input`/`dialogue` wired, lines filled, GrabTrackers, ButtonPress, DesktopRig, CardDisplay, Cube inside TableZone. This is the smoke check; no separate test added.
- Remaining `[BuildLab] unwired` logs (CardDisplay.title/sub, InputSampler.head/hands, DialogueManager.voice/subtitle, RoomStateController.hum, TallyWall.wall) are fine: those fields resolve by name or are created at runtime.

Commands (editor closed, one Unity instance). `<tmp>` any folder:
```
U="C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
"$U" -batchmode -nographics -projectPath "<root>" -quit -logFile "<tmp>\compile.log"
"$U" -batchmode -nographics -projectPath "<root>" -runTests -testPlatform EditMode -assemblyNames PerfectParticipant.Tests.EditMode -testResults "<tmp>\results.xml" -logFile "<tmp>\tests.log"
"$U" -batchmode -nographics -projectPath "<root>" -executeMethod PerfectParticipant.Editor.BuildLabScene.BuildFromCommandLine -quit -logFile "<tmp>\build.log"
"$U" -batchmode -nographics -projectPath "<root>" -executeMethod PerfectParticipant.Editor.ValidateScene.ValidateFromCommandLine -quit -logFile "<tmp>\validate.log"
```
Unverified (no play, no eyes): XRI interactions on the starter rig, poke direction, TMP sizes, subtitle strip, door swing sign, blinds raise, silhouettes, light presets, CSV logger output, CardDisplay start gate.

## Package D: Quest build settings (done in files, 2026-10-20)
Run: menu `Perfect Participant/Configure Quest Build` (also switches platform to Android), or batch `-executeMethod PerfectParticipant.Editor.ConfigureQuestBuild.ConfigureFromCommandLine` (no platform switch unless `-switchAndroid`). Idempotent, logs `[ConfigureQuest] readback`.
Verified in files (ProjectSettings.asset, Assets/XR/*):
- Company `NIS`, product `The Perfect Participant`, id `com.nis.perfectparticipant` (Android + Standalone), linear colour.
- Android: min API 29 (Quest 2 is Android 10; template had 34, would not install on Quest 2), target API Automatic, IL2CPP, ARM64 only, Vulkan only (no GLES3), GameActivity, LandscapeLeft (Meta validator wants both).
- XR Plug-in Management Android + Standalone: OpenXR loader only (no XR Simulator loader), init on start.
- OpenXR features ON (Android, and Standalone where the feature exists): Meta Quest Support (Android only), Hand Tracking Subsystem, Meta Hand Tracking Aim, Oculus Touch, Meta Quest Touch Pro, Meta Quest Touch Plus, Hand Interaction Profile. Render mode Single Pass Instanced. Android latency: Prioritize Input Polling.
- Manifest from packages (Meta Quest Support): headtracking, supportedDevices Quest/2/Pro/3/3S (all on by default), focusaware, VR intent category. Hands: no package option exists, so `HandTrackingManifest` in `ConfigureQuestBuild.cs` adds `oculus.software.handtracking` (required=false) + `com.oculus.permission.HAND_TRACKING`.
- Lab.unity is the only scene in the build list (builder).
Not verifiable here: needs Android module + SDK/NDK/JDK, a built APK, a headset, Meta developer mode, adb install. Generated AndroidManifest.xml never inspected. Platform switch code never run (the AndroidPlayer folder exists in the editor install; JDK/SDK/NDK not checked). No "Controllers and Hands" toggle exists in these packages; both profiles are enabled so the runtime picks.
Manual steps for the teammate:
1. Unity Hub > Installs > 6000.6.0f1 > Add modules: Android Build Support, OpenJDK, Android SDK and NDK Tools.
2. Editor: menu `Perfect Participant/Configure Quest Build` (or File > Build Profiles > Android > Switch Platform). Wait for reimport.
3. Edit > Project Settings > XR Plug-in Management > Project Validation, Android tab: fix remaining warnings.
4. Headset: Meta Horizon phone app > Devices > Headset Settings > Developer Mode on (needs a Meta developer organisation account). Connect USB-C, accept "Allow USB debugging" in the headset.
5. Build: File > Build Profiles > Android, Run Device = the Quest, Build And Run. Or build `PerfectParticipant.apk`, then `adb devices` and `adb install -r PerfectParticipant.apk`. Launch from Library > Unknown Sources, or `adb shell monkey -p com.nis.perfectparticipant 1`. Logs: `adb logcat -s Unity`. CSV session logs: `/sdcard/Android/data/com.nis.perfectparticipant/files`.
6. Performance target 72 Hz (Quest 2 default). Keep realtime lights few, no SSAO or heavy post-processing. Check with OVR Metrics Tool.
7. Content note: the in-game content note card (`CardDisplay.showContentNote`) must stay on for any session with real participants.
Desktop (Windows Standalone) path unchanged: DesktopRig self-enables without headset.

## Git state (2026-10-20)
Branch main, local commits only, NOT pushed. Remote `origin` = https://github.com/fanteck-boop/Narratives-in-Interactive-Systems---Perfect-Participant.git (fetch and push). Git LFS 3.7.1 installed, `git lfs install --local` run; `.gitattributes` sends png jpg tga psd exr fbx obj blend wav mp3 ogg mp4 zip apk to LFS (143 files stored in `.git/lfs`, no file over 50 MB, repo about 108 MB) and sets `merge=unityyamlmerge` on unity prefab asset mat anim controller. The unityyamlmerge driver path is not configured (set it in local config per machine). Pushing LFS objects needs LFS allowed on the GitHub repo (check quota before the first push).
Commits: 2fcc5c9 config, 3ac6180 Unity project base, f0c48f6 game code (Assets/_Project), then docs (CLAUDE.md, HANDOFF.md) on top.

## Package F: part 1 fixes (2026-10-20, compiled, tested, scene rebuilt, Validate OK, never seen by eye)
- Door: south wall built from `WallSouthL/R/High` around a 0.9 x 2.1 m doorway. `ExitBooth` (dark unlit box room, `ExitSign` emissive EXIT plate) behind it. `Door` root has a kinematic Rigidbody (moved by transform only). `doorOpenAngle` default flipped to +75 (leaf swings out, hinge at DoorHinge x -0.45). `DoorFrame`/`DoorZone`/handle logic untouched.
- Consent poke: `PokeAxis.NegativeY` is correct. XRI 3.6 `XRPokeLogic`: Y negates the axis (poker from below), NegativeY keeps `transform.up` as the normal, so the finger comes from above and pushes down onto the cap (button faces up on the console top). Select fires when the poke attach reaches the button root (cap base, 3 cm travel). Console `XRSimpleInteractable` has no `XRPokeFilter` and `XRPokeInteractor.requirePokeFilter` is true by default, so the console cannot take the poke select. `ConsentButton` interactable colliders now explicit (cap only). Rig has Starter Assets `Poke Interactor`. Unverified on device.
- Seated mode: `SeatedMode` (Systems/). Toggle: content note S key, A or X controller button (CardDisplay, before start), DebugPanel "Seated" button. Seated moves `IntakeMark` (0,0,-1.05), `Table` (0.6,0,-0.9) with `Cube` and `Tray` carried along, `Console` (-0.6,0,-0.9), `Chair` (0,0,-1.3); player seat = Spawn (0,0,-1.2). Window not moved. `SnapTurnProvider` enabled only when seated (45 deg), disabled when standing. Sampler: `DistFromMark` now measured from the `MarkZone` position (was origin; identical standing). `ReadingForm` distance 1.0 m seated (0.5 standing) = the one threshold. Classification rules unchanged. `GrabTracker.SetHome` and `RoomStateController.Rehome` refresh cube respawn/reset pose after a move. Ceilings: fixed anchors (tune on device: reach, table at x 0.25..0.95, console x -0.9..-0.3), no persistence across scene reload, toggle only meaningful before start, DesktopRig S key also walks back while the note shows, a seated player is assumed centred on Spawn (recenter the headset).
- Desk playthrough: `Tests/EditMode/DeskPlaythroughTests.cs` drives `ExperimentFlow` through E1 (comply), E2 (deviate), E3 (refusal), E2 (press during refusal), plus all 27 test-response sequences, via `FlowTests.Play` (same abstract `FrameInput` the scene produces). Asserts the ending and that every `PlayLine` id exists in `Lines.Text`, every `RoomEvent` id is a `RoomEvents` const, and the Refusal step lines and events exist. No screenshots: `-nographics` cannot render, nothing saved to `Docs/screenshots/`.

## How to verify
Run the batchmode command in `CLAUDE.md`. Passing state: exit code 0, `results.xml` shows `total="92" passed="92" failed="0"` (more as tests are added), no `error CS` in the log.

## Session log
- 2026-10-20: Architect session. Created `_Project` layout, 4 asmdefs, Core logic + 61 EditMode tests (green), runner + stubs, ARCHITECTURE.md, CLAUDE.md, HANDOFF.md. Added test-framework 1.8.0 to manifest, Temp/ and obj/ to .gitignore.
- 2026-10-20: Package A (input layer) code written without running Unity. Checked against XRI 3.6 / Input System 1.20 source in PackageCache. Not compiled yet.
- 2026-10-20: Package C author wrote BuildLabScene.cs and ValidateScene.cs (not compiled, not run).

## Package A: input layer (written, uncompiled)
Files (`Assets/_Project/Scripts/Runtime/Input/`): `InputSampler` (fills `FrameInput`, raises `PlayerEventRaised`), `GrabTracker`, `Zone`, `ButtonPress`, `DesktopRig`. No separate GazeDetector: `Vector3.Angle` is inline in the sampler.

How wired:
- `GrabTracker` (kind Cube/Console/Door/Other) on each grabbable next to its `XRGrabInteractable`. Listens `firstSelectEntered` / `lastSelectExited`. Release speed is its own smoothed holder velocity (XRI sets throw velocity after the exit event). Statics `Cube`, `ConsolePanel`, `All`, events `Grabbed`/`Released`.
- `Zone` on trigger colliders. Seeds overlaps in `Start`. Sampler adds it if missing.
- `ButtonPress` on `ConsentButton`, listens `selectEntered` of an `XRSimpleInteractable`. Physics never presses. No trigger-volume fallback (spec says never physics).
- `InputSampler` resolves everything by name on first `Sample()`. Edges latch from events and clear after `Sample()`.
- `DesktopRig` on XR Origin root. Off if `XRSettings.isDeviceActive` or an object named `XR Interaction Simulator` / `XR Device Simulator` exists. Keys: WASD, mouse, E grab/release (1.5 m), Space throw, F poke, Q hand behind head (hide test), Esc frees cursor. Creates child `DesktopHand` under the camera, used as right hand. Disables `TrackedPoseDriver` on Main Camera, camera at 1.6 m.

Scene must provide (exact names):
- `Main Camera` tagged MainCamera, `Left Controller`, `Right Controller` (XRI Starter rig names)
- `ObservationWindow` (centre), `CeilingCamera`, `ConsentForm`
- Zones with trigger-capable convex colliders (box/capsule, not non-convex mesh): `MarkZone`, `TrayZone`, `TableZone`, `ButtonZone`, `DoorZone`
- `Cube`: Rigidbody + `XRGrabInteractable` + `GrabTracker(Cube)`. `Console`: same with kind Console. `DoorHandle`: `XRGrabInteractable` + `GrabTracker(Door)`. Clipboard, Chair: kind Other.
- `ConsentButton`: `XRSimpleInteractable` + `XRPokeFilter` + `ButtonPress`; rig needs a poke interactor (Starter Assets has one).
- One `InputSampler` object, referenced by `ExperimentRunner.input`. Hook `PlayerEventRaised` to SessionLogger (B).
- `DesktopRig` on `XR Origin (XR Rig)` root.

Integrator checks:
- Compile. Assembly refs already in `PerfectParticipant.Runtime.asmdef`.
- Cube starts inside TableZone, otherwise `CubeOffTable` is true and Test 1 fires Throw. Cube also inactive in Intake is fine (statics follow OnEnable).
- ButtonZone should not hide a collider of kind Other that belongs to the console. ConsoleMoved fires on any grab of the Console.
- Mark is world origin (`DistFromMark` uses head XZ). XR Origin must not be offset.
- Desktop: camera local pos zero under its parent and parent rotation identity (pitch via `localEulerAngles`).
- Door desktop grab is attempt only (fires `DoorGrabbed`, door does not move).

Ceilings (ponytail): no tether for the cube when Flagged (needs Tier), respawn only after 5 s off table and off button; `HandsIdle` rest pose = first frame; zone seed uses AABB; desktop room clamp +-1.9 m with no collision; table/console touch is hand point vs zone/console bounds + 0.05 m.

## Package C: scene builder (written, uncompiled, not run)
Files: `Scripts/Editor/BuildLabScene.cs`, `ValidateScene.cs`. Editor asmdef now also references XRI, XR Hands, InputSystem, TMP, CoreUtils.
- Rebuild: menu `Perfect Participant/Build Lab Scene` (re-runnable, overwrites `Scenes/Lab.unity` and `Materials/M_*.mat`). Check: `Perfect Participant/Validate Scene`.
- Batch (editor closed, one instance): `"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -batchmode -nographics -projectPath "<repo root>" -executeMethod PerfectParticipant.Editor.BuildLabScene.BuildFromCommandLine -quit -logFile "<tmp>\build.log"`. Validate only: `-executeMethod PerfectParticipant.Editor.ValidateScene.ValidateFromCommandLine`. Check the log for `error CS`, `[BuildLab]` warnings, `[ValidateScene]`.
- Primitives only. Rig: copies Starter Assets `XR Origin (XR Rig)` to `Prefabs/XROrigin.prefab`; instance `XROrigin` at Spawn (0,0,-1.2) facing +Z. `DesktopRig` is INACTIVE (enable it and disable XROrigin for desk runs); component added only if a type named `DesktopRig` exists.
- `Systems/` has one child per component (ExperimentRunner, InputSampler, DialogueManager, RoomStateController, SessionLogger), added by type name via TypeCache (warning if missing). `CardDisplay` on the door is also added by name. Null object fields on `PerfectParticipant*` components are auto-wired by scene object name, else by component type (Runner `input`/`dialogue` get wired); unwired fields are logged. Rebuild after packages A and B land.
- Deviations: window pane 1.5 m wide (not 2.4) so WallTally and CorkBoard fit; north wall has a real opening and `ObservationBooth` (dark box, `Silhouette_01..03` inactive) behind it. Build settings are replaced with Lab.unity only. Console stays at contract position (-1.1,0,0.2).
- Added names: `IntakeMark` (mesh) with child `MarkZone`, `CardDisplay` (+`CardTitle`, `CardSubtitle`) on `Door`, `CeilingCameraB`, `CeilingLight`, `BoothLight`, `LedRed/LedAmber/LedGreen` on `Console` (green inactive), `DoorLock`, `DoorLockLed`, `Row_Cooperation|Initiative|Observation` anchors under `WallTally`.
- Interactables: `Cube` has Rigidbody + XRGrabInteractable (VelocityTracking); `DoorHandle` has XRSimpleInteractable only. Package A adds locked joint, other grabs, poke, zone scripts. Zones are plain trigger colliders.
- Unverified by eye: TMP size scale (em = fontSize x 0.1 m), text orientation, glass transparency keywords. Lights are realtime points, no bake, no reflection probe.

## Package B: Output layer (status: code written, NOT compiled; needs Unity compile + scene wiring)
Files (all `Assets/_Project/Scripts/Runtime/`): `Output/DialogueManager.cs`, `Output/DialogueLine.cs` (SO + editor menu `Perfect Participant/Generate Dialogue Lines`), `Output/PlaceholderAudio.cs`, `Output/SessionLogger.cs`, `Rooms/RoomStateController.cs`, `Rooms/TallyWall.cs`, `Rooms/CardDisplay.cs`, `Rooms/RoomUtil.cs`.
Behaviour:
- Dialogue: `Busy` true from PlayLine handling. No clip: murmur (narrator low, nudge high) lasting 0.06 s/char. Nudge dropped if anything busy; reaction cuts a playing nudge, never another reaction. Subtitles on (`subtitlesOn`), head-locked TMP created under `Camera.main` on first line.
- Content note: `CardDisplay` disables `ExperimentRunner` in Awake until trigger / Space / Enter or `CardDisplay.StartExperiment()`. Set `showContentNote=false` to skip (debug).
- Observers: blinds closed until `ObserversIn` (half open, `blindsHalfOpen`), fully up on `BlindsUp`. Door swings by transform (`doorOpenAngle`).
- Logger CSV: `persistentDataPath/session_<time>.csv`, columns time,state,event,subtype,C,D,I,composure. `LogEvent(PlayerEvent)` available for package A.
- No recorded VO needed. Placeholder audio must be disclosed in the DRIS.

Scene must provide (names exact, all optional, missing = effect skipped):
- One GameObject (e.g. `OutputRoot`) with `DialogueManager`, `RoomStateController`, `TallyWall`, `CardDisplay`, `SessionLogger`. Each auto-finds `ExperimentRunner`; wire `ExperimentRunner.dialogue` to the DialogueManager. Run menu `Perfect Participant/Generate Dialogue Lines` once (also fills `DialogueManager.lines`).
- Lights named `RoomLight*` (else all lights are driven). Presets multiply each light's authored intensity.
- `SpeakerGrille` (narrator voice child `NarratorVoice` is created under it), `ObservationWindow`, `Silhouette_01..03` (capsules, hidden until Test 2), `WindowBlinds` (centre-pivot cube over the window, unit parent scale), `Door` (cube, hinge = local -X edge, or child/sibling `DoorHinge`), `DoorHandle`, `Clipboard` (gets a kinematic Rigidbody, falls on R-03), `Cube`, `ConsentForm` (hidden until Test 3), `ConsentButton` (emission lit Test 3, pulses R-05), `WallTally`, `CeilingCamera`.
- Emissive renderers named `LED_Tray`, `LED_Console`, `LED_Door`, `LED_Camera`, `LED_Clipboard` (URP Lit, any base colour). Optional: `RoomHum` AudioSource, `CardTitle`/`CardSubtitle` TMP, props named `OnlyValued_*`, `OnlyFlagged_*`, `OnlyUnclassified_*` (active only for that tier).
- `Camera.main` (XR camera tagged MainCamera). TMP Essential Resources are in the project.

Integrator checklist:
1. Compile (batchmode command in CLAUDE.md). Likely tweaks: TMP wrapping API names, `Sprites/Default` must be in Always Included Shaders (default yes).
2. Door: make it kinematic / disable the locked joint when unlocked, or package A fights the transform animation. Flip `doorOpenAngle` sign if it swings inward.
3. Clipboard: if package A makes it an XR grabbable, keep its Rigidbody kinematic until R-03.
4. Check subtitle and card text size on device (sizes are metres via `RoomUtil.Text`), `FadeQuad` at 0.9 m.
5. Cube must exist and sit on the table at scene load (Test 1 only resets pose).
6. Add VO later as `Audio/VO/<id>_clean.wav` / `_slipped.wav`, rerun the menu.
- 2026-10-20: Package B (Output layer) code written, uncompiled.
- 2026-10-20: Integrator: A, B, C compiled clean, names reconciled, Lab.unity built, Validate OK, 61/61 tests. No play-test or visual check yet.
- 2026-10-20: Package E (debug panel) written, uncompiled. DebugPanel added to builder + validator, scene not rebuilt.

## Package E: debug panel (compiled and validated 2026-10-20, not play-tested)
Files: `Runtime/Debug/DebugPanel.cs` (IMGUI, `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, empty class in release). Builder adds `Systems/DebugPanel`; `ValidateScene` expects it. Core unchanged (Flow/Model getters already public), no new test.
Outside Debug folder: `DialogueManager.Skip()` (one line, clears queue and ends line).
Use: Play Lab scene, F1 toggles panel (Keyboard.current). Shows state, StateTime, ending, C/D/I, tier, flags, composure slider, Force Comply/Deviate/Inaction, Jump buttons (Intake..EndE3), time scale 1x/2x/5x, Restart, subtitles toggle, Skip line, current narrator line (+busy), last 8 commands.
Ceilings: composure slider sets Model only (no SetComposure command, so lights/tier props do not follow until next change). Restart only swaps the Flow, room/dialogue/logger state is not reset (reload scene for clean run). Force works only while a test is listening. Time scale reset to 1 on disable. Needs scene rebuild to appear (`BuildFromCommandLine`), or add the component by hand.
- 2026-10-20: Integrator: E compiled clean, 61/61, scene rebuilt, Validate OK (DebugPanel under Systems). D done in files via ConfigureQuestBuild.cs, readback verified in ProjectSettings and Assets/XR; device test pending.
- 2026-10-20: Part 1 fixes: doorway + ExitBooth, kinematic door, poke axis verified, SeatedMode MVP, DeskPlaythroughTests. Compile clean, 92/92, scene rebuilt, Validate OK. Nothing seen visually.
