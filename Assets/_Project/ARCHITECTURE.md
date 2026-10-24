# Architecture contract

Source of truth for story and numbers: `Perfect_Participant_Design_Brief.md` (root). This file is the code contract. Change it only with the human's approval.

## Rule
All gameplay logic (timers, classification, scoring, endings, line choice) lives in `PerfectParticipant.Core` (pure C#, `noEngineReferences`, unit tested). The Unity layer only (1) fills one `FrameInput` per frame and (2) turns `Command`s into effects. No Unity script decides a response, a line id or a transition.

## Assemblies and files (`Assets/_Project/`)
| asmdef | folder | content |
|---|---|---|
| PerfectParticipant.Core | Scripts/Core | `Types.cs` (enums, `PlayerEvent`, `FrameInput`, `Command`, `Verdict`), `BehaviourModel.cs`, `Classifiers.cs` (`Pacing`, Intake/Test1/Test2/Test3Classifier; Test 4 is in `Route.cs`), `ExperimentFlow.cs`, `Route.cs` (`CorridorLayout`, Test 4 corridor route as a partial of `ExperimentFlow`), `Script.cs` (`Lines`, `RoomEvents`, `Refusal`, `Cards`) |
| PerfectParticipant.Runtime | Scripts/Runtime (+Input, Output, Rooms, Debug) | `ExperimentRunner.cs` (glue), `Input/` (InputSampler, GrabTracker, Zone, ButtonPress, DesktopRig, SeatedMode), `Output/` (DialogueManager, DialogueLine, SessionLogger, CardDisplay, PlaceholderAudio), `Rooms/` (RoomStateController, RoomUtil, TallyWall), `Debug/DebugPanel.cs` |
| PerfectParticipant.Editor | Scripts/Editor | scene builder (`BuildLabScene`), `ValidateScene`, `CaptureScreenshots`, `LabTextures`, `ConfigureQuestBuild`, dialogue asset generation |
| PerfectParticipant.Tests.EditMode | Tests/EditMode | NUnit tests of Core |

## Core API
```csharp
class ExperimentFlow {
  BehaviourModel Model; GameState State; float StateTime; EndingId Ending; bool Finished;
  List<Command> Tick(float dt, in FrameInput i);   // first call enters Intake
  List<Command> Force(ResponseKind k, Subtype s);  // debug: resolve running test
  List<Command> JumpTo(GameState s);               // debug
  List<Command> SetComposure(int v);               // debug: SetComposure/SetTier on change
}
class BehaviourModel { int C, D, I, Composure; Tier Tier; Flag Flags; ResponseKind PrologueResponse;
  void Apply(ResponseKind r); void AddComposure(int d); void SetComposure(int v);
  Tier PickTier(); EndingId PickEnding(ResponseKind t3, bool pressedDuringRefusal, bool routeClean); bool Has(Flag f); const int E1Complies = 6; }
XClassifier.Tick(float dt, in FrameInput i) -> Verdict { Kind, Subtype, Nudge, ComposureDelta, Deviation }
static Lines.Text[id], Lines.ForTier(prefix, tier), Lines.Reaction(test, kind, subtype)
```
Returned command list is reused: consume before the next call. Timing constants are `public const` on each classifier.

## FrameInput (filled by package A each frame)
Edge = true on one frame only. Level = true while it holds. Angles in degrees, distances in metres, world space, mark at origin.
- Head: `HeadX, HeadZ, HeadYaw`, `DistFromMark` (horizontal), `GazeAngleToWindow` (head fwd vs window centre), `GazeAngleToCamera` (vs CeilingCamera), `FacingAwayAngle` (flat fwd vs flat dir to window, 180 = back to it), `HandsIdle`.
- Cube: `CubeHeld` (level), `CubeReleased` (edge, `CubeHeld` false that frame) + `CubeReleaseSpeed`, `CubeInTray` (level, in TrayZone, not held), `CubeOffTable` (level, not held, outside TableZone and TrayZone), `CubeHandBehindHead` (level, holding hand, normalized (hand - head) dot head fwd < -0.3), `CubeOnButton` (level, in ButtonZone).
- Other: `ButtonPressed` (edge, poke/controller only, never physics), `ButtonCovered` (level, non-cube object in ButtonZone), `ConsoleMoved` (edge, panel grabbed), `DoorGrabbed` (edge), `CorridorDoor` (edge, room number 2..8 of the corridor door grabbed, 0 none), `CardSwiped` (edge, 3 or 6: the held keycard reached `CardReader_n`), `BoxOnShelf` (level, Room 2), `GazeAngleToScreen` (Room 4), `RoomButton` (edge, 5 or 7), `ClipboardThrown` (edge), `TouchedTableOrConsole` (level), `OtherObjectMoved` (edge, any other grabbable grabbed), `ReadingForm` (level, head within 0.5 m of form and facing it).
- `NarratorBusy` set by `ExperimentRunner` from `DialogueManager.Busy`.

## Commands (consumed via `ExperimentRunner.CommandIssued`)
| Kind | fields | consumer |
|---|---|---|
| PlayLine | Id, Slipped | DialogueManager: queue line; slipped take if `Slipped` and clip exists, else clean. Subtitle from `Lines.Text`. Drop a nudge (`*-N*`) if a reaction is queued. Drop a line whose id is already playing or queued. On Transition, clear lines still queued (playing line finishes). `Busy` true from handling until queue empty (no clip: estimate 0.06 s/char) |
| StopDialogue | | DialogueManager: cut current line, clear queue |
| SetTier / SetComposure | Tier / Value | RoomStateController (light preset, props, window, hum), logger. Only sent on change |
| RoomEvent | Id (`RoomEvents.*`) | RoomStateController / audio, one-shot beats |
| Transition | State | RoomStateController: per-state setup (Test1 cube on table, Test3 console lit + form, EndE1 warm + green LEDs + applause + chime, EndE2 clipboard lit, Refusal nothing). The room door opens on RoomEvent `DoorUnlock` (after Test3 consent, before Test4) or `DoorUnlockHeavy` (R-06) |
| ShowCard | Id = title, Text = subtitle | CardDisplay: black screen with text (all endings, the player is in the corridor) |
| Classified | Test, Response, Subtype | TallyWall (skip Test=Intake), SessionLogger |
SessionLogger writes every command plus `PlayerEvent`s to `Application.persistentDataPath/session_<time>.csv`: time, state, kind, id/subtype, C, D, I, composure.

## Scene layout (package C), metres, origin = mark on floor, +Z north, +X east
Room shell 4 x 4, ceiling 2.6. `Spawn` (0,0,-1.2) facing +Z. `MarkZone` cylinder r 0.35. `ObservationWindow` north wall centre (0,1.5,2), 2.4 x 1.2, `Silhouette_01..03` behind at z 2.6. `WallTally` (-1.2,1.5,1.98). `CorkBoard` (1.2,1.5,1.98). `Console` west (-1.1,0,0.2), top 1.0, with `ConsentButton` and `ConsentForm` on top, `ButtonZone` box over button. `Table` east (1.1,0,0), top 0.90 (lifted 0.15 m by `BuildLabScene.Lift`), `TableZone` box over top, `Cube` 0.08 at (1.0,0.94,-0.1), `Tray` + `TrayZone` (1.15,0.91,0.15). `Door` south (0,0,-2) 0.9 x 2.1 with `DoorHandle` + `DoorZone`. Behind it `Corridor` (straight on, x +-0.75, z -2.1..-14.1; even rooms on the -X side, odd on +X) with `CorridorDoor_2..8` (leaf pivot `Leaf_n`, number on the leaf, `ROOM n` sign beside it, STAFF ONLY on 3 and 6) and Rooms 2..8 behind them, all positions from `Core/Route.cs` `CorridorLayout`. Room props: `ShelfZone` + `Box` (2), files (3), `Room4Screen` (4), `RoomButton_5`, observers' chairs (6), `RoomButton_7`, `Room8Chair`. `Key` (a STAFF keycard, object name kept) taped under the lab table (underside about 0.85 m, desk crouch eye 0.5 m); `CardReader_3` / `CardReader_6` beside the locked doors (LED_Reader<n>), `FrameInput.CardSwiped` when the held card is within 0.2 m. A room door shuts only once the head is `CorridorLayout.CloseDepth` (1.2 m) inside. Test4 = the route 2 > 4 > 7 > 8 (Route.cs); it follows Test3 consent (or late consent), and after R-06 on the refusal path when the player walks out. Room 8 done opens `CorridorDoor_9` into the `Hall` (CorridorLayout.Hall = 9, z < EndZ - Wall); ending E1 / E2 is picked and plays only on entering the Hall (formal lines, then H-01/H-02, then E1-03 / E2-03; ended in the lab without H lines). Refusal path: no route (RouteTarget 0, no C-01, nothing scored), the Hall is open from the start, non-forbidden doors open on a grab, H-R in the Hall, then the RESULT: NONE card. RoomEvent `DoorOpen` / `DoorClose` carry the room in Command.Value. `ClipboardShelf` + `Clipboard` west wall (-1.95,1.4,1.0). `Chair` (-1.4,0,-1.4). `SpeakerGrille` (0,2.58,0) = narrator AudioSource. `CeilingCamera` (1.6,2.5,-1.6) aimed at mark. Floor `TapeMarks`.

## Naming
GameObjects and prefabs PascalCase exactly as above. Prefabs in `Prefabs/` same name. Zones are trigger colliders on child objects named `*Zone`. Scene: `Scenes/Lab.unity`. Materials `M_<Name>`. Dialogue assets `Data/Dialogue/<LineId>.asset`. Audio `Audio/VO/<LineId>_clean.wav`, `<LineId>_slipped.wav`. Namespaces `PerfectParticipant` (Runtime), `PerfectParticipant.Core`, `PerfectParticipant.Editor`.

## Work packages
- **A Input** (`Scripts/Runtime/Input`): XR Origin from XRI Starter Assets (controllers + XR Hands poke), cube `XRGrabInteractable`, console/clipboard/chair grabbables, door handle grab with locked joint, zone trigger scripts, gaze angles, cube respawn after 5 s out of reach (tether when Flagged), `InputSampler.Sample()` filling every field and clearing edges, desktop fallback via XR Device Simulator. Raise `PlayerEvent`s for the logger.
- **B Output** (`Scripts/Runtime/Output`, `Rooms`): `DialogueLine` ScriptableObject (id, clean clip, slipped clip), `DialogueManager` (replace stub, keep `Busy`), world-space TMP subtitle strip, placeholder audio (silence or TTS, disclose in DRIS), `RoomStateController` (3 tier light presets + emergency, slow lerps only, no flicker > 1 Hz), `TallyWall`, `CardDisplay`, `SessionLogger`. Editor menu to generate `DialogueLine` assets from `Lines.Text`.
- **C Greybox** (`Scenes`, `Prefabs`, `Materials`): primitives per layout above, URP Lit materials, baked lighting later. Optional editor script `Editor/LabBuilder.cs` that builds the scene.
- **D Quest build**: Android, IL2CPP, ARM64, OpenXR + Meta Quest feature, Lab.unity only in build list, 72 Hz target, linear colour.
- **E Debug + wiring**: `ExperimentRunner` in scene wired to InputSampler, DialogueManager and all consumers; editor window (`Editor/DebugPanel.cs`) showing state, C/D/I, composure, tier, flags, buttons for `Force`, `JumpTo`, `SetComposure`.
