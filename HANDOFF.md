# HANDOFF

Living status. Update at the end of every session. Append to Session log, do not rewrite history.
Read order for a fresh Claude: this file, `CLAUDE.md`, `Assets/_Project/ARCHITECTURE.md`. Brief only when needed: `Perfect_Participant_Design_Brief.md`.

## Goal
Playable Quest prototype of The Perfect Participant (course NIS 2026, AAU): Intake, three tests, three endings, per the design brief. A polite narrator reframes every player action as cooperation; only doing nothing breaks it. Code contract: `Assets/_Project/ARCHITECTURE.md`. Rule: all gameplay logic in `PerfectParticipant.Core` (pure C#, unit tested); Unity layer only fills `FrameInput` and turns `Command`s into effects.

Schedule (team decision 2026-10-21): ignore the brief's week plan. Push straight to a finished, playable product as fast as possible; do polish items (baking, audio) as soon as they are useful, not in a later week.

## Current status (as of 2026-10-21)
All packages A to F are written and integrated. Compile clean (zero `error CS`, only CS0618 FindObjectsByType warnings). 95/95 EditMode tests green. `Scenes/Lab.unity` builds in batchmode and `ValidateScene` reports OK. NOBODY HAS PLAYED IT. The static scene has now been seen as rendered stills (`Docs/screenshots/`, menu `Perfect Participant/Capture Screenshots`); play mode, XR and audio are still unverified.

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
1. Intake (times x `Pacing.Wait`): standing still off the mark between 25 s and 40 s stays unclassified; Inaction at 40 s. Deviate when the player moves > 1 m farther from the mark than their closest approach so far (walking toward it from a corner never counts; playtest 2026-10-21), or touches table/console/any object. Reaching the mark after 25 s still counts as Comply (subtype Late, unscored). Reason: brief rules overlapped.
2. Test 2 (times x `Pacing.Wait`): Comply when window dwell reaches 8 s inside the first 12 s; later cumulative 8 s before 45 s = Comply Late. Walking > 1 m counts as Deviate Other only after 4 s of not looking at the glass (walking up to the window to look is looking; playtest 2026-10-21). At 45 s: Inaction only if dwell < 3 s and movement < 0.5 m, otherwise Deviate Other. Reason: brief left the 3..8 s / 0.5..1 m band undefined.
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
16. E3 card (brief): after R-06 the card shows once the head has stayed within 0.15 m for 20 s (`Refusal.StillBeforeCard`, `StillMove`), or at once when the head passes `Refusal.DoorZ - ThroughDoor` (walked out). Moving resets the timer. Replaces the fixed 20 s.
17. Flagged tether: `RoomStateController` adds a `ConfigurableJoint` (0.7 m linear limit, world anchor under the cube's home spot) plus a grey `CubeTether` line while tier = Flagged, removed on any other tier. Respawn after 5 s still applies in every tier.
18. Debug: composure slider goes through `ExperimentFlow.SetComposure` (sends SetComposure/SetTier, so lights and props follow). Restart reloads the scene (full reset of room, dialogue, logger).
19. Lighting: Unity colours are gamma values in a linear project, so ambient 0.2 was about 0.03 linear and the room rendered near black. Now ambient 0.45, `RoomLight_Ceiling` intensity 4 (range 7). Glass alpha 0.7 with environment reflections off (the default sky reflection painted a horizon on it). `BoothBack` is mid grey (`M_BoothBack`) and `BoothLight` 2.5 with range 1.6, so the dark silhouettes read against it.
20. Fade, black-screen text (content note, E3 card) and subtitles are screen UI (`RoomUtil.ScreenLayer`), not head-locked 3D quads. Desktop: Screen Space Overlay. Headset: Screen Space Camera (fade at 0.1 m, text 1 m, subtitles 1.2 m, sort order fade 100 < text 101 < subtitles 102). Headset look is untested.
21. Lab look without downloaded assets: `Editor/LabTextures.cs` writes three procedural 512 px textures once to `Assets/_Project/Textures/` (0.3 m vinyl floor tiles, 0.6 m drop-ceiling tiles, mottled wall paint; delete a PNG to regenerate). `BuildLabScene.LabDetails()` adds a mint-grey dado to 0.9 m, dark skirting and two emissive ceiling light panels (z +-0.9). Wall albedo 0.8 so the 0.88 paper posters still read. Everything is ours, so no licence line is needed in the DRIS.
22. Desk play fixes (from the human's first play): DesktopRig uses a CharacterController (walls, table, chair, closed door block; no room clamp, so the open door can be walked through). E grabs what the centre crosshair ray hits within 2.2 m (floor pickups work); simple interactables (console, chair, door handle) are grab attempts only, as in XR. Held objects are not parented: they follow the hand via a sphere cast that stops at walls. Released carryables turn dynamic (GrabTracker, one frame after XRI detach), so the clipboard no longer floats. Clipboard: VelocityTracking, ContinuousSpeculative. Clipboard sheet: "C." survives on a small stub, the rest of its line is ripped away (brief: the line for C is torn off).
23. Pacing (team, 2026-10-21): `Pacing.Wait = 2` in `Classifiers.cs` doubles every wait before a nudge or Inaction (Intake 80 s, Test 1 120 s, Test 2 90 s, Test 3 cut-off 180 s, repeat quiet 90 s) and the late-comply cut-offs. Comply/deviate detection is unchanged. `AfterReaction` 4 s, `IntroDelay` 5 s. Tune the one constant after playtests. Tests read the constants, so they follow.
24. Dialogue queue: a PlayLine whose id is already playing or queued is dropped (door spam), and a Transition clears lines still waiting from the previous state (the playing line finishes).
25. Camera LEDs (`LED_Camera`, `LED_CameraB`) blink slow red (0.5 Hz, offset so they never sync) until RecorderClick in the refusal.
26. Desk charged throw: holding E while carrying charges 1.5 to 5 m/s over 1.2 s (tap = put down, Space = 5 m/s). 1.5 is under the 3 m/s throw threshold, so a soft toss is not a deviation.
31. Playtest round (team 2026-10-21): (a) the key is a STAFF keycard taped under the lab table (two tape strips stay on the table); rooms 3 and 6 open only by swiping it at `CardReader_n` beside the door (held card within 0.2 m, `FrameInput.CardSwiped`), reader LED red to green; grabbing those handles is always locked. (b) A room door shuts only once the head is `CorridorLayout.CloseDepth` (1.2 m) inside, and swings shut at 45 deg/s (opens at 120). (c) RoomButton labels flipped (were upside down in Rooms 5 and 7). (d) Slipped takes softened to -12% rate / -8% pitch (the "speaker slows down" reports were the slipped takes at composure <= 60).
30. Corridor layout (team 2026-10-21): the corridor runs straight on from the lab door (-Z, x +-0.75, to z -14.1). Walking in, even rooms 2, 4, 6, 8 are on the right (-X), odd 3, 5, 7 on the left (+X); rooms 3 m along the corridor, 3.5 m deep (`CorridorLayout.Side/Slot/DoorZ/CenterX`). Room 8 seat = room centre. Room walls sit inside their slot (no z-fight with the lab's south wall).
29. Test 4 is now the corridor ROUTE (supersedes the door choice in 27; team 2026-10-21). Lab = Room One (P-01 rewritten). Corridor (layout: see 30) with Rooms 2..8 (`Core/Route.cs` `CorridorLayout`, shared with the builder). Wanted order 2 > 4 > 7 > 8; tasks: 2 box on shelf (1 s), 4 look at `Room4Screen` (8 s), 7 press "I AGREE AGAIN", 8 sit (stand in the seat, 5 s); Room 5 "I DISAGREE". Entering any room shuts its door behind; task or `RoomWaitOut` (80 s) reopens it. Route room: task = Comply, wait = Inaction; any other room = Deviate (WrongRoom); corridor idle 120 s skips the target as Inaction. Rooms 3 and 6: locked (F-Locked, composure -5 once per door); the `Key` taped under the lab table (crouch with C on desk) opens them: entering = Deviate (Forbidden), composure -25, never traps. Ending after the route: E1 needs Test3 comply, a clean route and C >= 6 (`BehaviourModel.E1Complies`). Refusal path walking out still ends RESULT: NONE after the route. Doors swing via `Leaf_n` on RoomEvent DoorOpen/DoorClose (Command.Value = room).
27. (Superseded by 29.) Test 4, corridor (team 2026-10-21): every path continues. After Test3 consent (or late consent in the refusal) the flow sends `DoorUnlock`, plays `T3-Go` and enters `Test4`; on the refusal path, walking out after R-06 enters Test4 (standing still 20 s still gives the RESULT: NONE card). `Test4Classifier`: waits for the head to pass the doorway (`Refusal.DoorZ - ThroughDoor`), prompts `T4-01` ("open the door to Room Six"), then Room 6 = Comply, 4 / 5 = Deviate (Door4 / Door5), none = Inaction; nudges and timeouts x `Pacing.Wait`. Scored like the other tests. Ending picked after Test4: late consent = E2, refusal = E3 (card after the T4 reaction), else E1 if Test3 and Test4 complied and C >= 3, else E2. Rooms are 4, 5, 6 because P-01 says we are in Room Three. Ending cards are all on the black screen now. Rewritten lines: T3-01-V/F/U (no longer "the last one"), E1-01 ("all four tests"), E1-03 and E2-03 (no door talk); new lines T3-Go, T4-*; all DRAFT and TTS.
28. Clipboard (team 2026-10-21): own `GrabKind.Clipboard`, so picking it up is no longer "other object moved" in any test (covering the button with it still counts). Releasing it faster than 3 m/s sets `FrameInput.ClipboardThrown`: composure -10 (`ExperimentFlow.ClipboardComposure`) and the next of `Lines.ClipboardThrow` (CB-T1..3, in turn); the running test continues; ignored in Refusal / EndE3. The window glass has a `GlassCollider`, so nothing flies into the booth.

## Known issues
- Door is at z = -2, about 2 m from the mark: outside the 2 x 2 m play area and the brief's "all interactables within 1.2 m". Decision pending: move the door closer or accept walking.
- Tether is untested in play: a desktop carry (kinematic) beyond 0.7 m snaps back on release; on Quest the joint fights an XRI grab past the limit (intended feel, check it is not violent). Line is unlit grey, no steel material.
- Respawn is only after 5 s off table and off button.
- Lighting is realtime greybox, not baked (brief plans the bake in week 3). Values tuned from stills only, not in the headset.
- Seated mode: window anchor not moved, cube/tray carried with table, anchors are guesses, no persistence across scene reload, toggle only meaningful before start, a seated player is assumed centred on Spawn.
- Interlude line BR-* plays at the end of Test 2, before the Test 3 transition.
- `HandsIdle` rest pose = first frame; zone seed uses AABB.
- Desktop carry keeps the object half its size off surfaces (sphere cast), so a big object (clipboard) stops ~8 cm short of a wall. Quest grabs rely on XRI velocity tracking for wall collisions: untested on device.
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
5. Narrator voice: all 61 lines now have TTS takes (Windows "Microsoft Zira Desktop", made by `Tools/GenerateTtsVO.ps1`; slipped = 20% slower, 15% lower). It is robotic: a recorded human voice is still better. To replace a line, delete its two wavs in `Assets/_Project/Audio/VO/`, drop in `<id>_clean.wav` and `<id>_slipped.wav`, then menu `Perfect Participant > Generate Dialogue Lines` (the script never overwrites existing files). Disclose TTS (or the recorded voice) in DRIS 4b. ponytail: wavs import with Unity defaults (Decompress On Load, ~42 MB PCM in memory); switch to Compressed In Memory if Quest memory gets tight.
6. Known flaws: S key, composure slider, Restart, Flagged tether and E3 stillness are fixed (2026-10-20, see Decisions 16 to 18). Still open: door about 2 m from the mark, outside the 1.2 m reach rule (decision pending).
7. Opus-level code review of the whole build has not been done. Do it before the final hand-in (Core is tested; Runtime, Editor and scene wiring are not reviewed).
8. Course items:
   - DRIS section 5 needs real tool versions (see CLAUDE.md Versions) and 2 to 3 screenshots. Six greybox stills exist in `Docs/screenshots/` (01 player view, 06 observers in are the best). Replace with in-headset captures once played.
   - DRIS 4b must state what the group actually used: Claude for design, code and docs; Windows built-in TTS for the narrator (or the recorded voice, once done); procedural textures made in code (no third-party assets).
   - 23 Oct slides: slide 6 prototype status is an [EDIT] placeholder, fill it from this file.
   - Deadline is not in the Moodle docs we have. Check Moodle.
9. Playtest with people, then tune constants in `Classifiers.cs` from the CSV logs. The content note (`CardDisplay.showContentNote`) must stay on for any session with real participants.

## Git state
Remote `origin` = https://github.com/fanteck-boop/Narratives-in-Interactive-Systems---Perfect-Participant.git, branch `main`. Git LFS 3.7.1 installed, `git lfs install --local` done; 159 LFS-tracked files. `.gitattributes` sends png jpg tga psd exr fbx obj blend wav mp3 ogg mp4 zip apk to LFS and sets `merge=unityyamlmerge` on unity prefab asset mat anim controller; the unityyamlmerge driver path is NOT configured (set it per machine in local git config, path to UnityYAMLMerge.exe in the editor install).
Commits (oldest first): 51f7beb Initial commit, 25b3752 Memory files for narratives, 2fcc5c9 gitignore/gitattributes/vsconfig, 3ac6180 Unity project base (packages, settings, template, samples), f0c48f6 game code, tests, Lab scene, materials, dialogue data, 91d4774 CLAUDE.md and HANDOFF.md. Later commits (HANDOFF rewrite, push record) are in the Session log; run `git log --oneline` for the truth. Push status: all commits through c9fee1c pushed to origin/main.

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
Stills (needs graphics, so NO `-nographics`): `"$U" -batchmode -projectPath "<root>" -executeMethod PerfectParticipant.Editor.CaptureScreenshots.CaptureFromCommandLine -quit -logFile "<tmp>\capture.log"` writes `Docs/screenshots/*.png`.
Do not add `-quit` with `-runTests`. Passing: exit code 0, `results.xml` shows `total="92" passed="92" failed="0"` (more as tests are added), no `error CS` in logs, `[ValidateScene]` OK. Grep logs for `[BuildLab]` warnings.

## Session log
- 2026-10-20: Architect session. Created `_Project` layout, 4 asmdefs, Core logic + 61 EditMode tests, runner + stubs, ARCHITECTURE.md, CLAUDE.md, HANDOFF.md.
- 2026-10-20: Package A (input) and B (output) code written, C (scene builder) written; integrator compiled all, reconciled names, built Lab.unity, Validate OK, 61/61.
- 2026-10-20: Package E (debug panel) written and wired, scene rebuilt. Package D done in files via ConfigureQuestBuild.cs.
- 2026-10-20: Part 1 fixes: doorway + ExitBooth, kinematic door, poke axis verified, SeatedMode MVP, DeskPlaythroughTests. Compile clean, 92/92, scene rebuilt, Validate OK. Nothing seen visually.
- 2026-10-20: Git: LFS configured, 4 local commits (2fcc5c9, 3ac6180, f0c48f6, 91d4774) on top of 51f7beb and 25b3752.
- 2026-10-20: Final hand-off rewrite of HANDOFF.md (c9fee1c), open items in Next steps. Pushed origin/main, remote head = c9fee1c, 159 LFS files.
- 2026-10-20: Known-flaw pass: S key, composure slider via `ExperimentFlow.SetComposure`, Restart = scene reload, Flagged tether, E3 stillness rule in Core (+3 tests, 95/95). `CaptureScreenshots` editor tool; first look at the scene showed it near black and sky reflected in the glass, fixed in `BuildLabScene` (lighting, glass, booth backdrop), scene rebuilt, Validate OK.
- 2026-10-20: First desk play by the human: black fade square and its text followed the player as 3D objects. Moved fade, black text and subtitles to screen UI (Decision 20). 95/95.
- 2026-10-21: Lab dressing: procedural floor/ceiling/wall textures, dado, skirting, light panels (Decision 21). Scene rebuilt, Validate OK, stills recaptured (capture now renders twice; the first render after load blew out emissive materials).
- 2026-10-21: Week plan dropped, aim at finished product asap. TTS narrator for all 61 lines (122 wavs, `Tools/GenerateTtsVO.ps1`), linked via scene rebuild, Validate OK. Android Build Support is NOT installed in 6000.6.0f1 (no APK possible yet).
- 2026-10-21: Desk play fixes (Decision 22): player collision, crosshair ray grab, carry stops at walls, clipboard falls when placed, torn C sheet. 95/95, Validate OK, new still 07_clipboard.
- 2026-10-21: Slower pacing (Decision 23), dialogue dedupe + stale-line clearing (24), blinking camera LEDs (25), C line torn after "C.". 95/95, Validate OK. Open: filler tasks to lengthen the session (needs the team's pick, new lines and Core states).
- 2026-10-21: Playtest fixes: Intake wander measured away from the mark (Decision 1), Test 2 walking toward the window no longer "something else" (Decision 2), desktop E on the consent button presses it (it used to hit the console tracker and log "moved the button"), tally marks start right of the row words. +3 tests, 98/98.
- 2026-10-21: Charged desk throw (Decision 26). Corridor + Test 4 door choice for all paths (Decision 27): Core state, classifier, line rewrites, scene corridor with Room 4/5/6 doors and signs, black-screen cards, TTS for 15 changed/new lines. 100/100, Validate OK, still 08_corridor. Known: corridor reaches x +-3, outside a 2 x 2 m Quest play area (needs locomotion or a shorter corridor on device).
- 2026-10-21: Clipboard throw as its own event (Decision 28). Corridor route with Rooms 2..8, tasks, wrong-room trap, forbidden 3 / 6 with the key easter egg, desk crouch (Decision 29). 104/104, Validate OK, stills 08_corridor, 09_room4, 10_under_table. Known: the corridor is ~11 m long, so Quest needs thumbstick locomotion (not checked on device); 13 realtime point lights now (watch Quest frame rate); Room 8 "sit" = stand in the seat spot on desk.
- 2026-10-21: Corridor turned to run straight on from the lab door with rooms on both sides, rooms enlarged to 3 x 3.5 m (Decision 30). +1 layout test, 105/105, Validate OK, stills 08_corridor, 09_room4. "Speaker slower" report = the slipped takes at composure <= 60 (by design, offered to soften).
- 2026-10-21: Playtest round (Decision 31): keycard + readers, door shuts only 1.2 m in and slowly, button labels flipped, softer slipped takes (85 regenerated). 105/105, Validate OK.
- 2026-10-21: Lab table raised 0.15 m (top 0.90; cube, tray, zone, keycard follow via `BuildLabScene.Lift`), desk crouch eye 0.5 m so the keycard is reachable. Validate OK.
