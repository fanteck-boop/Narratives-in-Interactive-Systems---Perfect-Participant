using System;

namespace PerfectParticipant.Core
{
    public enum TestId { Intake, Test1, Test2, Test3, Test4 }
    public enum ResponseKind { None, Comply, Deviate, Inaction }
    public enum Subtype { None, Late, Hold, Throw, Hide, Other, TurnedAway, Camera, MovedButton, CubeOnButton, TriedDoor, WrongRoom, Forbidden }
    public enum Tier { Neutral, Valued, Flagged, Unclassified }
    // Test4 = corridor door choice after Test3 (all paths, team 2026-10-21). Kept before Refusal: Force checks State <= Test4.
    public enum GameState { Intake, Test1, Test2, Test3, Test4, Refusal, EndE1, EndE2, EndE3 }
    public enum EndingId { None, E1, E2, E3 }

    [Flags]
    public enum Flag { None = 0, ThrewCube = 1, HidCube = 2, TurnedAway = 4, TriedDoor = 8, LateConsent = 16, ReadForm = 32 }

    public enum PlayerEventType
    {
        ZoneEntered, ZoneExited, CubeGrabbed, CubeReleased, CubeInTray, CubeThrown, CubeRespawned,
        GazeOnWindow, GazeOnCamera, FacingAway, ButtonPressed, ConsoleMoved, DoorGrabbed, ObjectMoved
    }

    // Raw event for logger / debug. Flow does not read these; it reads FrameInput.
    public struct PlayerEvent
    {
        public float Time;
        public PlayerEventType Type;
        public string Payload; // zone or object name
        public PlayerEvent(float time, PlayerEventType type, string payload = null) { Time = time; Type = type; Payload = payload; }
    }

    // One frame of abstract input. Unity input layer fills every field each frame.
    // "edge" = true only on the frame it happens. "level" = true while it holds.
    public struct FrameInput
    {
        public float HeadX, HeadZ;          // world m, horizontal head position
        public float HeadYaw;               // deg
        public float DistFromMark;          // m, horizontal head distance to mark centre
        public float GazeAngleToWindow;     // deg, head forward vs dir to window centre (3D)
        public float GazeAngleToCamera;     // deg, head forward vs dir to ceiling camera (3D)
        public float FacingAwayAngle;       // deg, flat head forward vs flat dir to window (0 facing, 180 back)
        public bool HandsIdle;              // level, both hands within 10 cm of rest pose (logged only)

        public bool CubeHeld;               // level
        public bool CubeReleased;           // edge, CubeHeld is false on this frame
        public float CubeReleaseSpeed;      // m/s, valid on CubeReleased frame
        public bool CubeInTray;             // level, cube inside TrayZone and not held
        public bool CubeOffTable;           // level, cube not held and outside TableZone and TrayZone
        public bool CubeHandBehindHead;     // level, holding hand: dot(hand - head, head fwd) normalized < -0.3
        public bool CubeOnButton;           // level, cube inside ButtonZone

        public bool ButtonPressed;          // edge, poke or controller press only (not physics)
        public bool ButtonCovered;          // level, any non-cube object inside ButtonZone
        public bool ConsoleMoved;           // edge, console / button panel grabbed
        public bool DoorGrabbed;            // edge, door handle grab attempt
        public int CorridorDoor;            // edge, room number (2..8) of the corridor door grabbed, 0 = none
        public int CardSwiped;              // edge, room number (3, 6) of the card reader the keycard from under the lab table was swiped at
        public bool BoxOnShelf;             // level, Room 2 box inside ShelfZone and not held
        public float GazeAngleToScreen;     // deg, head forward vs Room 4 screen centre
        public int RoomButton;              // edge, room number of the room button pressed (5, 7), 0 = none
        public bool ClipboardThrown;        // edge, clipboard released faster than Test1Classifier.ThrowSpeed (own event, never a test response)
        public bool TouchedTableOrConsole;  // level, hand or controller inside TableZone or console collider
        public bool OtherObjectMoved;       // edge, any grabbable other than cube, console, door grabbed
        public bool ReadingForm;            // level, head within 0.5 m of form and facing it

        public bool NarratorBusy;           // level, DialogueManager has a line playing or queued
    }

    public enum CommandKind { PlayLine, StopDialogue, SetTier, SetComposure, RoomEvent, Transition, ShowCard, Classified }

    // Output of ExperimentFlow. Only fields relevant to Kind are set.
    public struct Command
    {
        public CommandKind Kind;
        public string Id;          // PlayLine: line id. RoomEvent: event name. ShowCard: title
        public string Text;        // ShowCard: subtitle
        public bool Slipped;       // PlayLine: use slipped take (composure <= 60), fall back to clean
        public Tier Tier;          // SetTier
        public int Value;          // SetComposure
        public GameState State;    // Transition
        public TestId Test;        // Classified
        public ResponseKind Response;
        public Subtype Subtype;

        public static Command Line(string id, bool slipped) => new Command { Kind = CommandKind.PlayLine, Id = id, Slipped = slipped };
        public static Command Stop() => new Command { Kind = CommandKind.StopDialogue };
        public static Command TierCmd(Tier t) => new Command { Kind = CommandKind.SetTier, Tier = t };
        public static Command Composure(int v) => new Command { Kind = CommandKind.SetComposure, Value = v };
        public static Command Room(string ev) => new Command { Kind = CommandKind.RoomEvent, Id = ev };
        public static Command Door(string ev, int room) => new Command { Kind = CommandKind.RoomEvent, Id = ev, Value = room }; // DoorOpen / DoorClose
        public static Command To(GameState s) => new Command { Kind = CommandKind.Transition, State = s };
        public static Command Card(string title, string sub) => new Command { Kind = CommandKind.ShowCard, Id = title, Text = sub };
        public static Command Class(TestId t, ResponseKind r, Subtype s) => new Command { Kind = CommandKind.Classified, Test = t, Response = r, Subtype = s };

        public override string ToString()
        {
            switch (Kind)
            {
                case CommandKind.PlayLine: return "PlayLine " + Id + (Slipped ? " (slipped)" : "");
                case CommandKind.SetTier: return "SetTier " + Tier;
                case CommandKind.SetComposure: return "SetComposure " + Value;
                case CommandKind.RoomEvent: return "RoomEvent " + Id;
                case CommandKind.Transition: return "Transition " + State;
                case CommandKind.ShowCard: return "ShowCard " + Id + " | " + Text;
                case CommandKind.Classified: return "Classified " + Test + " " + Response + " " + Subtype;
                default: return Kind.ToString();
            }
        }
    }

    // Classifier output for one tick.
    public struct Verdict
    {
        public ResponseKind Kind;   // None = not resolved this tick
        public Subtype Subtype;
        public string Nudge;        // line id to play, or null
        public int ComposureDelta;  // Test3 inactivity drops
        public Subtype Deviation;   // Test3 only: non-final deviation this tick

        public static Verdict Of(ResponseKind k, Subtype s = Subtype.None) => new Verdict { Kind = k, Subtype = s };
    }
}
