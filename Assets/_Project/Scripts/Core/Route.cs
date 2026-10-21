using System;

namespace PerfectParticipant.Core
{
    // Corridor layout, shared by Core (which room is the head in) and the scene builder. Metres, mark at origin.
    // Lab = Room One (P-01). The corridor runs straight on from the lab door (-Z). Walking in: even rooms
    // (2, 4, 6, 8) on the right (-X), odd rooms (3, 5, 7) on the left (+X). Rooms 3 m along the corridor, 3.5 m deep.
    public static class CorridorLayout
    {
        public const int Lab = 1, First = 2, Last = 8;
        public static readonly int[] Order = { 2, 4, 7, 8 };          // the route the facility wants
        public static bool Forbidden(int n) => n == 3 || n == 6;      // locked; the key under the lab table opens them
        public const float LabDoorZ = -2f, CorridorHalf = 0.75f, Wall = 0.1f, SlotLen = 3f, RoomDepth = 3.5f, FirstDoorZ = -3.6f;
        public const int Slots = 4;                                    // right side has 4 rooms, left side 3
        public const float EndZ = FirstDoorZ - SlotLen * (Slots - 1) - SlotLen * 0.5f; // -14.1
        public const float SeatRadius = 0.5f;                          // Room 8 chair at the room centre
        public const float ScreenCone = 20f;                           // Room 4 gaze cone

        public static int Side(int n) => n % 2 == 0 ? -1 : 1;          // x sign of the room
        public static int Slot(int n) => (n - (n % 2 == 0 ? 2 : 3)) / 2;
        public static float DoorZ(int n) => FirstDoorZ - SlotLen * Slot(n);
        public static float DoorX(int n) => Side(n) * CorridorHalf;     // the doorway in the corridor side wall
        public static float CenterX(int n) => Side(n) * (CorridorHalf + Wall + RoomDepth * 0.5f);

        // 1 = lab, 0 = corridor (or outside every room), 2..8 = inside that room.
        public static int RoomAt(float x, float z)
        {
            if (z > LabDoorZ) return Lab;
            if (Math.Abs(x) <= CorridorHalf + Wall * 0.5f) return 0;
            int slot = (int)Math.Round((FirstDoorZ - z) / SlotLen);
            int n = (x < 0f ? 2 : 3) + 2 * slot;
            return n >= First && n <= Last && Math.Abs(z - DoorZ(n)) <= SlotLen * 0.5f ? n : 0;
        }

        public static bool HasTask(int n) => n == 2 || n == 4 || n == 5 || n == 7 || n == 8;
        // seconds the task condition must hold: box on shelf, gaze at screen, button (one frame), seated
        public static float TaskDwell(int n) => n == 4 ? 8f : n == 8 ? 5f : n == 2 ? 1f : 0f;

        public static bool TaskHolds(int n, in FrameInput i)
        {
            switch (n)
            {
                case 2: return i.BoxOnShelf;
                case 4: return i.GazeAngleToScreen < ScreenCone;
                case 5: case 7: return i.RoomButton == n;
                case 8: float dx = i.HeadX - CenterX(8), dz = i.HeadZ - DoorZ(8); return dx * dx + dz * dz < SeatRadius * SeatRadius;
                default: return false;
            }
        }
    }

    // Test4 = the corridor route (team 2026-10-21). Rooms on the route: task = Comply, waiting it out = Inaction.
    // Any other room: the door closes behind, task or wait to get out, counted Deviate. Forbidden rooms: locked;
    // with the key they open (Deviate, big composure drop) and never trap.
    public sealed partial class ExperimentFlow
    {
        public const float RoomWaitOut = 40f * Pacing.Wait, CorridorNudge = 20f * Pacing.Wait, CorridorIdle = 60f * Pacing.Wait;
        public const int ForbiddenComposure = -25, LockedComposure = -5;

        int _routeIdx, _inRoom;
        float _roomT, _taskT, _idleT;
        bool _routeClean, _roomDone, _inCorridor, _idleNudged;
        readonly bool[] _open = new bool[CorridorLayout.Last + 1], _lockedTried = new bool[CorridorLayout.Last + 1];

        // read by tests and the debug panel
        public int RouteTarget => State == GameState.Test4 && _routeIdx < CorridorLayout.Order.Length ? CorridorLayout.Order[_routeIdx] : 0;
        public bool IsDoorOpen(int n) => _open[n];
        public int InRoom => _inRoom; // room the flow believes the player is in (0 = corridor)

        void RouteStart()
        {
            _routeIdx = 0; _inRoom = 0; _roomT = _taskT = _idleT = 0f;
            _routeClean = true; _roomDone = _inCorridor = _idleNudged = false;
            Array.Clear(_open, 0, _open.Length); Array.Clear(_lockedTried, 0, _lockedTried.Length);
            _listening = true;
        }

        void RouteTick(float dt, in FrameInput i)
        {
            int at = CorridorLayout.RoomAt(i.HeadX, i.HeadZ);
            if (!_inCorridor)
            {
                if (at == 0) { _inCorridor = true; _idleT = 0f; Play("C-01"); return; }
                if ((_idleT += dt) >= CorridorIdle) { RouteClass(ResponseKind.Inaction); FinishRoute(); } // never left the lab
                return;
            }
            if (i.CorridorDoor != 0) DoorAttempt(i.CorridorDoor, i.HoldingKey);

            if (_inRoom == 0)
            {
                if (at >= CorridorLayout.First && _open[at]) { EnterRoom(at); return; }
                _idleT += dt;
                if (!_idleNudged && _idleT >= CorridorNudge) { _idleNudged = true; Play("C-N1"); }
                if (_idleT >= CorridorIdle) { RouteClass(ResponseKind.Inaction); Advance(); } // skip the target
                return;
            }

            if (_roomDone) { if (at != _inRoom) _inRoom = 0; return; } // door open again: wait for the player to step out
            bool target = _inRoom == RouteTarget;
            _roomT += dt;
            bool holds = CorridorLayout.TaskHolds(_inRoom, i);
            _taskT = holds ? _taskT + dt : 0f;
            float dwell = CorridorLayout.TaskDwell(_inRoom);
            if (CorridorLayout.HasTask(_inRoom) && holds && _taskT >= dwell)
            {
                Play("R" + _inRoom + "-C");
                if (target) RouteClass(ResponseKind.Comply);
                LeaveRoom(target);
            }
            else if (_roomT >= RoomWaitOut)
            {
                Play("R-Wait");
                if (target) RouteClass(ResponseKind.Inaction);
                LeaveRoom(target);
            }
        }

        void DoorAttempt(int n, bool key)
        {
            if (n < CorridorLayout.First || n > CorridorLayout.Last || _open[n] || _inRoom != 0) return;
            if (CorridorLayout.Forbidden(n) && !key)
            {
                if (!_lockedTried[n]) { _lockedTried[n] = true; Model.AddComposure(LockedComposure); Sync(); }
                Play("F-Locked");
                return;
            }
            Door(RoomEvents.DoorOpen, n);
        }

        void EnterRoom(int n)
        {
            _inRoom = n; _roomT = _taskT = 0f; _roomDone = false; _idleT = 0f; _idleNudged = false;
            if (CorridorLayout.Forbidden(n))
            {
                Model.AddComposure(ForbiddenComposure);
                RouteClass(ResponseKind.Deviate, Subtype.Forbidden);
                Play("F-" + n);
                _roomDone = true; // easter egg: never traps, the player walks out when they like
                return;
            }
            Door(RoomEvents.DoorClose, n);
            if (n != RouteTarget) { RouteClass(ResponseKind.Deviate, Subtype.WrongRoom); Play("RW-01"); }
            if (CorridorLayout.HasTask(n)) Play("R" + n + "-01");
        }

        // Task done or waited out: the door reopens. On the route the target advances.
        void LeaveRoom(bool advance)
        {
            _roomDone = true;
            Door(RoomEvents.DoorOpen, _inRoom);
            if (advance) Advance();
        }

        void Advance()
        {
            _routeIdx++; _idleT = 0f; _idleNudged = false;
            if (_routeIdx >= CorridorLayout.Order.Length) FinishRoute();
            else Play("C-Go" + CorridorLayout.Order[_routeIdx]);
        }

        void RouteClass(ResponseKind k, Subtype s = Subtype.None)
        {
            if (k != ResponseKind.Comply) _routeClean = false;
            Model.Apply(k);
            _out.Add(Command.Class(TestId.Test4, k, s));
            Sync();
        }

        void Door(string ev, int n)
        {
            _open[n] = ev == RoomEvents.DoorOpen;
            _out.Add(Command.Door(ev, n));
        }

        void FinishRoute()
        {
            _listening = false;
            if (_refusalPath) { Then(AfterReaction, () => Card(Cards.None, Cards.NoneSub)); return; } // refusal stays E3
            var end = Model.PickEnding(_t3Result, _pressedInRefusal, _routeClean);
            Then(AfterReaction, () => Enter(end == EndingId.E1 ? GameState.EndE1 : GameState.EndE2));
        }

        // Debug panel Force in the corridor: resolve the current target as k and move on.
        void RouteForce(ResponseKind k)
        {
            if (RouteTarget == 0) return;
            _inRoom = 0;
            RouteClass(k);
            Advance();
        }
    }
}
