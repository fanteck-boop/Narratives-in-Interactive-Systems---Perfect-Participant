# HANDOFF

Living status. Update at the end of every session. Append to Session log, do not rewrite history.
Read order for a fresh Claude: this file, `CLAUDE.md`, `Assets/_Project/ARCHITECTURE.md`. Brief only when needed: `Perfect_Participant_Design_Brief.md`.

## Goal
Playable Quest prototype of The Perfect Participant (course NIS 2026, AAU): Intake, three tests, three endings, per the design brief. A polite narrator reframes every player action as cooperation; only doing nothing breaks it. Code contract: `Assets/_Project/ARCHITECTURE.md`. Rule: all gameplay logic in `PerfectParticipant.Core` (pure C#, unit tested); Unity layer only fills `FrameInput` and turns `Command`s into effects.

## Current status (as of 2026-10-20)
All packages A to F are written and integrated. Compile clean (zero `error CS`, only CS0618 FindObjectsByType warnings). 92/92 EditMode tests green. `Scenes/Lab.unity` builds in batchmode and `ValidateScene` reports OK. Committed. Pushed state: see Git state. NOBODY HAS OPENED Lab.unity OR PLAYED IT. Everything visual, XR and audio is unverified.

| Package | Status | Where |
|---|---|---|
| Core (logic) | Done, 92/92 tests (61 unit + 31 desk playthrough) | `Scripts/Core/` |
| Runtime glue | Done | `ExperimentRunner` ticks flow, broadcasts `CommandIssued` |
| A Input layer | Compiled, scene-wired, not play-tested | `Scripts/Runtime/Input/` (InputSampler, GrabTracker, Zone, ButtonPress, DesktopRig, SeatedMode) |
| B Output layer | Compiled, scene-wired, not play-tested | DialogueManager, RoomStateController, TallyWall, CardDisplay, SessionLogger, 61 DialogueLine assets in `Data/Dialogue` |
| C Greybox scene | Built, validated, never seen by eye | `Scenes/Lab.unity` via `Editor/BuildLabScene.cs` + `ValidateScene.cs` |
| D Quest build settings | Configured in project files, no APK built, not on device | `Editor/ConfigureQuestBuild.cs` |
| E Debug panel | Compiled, scene rebuilt, Validate OK, not play-tested | `Runtime/Debug/DebugPanel.cs` (F1) |
| F Part 1 fixes + seated mode | Compiled, tested, scene rebuilt, Validate OK, not play-tested | doorway + ExitBooth, kinematic door, poke axis, `SeatedMode`, `DeskPlaythroughTests.cs` |

## Decisions made
1. Intake: standing still off the mark between 25 s and 40 s stays unclassified; Inaction at 40 s. Deviate only on walking > 1 m from spawn while ending > 1 m from the mark, or touching table/console/any object. Reaching the mark after 25 s still counts as Comply (subtype Late, unscored). Reason: brief rules overlapped.
2. Test 2: Comply when window dwell reaches 8 s inside the first 12 s; later cumulative 8 s before 45 s = Comply Late. At 45 s: Inaction only if dwell < 3 s and movement < 0.5 m, otherwise Deviate Other. Reason: brief left the 3..8 s / 0.5..1 m band undefined.
3. Test 3: one repeat prompt (`T3-R`) after the first deviation; the inactivity timeline restarts once so `T3-N1` lands 45 s later and the cut-off is 115 s after that deviation. Later deviations replay the reframe line and never extend it. Walking or looking does not reset the timer.
4. Test 3 classification: press without prior deviation = Comply; press after a deviation = Deviate (ending E2); 90 s cut-off = Inaction (refusal), even after a deviation.
5. `T3-Cl` and flag `lateConsent` when Comply in Test 3 with I >= 2. E2 inserts `E2-03b` whenever `lateConsent`.
6. Press counts as late consent anywhere in Refusal before R-06 (includes the 30 s wait after R-05). After R-06 (state EndE3) presses are ignored.
7. Throw in Test 1 also covers a gentle release outside table and tray (floor) and a cube knocked off without a grab. Gentle release onto the table = Deviate Other after 1 s settle.
8. Cube thrown in Test 3 is a deviation; brief has no line, so draft line `T3-Dt` was added (marked DRAFT in `Script.cs`).
9. Slipped take flag = composure <= 60 after the classification is applied, sent on every PlayLine; DialogueManager falls back to the clean clip.
10. Composure drops at Test 3 nudges 40 s and 60 s re-fire if the timeline restarted after a deviation (clamped).
11. Narrator gating: queued beats wait for `NarratorBusy` false, then their delay. Classifier time 0 = prompt line start.
12. Android min API 29 (Quest 2 is Android 10; template had 34). Vulkan only, ARM64, IL2CPP, OpenXR only (no XR Simulator loader in build).
13. Consent poke: `PokeAxis.NegativeY` (finger from above pushes the cap down). Console has no `XRPokeFilter`, so it cannot steal the poke.
14. Seated mode: fixed anchors (see Package F). Classification rules unchanged; only `ReadingForm` distance (1.0 m seated, 0.5 standing) and `DistFromMark` origin (MarkZone position) differ.
15. Door: south wall has a real 0.9 x 2.1 m doorway; leaf swings OUT into `ExitBooth` (`doorOpenAngle` = +75, by geometry only).

## Known issues
- Door is at z = -2, about 2 m from the mark: outside the 2 x 2 m play area and the brief's "all interactables within 1.2 m". Decision pending: move the door closer or accept walking.
- Desktop: S key (seated toggle) also walks the rig back while the content note shows.
- Composure slider in DebugPanel sets Model only (no SetComposure command): lights and tier props do not follow until the next change.
- Debug Restart swaps only the Flow; room, dialogue and logger state are not reset (reload the scene for a clean run).
- Cube is not tethered in the Flagged tier (needs Tier in the input layer). Respawn is only after 5 s off table and off button.
- E3 card shows 20 s after R-06 regardless of movement (brief: fade if still 20 s).
- Seated mode: window anchor not moved, cube/tray carried with table, anchors are guesses, no persistence across scene reload, toggle only meaningful before start, a seated player is assumed centred on Spawn.
- Interlude line BR-* plays at the end of Test 2, before the Test 3 transition.
- `HandsIdle` rest pose = first frame; zone seed uses AABB; desktop room clamp +-1.9 m with no collision.
- Remaining `[BuildLab] unwired` logs (CardDisplay.title/sub, InputSampler.head/hands, DialogueManager.voice/subtitle, RoomStateController.hum, TallyWall.wall) are fine: they resolve by name or are created at runtime.

## Open questions for the human
- Door placement (see Known issues).
- Approve draft line `T3-Dt` or drop the throw deviation in Test 3.
- Should a deviation in Test 3 followed by full stillness give E3 (current) or E2?
- Who records the narrator, and which voice goes in the DRIS 4b disclosure?
- Real submission deadline (not in the Moodle docs we have).

## Next steps (priority order)
OPEN items from the last session, in order:
1. NOBODY HAS OPENED Lab.unity OR PLAYED IT. Open it in the editor, press Play on desk (DesktopRig auto-on without headset), open the F1 debug panel and do a desk run through all three tests. Check by eye: TMP text size and orientation (sizes are metres via `RoomUtil.Text`, em = fontSize x 0.1 m), glass transparency, light presets, subtitle strip, content-note start gate (Space/Enter starts), blinds raise, silhouettes appear in Test 2, LEDs, tally wall, rig height. Check Console for errors. Keys: WASD, mouse, E grab/release, Space throw, F poke, Q hand behind head, Esc frees cursor.
2. Door swing direction and ExitBooth look are unverified. `doorOpenAngle` +75 is by geometry only. On unlock the leaf should swing OUT into `ExitBooth`. Flip the sign on `RoomStateController` if it swings inward or clips.
3. Real Quest test: grab and poke on the Starter Assets rig (cube, console, clipboard, chair, door handle locked), consent button poke (poke attach must reach the cap base, 3 cm travel), seated anchors are guesses (reach, table x 0.25..0.95, console x -0.9..-0.3), `SnapTurnProvider` enabled only when seated, recenter the headset so the seated player sits on Spawn.
4. Android build chain: Unity Hub > Installs > 6000.6.0f1 > Add modules: Android Build Support, OpenJDK, Android SDK and NDK Tools. Switch platform (menu `Perfect Participant/Configure Quest Build` or File > Build Profiles). Meta developer mode on the headset (needs a Meta developer org account), USB debugging, `adb devices`. `HandTrackingManifest` (adds `oculus.software.handtracking` required=false and `com.oculus.permission.HAND_TRACKING`) is untested and the generated AndroidManifest.xml was never inspected. Fix the XR Plug-in Management > Project Validation Android warnings. Target 72 Hz (Quest 2 default); check with OVR Metrics Tool. See Package D for the full steps.
5. Narrator voice: only placeholder generated audio exists (murmur, 0.06 s/char). About 65 recorded takes are needed; the list lives in `Perfect_Participant_Design_Brief.md`, line ids in `Scripts/Core/Script.cs`. Record a human voice, disclose it in DRIS 4b, drop clips in `Assets/_Project/Audio/VO/<id>_clean.wav` and `<id>_slipped.wav`, then run menu `Perfect Participant > Generate Dialogue Lines`. Slipped takes only matter at composure <= 60.
6. Fix known flaws: S key walks the rig back while the content note shows on desktop; composure slider does not drive lights (add a SetComposure command or call the room controller); Restart does not reset room/dialogue/logger; cube not tethered in Flagged tier; door about 2 m from the mark, outside the 1.2 m reach rule (decision pending).
7. Opus-level code review of the whole build has not been done. Do it before the final hand-in (Core is tested; Runtime, Editor and scene wiring are not reviewed).
8. Course items:
   - DRIS section 5 needs real tool versions (see CLAUDE.md Versions) and 2 to 3 screenshots (none exist; `-nographics` cannot render, `Docs/screenshots/` is empty).
   - DRIS 4b must state what the group actually used: Claude for design, code and docs; placeholder generated audio (or the recorded voice, once done).
   - 23 Oct slides: slide 6 prototype status is an [EDIT] placeholder, fill it from this file.
   - Deadline is not in the Moodle docs we have. Check Moodle.
9. Playtest with people, then tune constants in `Classifiers.cs` from the CSV logs. The content note (`CardDisplay.showContentNote`) must stay on for any session with real participants.

## Git state
Remote `origin` = https://github.com/fanteck-boop/Narratives-in-Interactive-Systems---Perfect-Participant.git, branch `main`. Git LFS 3.7.1 installed, `git lfs install --local` done; 159 LFS-tracked files. `.gitattributes` sends png jpg tga psd exr fbx obj blend wav mp3 ogg mp4 zip apk to LFS and sets `merge=unityyamlmerge` on unity prefab asset mat anim controller; the unityyamlmerge driver path is NOT configured (set it per machine in local git config, path to UnityYAMLMerge.exe in the editor install).
Commits (oldest first): 51f7beb Initial commit, 25b3752 Memory files for narratives, 2fcc5c9 gitignore/gitattributes/vsconfig, 3ac6180 Unity project base (packages, settings, template, samples), f0c48f6 game code, tests, Lab scene, materials, dialogue data, 91d4774 CLAUDE.md and HANDOFF.md. Later commits (HANDOFF rewrite, push record) are in the Session log; run `git log --oneline` for the truth. Push status: see the last Session log line.

### Git workflow rules for the team
- Large binaries go through LFS (already tracked by extension). After cloning run `git lfs install` then `git lfs pull`. Check GitHub LFS quota before pushing big assets (audio takes are wav, so LFS).
- NEVER commit `Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.sln` (already in .gitignore; do not force-add).
- ALWAYS commit `.meta` files with their assets. A missing .meta breaks references for everyone.
- Scene and prefab merges: UnityYAMLMerge is NOT configured, so two people editing `Lab.unity` will conflict. Do not hand-edit `Lab.unity`; change `BuildLabScene.cs` and rebuild the scene, and only one person commits the regenerated scene at a time. Tell the team before touching it.
- `git pull --rebase` (or pull) BEFORE every push. Never force-push `main`. No `--no-verify`.
- Explicit `git add <paths>`, no `git add -A` or `.`. Check `git status` first.
- One Unity editor instance per project. Close the editor before any batchmode command (the project lock otherwise makes batchmode fail).
- Commit messages: imperative, short subject.

## Integration notes (compile/build facts)
- Names B looks up are built by `BuildLabScene.cs`: `LED_Tray/Console/Door/Camera/Clipboard` (shared `M_Led`), `WindowBlinds`, `DoorHinge`, `RoomLight_Ceiling` (booth light is not driven), `DoorFrame` (frame + `DoorZone` stay put while `Door` swings). `Door` root = leaf, sign, handle, lock, card.
- `GrabTracker` on Cube, Console, DoorHandle, Clipboard, Chair. `XRSimpleInteractable` on Console, DoorHandle (never moves = locked, no joint), Chair. Clipboard is `XRGrabInteractable` + kinematic Rigidbody. `ConsentButton` has `XRSimpleInteractable` + `XRPokeFilter` (axis -Y) + `ButtonPress`. `ButtonZone` raised 1 cm.
- `DesktopRig` is a component on the XROrigin instance (self-disables with a headset or XR simulator). XROrigin stays at `Spawn` (0,0,-1.2); sampler uses world head position and the mark is at origin.
- `ValidateScene` is the smoke check (runner wiring, lines filled, GrabTrackers, ButtonPress, DesktopRig, CardDisplay, Cube inside TableZone, DebugPanel). No separate test.
- `DialogueLineTools.Generate` is public and called by the builder, so assets and `DialogueManager.lines` are filled on every build.

## Package D: Quest build settings (done in files)
Menu `Perfect Participant/Configure Quest Build` (also switches platform to Android), or batch `-executeMethod PerfectParticipant.Editor.ConfigureQuestBuild.ConfigureFromCommandLine` (add `-switchAndroid` to switch). Idempotent, logs `[ConfigureQuest] readback`.
- Company `NIS`, product `The Perfect Participant`, id `com.nis.perfectparticipant`, linear colour. Android: min API 29, target Automatic, IL2CPP, ARM64, Vulkan only, GameActivity, LandscapeLeft.
- XR Plug-in Management: OpenXR only, init on start. Features ON: Meta Quest Support (Android), Hand Tracking Subsystem, Meta Hand Tracking Aim, Oculus Touch, Meta Quest Touch Pro/Plus, Hand Interaction Profile. Single Pass Instanced. Prioritize Input Polling.
- Lab.unity is the only scene in the build list (the builder sets it).
- Run on device: build and run to the Quest, or `adb install -r PerfectParticipant.apk`, launch with `adb shell monkey -p com.nis.perfectparticipant 1`. Logs `adb logcat -s Unity`. CSV session logs in `/sdcard/Android/data/com.nis.perfectparticipant/files`. Keep realtime lights few, no SSAO or heavy post.

## Package F notes
- Doorway + `ExitBooth`; `Door` has a kinematic Rigidbody and moves by transform only.
- `SeatedMode` toggle: S key on the content note, A or X controller button (CardDisplay, before start), DebugPanel "Seated" button. Seated moves `IntakeMark` (0,0,-1.05), `Table` (0.6,0,-0.9) with Cube and Tray, `Console` (-0.6,0,-0.9), `Chair` (0,0,-1.3); seat = Spawn (0,0,-1.2). `GrabTracker.SetHome` and `RoomStateController.Rehome` refresh respawn poses.
- `Tests/EditMode/DeskPlaythroughTests.cs` drives `ExperimentFlow` through E1, E2, E3, E2 (press during refusal) plus all 27 test-response sequences; asserts every PlayLine id and RoomEvent id exists.

## Package A/B/C/E reference
- A: `InputSampler` resolves objects by name on first `Sample()`, edges latch and clear after `Sample()`. `GrabTracker` uses its own smoothed holder velocity for release speed. `ButtonPress` listens to `selectEntered` only (physics never presses). Door grab on desktop is an attempt only. Needs exact names: `Main Camera`, `Left/Right Controller`, `ObservationWindow`, `CeilingCamera`, `ConsentForm`, zones `MarkZone TrayZone TableZone ButtonZone DoorZone`.
- B: CSV logger `persistentDataPath/session_<time>.csv` (time,state,event,subtype,C,D,I,composure). Content note: `CardDisplay` disables `ExperimentRunner` in Awake until trigger/Space/Enter or `CardDisplay.StartExperiment()`; `showContentNote=false` skips it (debug only). Blinds closed until `ObserversIn` (half open), fully up on `BlindsUp`.
- C: rebuild via menu `Perfect Participant/Build Lab Scene` (overwrites `Scenes/Lab.unity` and `Materials/M_*.mat`); check `Perfect Participant/Validate Scene`. Primitives only. Window pane 1.5 m wide, north wall opening + `ObservationBooth` (`Silhouette_01..03` inactive).
- E: F1 toggles the IMGUI panel (only in editor or development builds): state, times, ending, C/D/I, tier, flags, composure slider, Force Comply/Deviate/Inaction, Jump buttons, time scale 1x/2x/5x, Restart, subtitles, Skip line, last 8 commands. Force works only while a test is listening.

## How to verify
Editor closed, one Unity instance, `<tmp>` any folder, `<root>` repo root:
```
U="C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
"$U" -batchmode -nographics -projectPath "<root>" -quit -logFile "<tmp>\compile.log"
"$U" -batchmode -nographics -projectPath "<root>" -runTests -testPlatform EditMode -assemblyNames PerfectParticipant.Tests.EditMode -testResults "<tmp>\results.xml" -logFile "<tmp>\tests.log"
"$U" -batchmode -nographics -projectPath "<root>" -executeMethod PerfectParticipant.Editor.BuildLabScene.BuildFromCommandLine -quit -logFile "<tmp>\build.log"
"$U" -batchmode -nographics -projectPath "<root>" -executeMethod PerfectParticipant.Editor.ValidateScene.ValidateFromCommandLine -quit -logFile "<tmp>\validate.log"
```
Do not add `-quit` with `-runTests`. Passing: exit code 0, `results.xml` shows `total="92" passed="92" failed="0"` (more as tests are added), no `error CS` in logs, `[ValidateScene]` OK. Grep logs for `[BuildLab]` warnings.

## Session log
- 2026-10-20: Architect session. Created `_Project` layout, 4 asmdefs, Core logic + 61 EditMode tests, runner + stubs, ARCHITECTURE.md, CLAUDE.md, HANDOFF.md.
- 2026-10-20: Package A (input) and B (output) code written, C (scene builder) written; integrator compiled all, reconciled names, built Lab.unity, Validate OK, 61/61.
- 2026-10-20: Package E (debug panel) written and wired, scene rebuilt. Package D done in files via ConfigureQuestBuild.cs.
- 2026-10-20: Part 1 fixes: doorway + ExitBooth, kinematic door, poke axis verified, SeatedMode MVP, DeskPlaythroughTests. Compile clean, 92/92, scene rebuilt, Validate OK. Nothing seen visually.
- 2026-10-20: Git: LFS configured, 4 local commits (2fcc5c9, 3ac6180, f0c48f6, 91d4774) on top of 51f7beb and 25b3752.
- 2026-10-20: Final hand-off rewrite of HANDOFF.md (this file), all open items listed in Next steps. Push to origin/main follows.
