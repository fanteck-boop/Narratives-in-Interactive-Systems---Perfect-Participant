# Architecture contract

Source of truth for story and numbers: `Perfect_Participant_Design_Brief.md` (root). This file is the code contract. Change it only with the human's approval.

## Rule
All gameplay logic (timers, classification, scoring, endings, line choice) lives in `PerfectParticipant.Core` (pure C#, `noEngineReferences`, unit tested). The Unity layer only (1) fills one `FrameInput` per frame and (2) turns `Command`s into effects. No Unity script decides a response, a line id or a transition.

## Assemblies and files (`Assets/_Project/`)
| asmdef | folder | content |
|---|---|---|
| PerfectParticipant.Core | Scripts/Core | `Types.cs` (enums, `PlayerEvent`, `FrameInput`, `Command`, `Verdict`), `BehaviourModel.cs`, `Classifiers.cs` (Intake/Test1/Test2/Test3Classifier), `ExperimentFlow.cs`, `Script.cs` (`Lines`, `RoomEvents`, `Refusal`, `Cards`) |
| PerfectParticipant.Runtime | Scripts/Runtime (+Input, Output, Rooms) | `ExperimentRunner.cs` (glue, done), `Input/InputSampler.cs` (stub, A), `Output/DialogueManager.cs` (stub, B) |
| PerfectParticipant.Editor | Scripts/Editor | debug panel, scene builder (E, C) |
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
  Tier PickTier(); EndingId PickEnding(ResponseKind t3, bool pressedDuringRefusal); bool Has(Flag f); }
XClassifier.Tick(float dt, in FrameInput i) -> Verdict { Kind, Subtype, Nudge, ComposureDelta, Deviation }
static Lines.Text[id], Lines.ForTier(prefix, tier), Lines.Reaction(test, kind, subtype)
```
Returned command list is reused: consume before the next call. Timing constants are `public const` on each classifier.

## FrameInput (filled by package A each frame)
Edge = true on one frame only. Level = true while it holds. Angles in degrees, distances in metres, world space, mark at origin.
- Head: `HeadX, HeadZ, HeadYaw`, `DistFromMark` (horizontal), `GazeAngleToWindow` (head fwd vs window centre), `GazeAngleToCamera` (vs CeilingCamera), `FacingAwayAngle` (flat fwd vs flat dir to window, 180 = back to it), `HandsIdle`.
- Cube: `CubeHeld` (level), `CubeReleased` (edge, `CubeHeld` false that frame) + `CubeReleaseSpeed`, `CubeInTray` (level, in TrayZone, not held), `CubeOffTable` (level, not held, outside TableZone and TrayZone), `CubeHandBehindHead` (level, holding hand, normalized (hand - head) dot head fwd < -0.3), `CubeOnButton` (level, in ButtonZone).
- Other: `ButtonPressed` (edge, poke/controller only, never physics), `ButtonCovered` (level, non-cube object in ButtonZone), `ConsoleMoved` (edge, panel grabbed), `DoorGrabbed` (edge), `TouchedTableOrConsole` (level), `OtherObjectMoved` (edge, any other grabbable grabbed), `ReadingForm` (level, head within 0.5 m of form and facing it).
- `NarratorBusy` set by `ExperimentRunner` from `DialogueManager.Busy`.

## Commands (consumed via `ExperimentRunner.CommandIssued`)
| Kind | fields | consumer |
|---|---|---|
| PlayLine | Id, Slipped | DialogueManager: queue line; slipped take if `Slipped` and clip exists, else clean. Subtitle from `Lines.Text`. Drop a nudge (`*-N*`) if a reaction is queued. Drop a line whose id is already playing or queued. On Transition, clear lines still queued (playing line finishes). `Busy` true from handling until queue empty (no clip: estimate 0.06 s/char) |
| StopDialogue | | DialogueManager: cut current line, clear queue |
| SetTier / SetComposure | Tier / Value | RoomStateController (light preset, props, window, hum), logger. Only sent on change |
| RoomEvent | Id (`RoomEvents.*`) | RoomStateController / audio, one-shot beats |
| Transition | State | RoomStateController: per-state setup (Test1 cube on table, Test3 console lit + form, EndE1 warm + green LEDs + applause + door unlock chime, EndE2 clipboard lit + door unlock, Refusal nothing) |
| ShowCard | Id = title, Text = subtitle | CardDisplay on door / black screen for E3 |
| Classified | Test, Response, Subtype | TallyWall (skip Test=Intake), SessionLogger |
SessionLogger writes every command plus `PlayerEvent`s to `Application.persistentDataPath/session_<time>.csv`: time, state, kind, id/subtype, C, D, I, composure.

## Scene layout (package C), metres, origin = mark on floor, +Z north, +X east
Room shell 4 x 4, ceiling 2.6. `Spawn` (0,0,-1.2) facing +Z. `MarkZone` cylinder r 0.35. `ObservationWindow` north wall centre (0,1.5,2), 2.4 x 1.2, `Silhouette_01..03` behind at z 2.6. `WallTally` (-1.2,1.5,1.98). `CorkBoard` (1.2,1.5,1.98). `Console` west (-1.1,0,0.2), top 1.0, with `ConsentButton` and `ConsentForm` on top, `ButtonZone` box over button. `Table` east (1.1,0,0), top 0.75, `TableZone` box over top, `Cube` 0.08 at (1.0,0.79,-0.1), `Tray` + `TrayZone` (1.15,0.76,0.15). `Door` south (0,0,-2) 0.9 x 2.1 with `DoorHandle` + `DoorZone`. `ClipboardShelf` + `Clipboard` west wall (-1.95,1.4,1.0). `Chair` (-1.4,0,-1.4). `SpeakerGrille` (0,2.58,0) = narrator AudioSource. `CeilingCamera` (1.6,2.5,-1.6) aimed at mark. Floor `TapeMarks`.

## Naming
GameObjects and prefabs PascalCase exactly as above. Prefabs in `Prefabs/` same name. Zones are trigger colliders on child objects named `*Zone`. Scene: `Scenes/Lab.unity`. Materials `M_<Name>`. Dialogue assets `Data/Dialogue/<LineId>.asset`. Audio `Audio/VO/<LineId>_clean.wav`, `<LineId>_slipped.wav`. Namespaces `PerfectParticipant` (Runtime), `PerfectParticipant.Core`, `PerfectParticipant.Editor`.

## Work packages
- **A Input** (`Scripts/Runtime/Input`): XR Origin from XRI Starter Assets (controllers + XR Hands poke), cube `XRGrabInteractable`, console/clipboard/chair grabbables, door handle grab with locked joint, zone trigger scripts, gaze angles, cube respawn after 5 s out of reach (tether when Flagged), `InputSampler.Sample()` filling every field and clearing edges, desktop fallback via XR Device Simulator. Raise `PlayerEvent`s for the logger.
- **B Output** (`Scripts/Runtime/Output`, `Rooms`): `DialogueLine` ScriptableObject (id, clean clip, slipped clip), `DialogueManager` (replace stub, keep `Busy`), world-space TMP subtitle strip, placeholder audio (silence or TTS, disclose in DRIS), `RoomStateController` (3 tier light presets + emergency, slow lerps only, no flicker > 1 Hz), `TallyWall`, `CardDisplay`, `SessionLogger`. Editor menu to generate `DialogueLine` assets from `Lines.Text`.
- **C Greybox** (`Scenes`, `Prefabs`, `Materials`): primitives per layout above, URP Lit materials, baked lighting later. Optional editor script `Editor/LabBuilder.cs` that builds the scene.
- **D Quest build**: Android, IL2CPP, ARM64, OpenXR + Meta Quest feature, Lab.unity only in build list, 72 Hz target, linear colour.
- **E Debug + wiring**: `ExperimentRunner` in scene wired to InputSampler, DialogueManager and all consumers; editor window (`Editor/DebugPanel.cs`) showing state, C/D/I, composure, tier, flags, buttons for `Force`, `JumpTo`, `SetComposure`.
