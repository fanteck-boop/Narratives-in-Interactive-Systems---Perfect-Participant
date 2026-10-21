using System;
using PerfectParticipant.Core;
using UnityEngine;

namespace PerfectParticipant
{
    // Fills FrameInput once per frame. Edges latch from events, cleared in Sample().
    // Finds scene objects by name (see HANDOFF). Missing object = that field stays at idle value.
    public class InputSampler : MonoBehaviour
    {
        public event Action<PlayerEvent> PlayerEventRaised; // for SessionLogger

        [SerializeField] Transform head, leftHand, rightHand; // auto: Main Camera, Left/Right Controller, DesktopRig hand
        [SerializeField] float respawnAfter = 5f;

        const float FormDist = 0.5f, SeatedFormDist = 1f, FormCone = 45f, HideDot = -0.3f, IdleDist = 0.1f, TouchPad = 0.1f;

        bool ready;
        Transform window, ceilingCam, form;
        Zone mark, tray, table, button;
        Collider[] consoleCols;
        bool bPress, bConsole, bDoor, bOther, bRel, bClipThrow;
        float relSpeed, offTime;
        int corridorDoor, roomButton;
        Transform screen4, box2;
        Zone shelf2;
        GrabTracker key; // the keycard
        readonly Transform[] readers = new Transform[CorridorLayout.Last + 1];
        readonly bool[] atReader = new bool[CorridorLayout.Last + 1];
        const float SwipeDist = 0.2f;
        bool pInTray, pWin, pCam, pAway, pMark;
        readonly Vector3[] rest = new Vector3[2];
        readonly bool[] hasRest = new bool[2];

        void OnEnable()
        {
            GrabTracker.Grabbed += OnGrab; GrabTracker.Released += OnRelease; ButtonPress.Pressed += OnPress;
        }

        void OnDisable()
        {
            GrabTracker.Grabbed -= OnGrab; GrabTracker.Released -= OnRelease; ButtonPress.Pressed -= OnPress;
        }

        void Raise(PlayerEventType t, string payload = null) => PlayerEventRaised?.Invoke(new PlayerEvent(Time.time, t, payload));

        void OnPress() { bPress = true; Raise(PlayerEventType.ButtonPressed); }

        void OnGrab(GrabTracker g)
        {
            switch (g.kind)
            {
                case GrabKind.Cube: Raise(PlayerEventType.CubeGrabbed); break;
                case GrabKind.Console: bConsole = true; Raise(PlayerEventType.ConsoleMoved, g.name); break;
                case GrabKind.Door: bDoor = true; Raise(PlayerEventType.DoorGrabbed, g.name); break;
                case GrabKind.CorridorDoor: corridorDoor = g.number; Raise(PlayerEventType.DoorGrabbed, g.name); break;
                case GrabKind.Clipboard: case GrabKind.Key: Raise(PlayerEventType.ObjectMoved, g.name); break; // logged only: not a test response
                case GrabKind.RoomButton: roomButton = g.number; Raise(PlayerEventType.ButtonPressed, g.name); break;
                default: bOther = true; Raise(PlayerEventType.ObjectMoved, g.name); break;
            }
        }

        void OnRelease(GrabTracker g, float speed)
        {
            if (g.kind == GrabKind.Clipboard && speed > Test1Classifier.ThrowSpeed) { bClipThrow = true; Raise(PlayerEventType.ObjectMoved, "Clipboard thrown " + speed.ToString("F1")); }
            if (g.kind != GrabKind.Cube) return;
            bRel = true; relSpeed = speed;
            Raise(PlayerEventType.CubeReleased, speed.ToString("F1"));
            if (speed > Test1Classifier.ThrowSpeed) Raise(PlayerEventType.CubeThrown, speed.ToString("F1"));
        }

        Zone Z(string n)
        {
            var g = GameObject.Find(n);
            if (!g) { Debug.LogWarning("InputSampler: missing " + n); return null; }
            if (!g.TryGetComponent(out Zone z)) z = g.AddComponent<Zone>(); // Start() seeds overlaps
            z.Entered += (zone, c) => ZoneEvent(PlayerEventType.ZoneEntered, zone, c);
            z.Exited += (zone, c) => ZoneEvent(PlayerEventType.ZoneExited, zone, c);
            return z;
        }

        // log grabbables only, hands and controllers would spam
        void ZoneEvent(PlayerEventType t, Zone z, Collider c)
        {
            var g = c.GetComponentInParent<GrabTracker>();
            if (g) Raise(t, z.name + ":" + g.name);
        }

        static Transform T(string n) { var g = GameObject.Find(n); return g ? g.transform : null; }

        void Resolve()
        {
            if (!head && Camera.main != null) head = Camera.main.transform;
            if (ready) return;
            ready = true;
            window = T("ObservationWindow"); ceilingCam = T("CeilingCamera"); form = T("ConsentForm");
            mark = Z("MarkZone"); tray = Z("TrayZone"); table = Z("TableZone"); button = Z("ButtonZone"); Z("DoorZone");
            shelf2 = Z("ShelfZone"); box2 = T("Box"); screen4 = T("Room4Screen"); // corridor rooms (Test4)
            foreach (var g in GrabTracker.All) if (g.kind == GrabKind.Key) key = g;
            for (int n = CorridorLayout.First; n <= CorridorLayout.Last; n++) if (CorridorLayout.Forbidden(n)) readers[n] = T("CardReader_" + n);
            if (!leftHand) leftHand = T("Left Controller");
            if (!rightHand) rightHand = T("Right Controller");
            var desk = FindFirstObjectByType<DesktopRig>();
            if (desk && desk.isActiveAndEnabled) rightHand = desk.Hand;
        }

        static float Gaze(Transform h, Transform target) => target ? Vector3.Angle(h.forward, target.position - h.position) : 180f;

        // ponytail: rest pose = first-frame pose in head space, drifts if user sits down. Logged only.
        bool Idle(int i, Transform h)
        {
            if (!h) return true;
            Vector3 l = head.InverseTransformPoint(h.position);
            if (!hasRest[i]) { hasRest[i] = true; rest[i] = l; }
            return (l - rest[i]).sqrMagnitude < IdleDist * IdleDist;
        }

        bool Touching(Transform h)
        {
            if (!h) return false;
            if (table && table.HasPoint(h.position)) return true;
            if (consoleCols == null && GrabTracker.ConsolePanel) consoleCols = GrabTracker.ConsolePanel.GetComponentsInChildren<Collider>();
            if (consoleCols != null)
                foreach (var c in consoleCols)
                {
                    if (!c || c.isTrigger) continue;
                    Bounds b = c.bounds; b.Expand(TouchPad);
                    if (b.Contains(h.position)) return true;
                }
            return false;
        }

        public FrameInput Sample()
        {
            Resolve();
            var f = new FrameInput { DistFromMark = 1.2f, GazeAngleToWindow = 90f, GazeAngleToCamera = 90f, FacingAwayAngle = 60f };
            float dt = Time.deltaTime;

            if (head)
            {
                Vector3 p = head.position, fw = head.forward;
                f.HeadX = p.x; f.HeadZ = p.z; f.HeadYaw = head.eulerAngles.y;
                Vector3 mp = mark ? mark.transform.position : Vector3.zero; // mark moves in seated mode
                f.DistFromMark = new Vector2(p.x - mp.x, p.z - mp.z).magnitude;
                f.GazeAngleToWindow = Gaze(head, window);
                f.GazeAngleToCamera = Gaze(head, ceilingCam);
                if (window)
                    f.FacingAwayAngle = Vector3.Angle(Vector3.ProjectOnPlane(fw, Vector3.up), Vector3.ProjectOnPlane(window.position - p, Vector3.up));
                f.HandsIdle = Idle(0, leftHand) & Idle(1, rightHand);
                f.ReadingForm = form && Vector3.Distance(p, form.position) < (SeatedMode.Seated ? SeatedFormDist : FormDist) && Gaze(head, form) < FormCone;

                // log-only edges
                bool win = f.GazeAngleToWindow < Test2Classifier.WindowCone, cm = f.GazeAngleToCamera < Test2Classifier.CameraCone;
                bool away = f.FacingAwayAngle > Test2Classifier.AwayAngle;
                bool onMark = mark && mark.HasPoint(new Vector3(p.x, mark.transform.position.y, p.z));
                if (win && !pWin) Raise(PlayerEventType.GazeOnWindow);
                if (cm && !pCam) Raise(PlayerEventType.GazeOnCamera);
                if (away && !pAway) Raise(PlayerEventType.FacingAway);
                if (onMark != pMark) Raise(onMark ? PlayerEventType.ZoneEntered : PlayerEventType.ZoneExited, "MarkZone:Head");
                pWin = win; pCam = cm; pAway = away; pMark = onMark;
            }

            GrabTracker c = GrabTracker.Cube;
            if (c)
            {
                bool inTray = tray && tray.Has(c.transform);
                f.CubeHeld = c.Held && !bRel;
                f.CubeInTray = inTray && !c.Held;
                f.CubeOnButton = button && button.Has(c.transform);
                f.CubeOffTable = !c.Held && !inTray && !(table && table.Has(c.transform));
                f.CubeHandBehindHead = c.Held && head && Vector3.Dot((c.Holder.position - head.position).normalized, head.forward) < HideDot;
                if (f.CubeInTray && !pInTray) Raise(PlayerEventType.CubeInTray);
                pInTray = f.CubeInTray;

                // Flagged tether lives in RoomStateController; respawn covers every tier
                offTime = f.CubeOffTable && !f.CubeOnButton ? offTime + dt : 0f;
                if (offTime > respawnAfter) { c.Respawn(); offTime = 0f; Raise(PlayerEventType.CubeRespawned); }
            }

            f.CubeReleased = bRel; f.CubeReleaseSpeed = relSpeed;
            f.ButtonPressed = bPress; f.ConsoleMoved = bConsole; f.DoorGrabbed = bDoor; f.OtherObjectMoved = bOther;
            f.ButtonCovered = button && (button.HasKind(GrabKind.Other) || button.HasKind(GrabKind.Clipboard)); // clipboard over the button still covers it
            f.TouchedTableOrConsole = Touching(leftHand) || Touching(rightHand);

            f.CorridorDoor = corridorDoor; f.ClipboardThrown = bClipThrow; f.RoomButton = roomButton;
            for (int n = 0; n < readers.Length; n++) // swipe = the held card comes within SwipeDist of a reader (edge on arrival)
            {
                if (!readers[n]) continue;
                bool near = key && key.Held && Vector3.Distance(key.transform.position, readers[n].position) < SwipeDist;
                if (near && !atReader[n]) { f.CardSwiped = n; Raise(PlayerEventType.ObjectMoved, "Keycard swiped " + n); }
                atReader[n] = near;
            }
            f.BoxOnShelf = shelf2 && box2 && shelf2.Has(box2) && !(box2.TryGetComponent<GrabTracker>(out var bt) && bt.Held);
            f.GazeAngleToScreen = head ? Gaze(head, screen4) : 180f;
            bPress = bConsole = bDoor = bOther = bRel = bClipThrow = false; relSpeed = 0f; corridorDoor = roomButton = 0;
            return f;
        }
    }
}
