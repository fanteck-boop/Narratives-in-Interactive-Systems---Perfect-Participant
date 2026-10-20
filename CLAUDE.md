# The Perfect Participant

Unity VR prototype (Meta Quest, standalone): a polite narrator reframes every player action as cooperation; only doing nothing breaks it. Course project NIS 2026, AAU.

## Start and end of every session
1. Start: read `HANDOFF.md`, then `Assets/_Project/ARCHITECTURE.md`. Brief only when needed: `Perfect_Participant_Design_Brief.md`.
2. End: update `HANDOFF.md` (status per work package, decisions, known issues, next steps) and append one line to its Session log. Keep its structure.

## Versions
Unity 6000.6.0f1, URP 17.6, XR Interaction Toolkit 3.6.0, XR Hands 1.9.0, OpenXR 1.18, Meta OpenXR 2.6.1, Input System 1.20 only (no legacy input), Test Framework 1.8.0, IL2CPP.

## Architecture rule
All gameplay logic lives in `PerfectParticipant.Core` (pure C#, no UnityEngine, unit tested). The Unity layer only fills `FrameInput` each frame and turns `Command`s into effects. Never decide responses, line ids or transitions in a MonoBehaviour. Contract: `Assets/_Project/ARCHITECTURE.md`. Change the contract only with the human's approval, then update tests.

## Folder map
```
Assets/_Project/
  ARCHITECTURE.md        code contract
  Scripts/Core/          PerfectParticipant.Core (pure C#)
  Scripts/Runtime/       PerfectParticipant.Runtime: ExperimentRunner, Input/, Output/, Rooms/
  Scripts/Editor/        PerfectParticipant.Editor
  Tests/EditMode/        PerfectParticipant.Tests.EditMode (NUnit)
  Scenes/ Prefabs/ Data/Dialogue/ Materials/ Audio/
```
Never edit `Assets/Samples`, `Assets/VRTemplateAssets`, `Assets/Scenes` (template), or `Assets/XR*`. Copy what you need into `_Project`.

## Coding rules
- Minimal code that works. Unity and .NET built-ins first. No speculative abstractions, no interface with one implementation, no config for constants that never change.
- Non-trivial logic leaves one runnable check (EditMode test for Core).
- Mark deliberate shortcuts with `// ponytail: <ceiling>, <upgrade path>`.
- Comments short and terse. No filler. No em dashes anywhere.
- Naming: PascalCase GameObjects and prefabs exactly as in ARCHITECTURE.md (e.g. `MarkZone`, `ConsentButton`), zones are trigger children named `*Zone`, materials `M_<Name>`, dialogue assets `<LineId>.asset`, VO `<LineId>_clean.wav` / `_slipped.wav`. Line ids only from `Lines.Text` in `Scripts/Core/Script.cs`.

## Compile and test (editor must be closed, only one Unity instance per project)
```
"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -batchmode -nographics -projectPath "<repo root>" -runTests -testPlatform EditMode -assemblyNames PerfectParticipant.Tests.EditMode -testResults "<tmp>\results.xml" -logFile "<tmp>\unity.log"
```
Do not add `-quit` with `-runTests`. Exit code 0 = all green. Check `<test-run ... failed="0">` in results.xml and `error CS` in the log. Compile only: same command with `-quit` instead of the `-runTests ...` flags.
