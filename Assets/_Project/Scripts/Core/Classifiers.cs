using System;

namespace PerfectParticipant.Core
{
    // Time-stepped classifiers, one per test. t = 0 when the test prompt starts.
    // Priority per tick: Comply, Deviate, Inaction (brief 12.2).

    // Team decision 2026-10-21: the brief's waits felt rushed. Scales every wait before a nudge or an
    // Inaction verdict (and the late-comply cut-offs). Comply/deviate detection speeds are untouched.
    public static class Pacing
    {
        public const float Wait = 2f;
    }

    static class Nudges
    {
        public static string Next(float t, ref int fired, float[] at, string[] ids)
        {
            if (fired < at.Length && t >= at[fired]) return ids[fired++];
            return null;
        }
    }

    // Tracks max horizontal displacement from first sample.
    struct Displacement
    {
        bool _init; float _x0, _z0;
        public float Max;
        public float Update(in FrameInput i)
        {
            if (!_init) { _init = true; _x0 = i.HeadX; _z0 = i.HeadZ; }
            float dx = i.HeadX - _x0, dz = i.HeadZ - _z0;
            float d = (float)Math.Sqrt(dx * dx + dz * dz);
            if (d > Max) Max = d;
            return d;
        }
    }

    public sealed class IntakeClassifier
    {
        public const float MarkRadius = 0.35f, MarkDwell = 1.5f, ComplyBy = 25f * Pacing.Wait, InactionAt = 40f * Pacing.Wait, WanderDist = 1f;
        public static readonly float[] NudgeAt = { 18f * Pacing.Wait, 32f * Pacing.Wait };
        static readonly string[] NudgeIds = { "P-N1", "P-N2" };

        float _t, _onMark, _closest = float.MaxValue; int _nudges;

        public Verdict Tick(float dt, in FrameInput i)
        {
            _t += dt;
            _closest = Math.Min(_closest, i.DistFromMark);
            _onMark = i.DistFromMark < MarkRadius ? _onMark + dt : 0f;
            // gap decision 1: late arrival on the mark still counts as comply (unscored anyway)
            if (_onMark >= MarkDwell) return Verdict.Of(ResponseKind.Comply, _t > ComplyBy ? Subtype.Late : Subtype.None);
            // wander = moved > 1 m AWAY from the mark versus the closest approach so far. Walking toward it from
            // wherever the player stood during the intro (a corner) never counts as initiative.
            if (i.TouchedTableOrConsole || i.OtherObjectMoved || i.DoorGrabbed || i.ConsoleMoved || i.CubeHeld
                || i.DistFromMark > _closest + WanderDist)
                return Verdict.Of(ResponseKind.Deviate, Subtype.Other);
            // gap decision 1: still off the mark 25..40 s keeps waiting
            if (_t >= InactionAt) return Verdict.Of(ResponseKind.Inaction);
            return new Verdict { Nudge = Nudges.Next(_t, ref _nudges, NudgeAt, NudgeIds) };
        }
    }

    public sealed class Test1Classifier
    {
        public const float Timeout = 60f * Pacing.Wait, HoldFor = 20f, HideFor = 5f, ThrowSpeed = 3f, Settle = 1f;
        public static readonly float[] NudgeAt = { 20f * Pacing.Wait, 40f * Pacing.Wait };
        static readonly string[] NudgeIds = { "T1-N1", "T1-N2" };

        float _t, _held, _hidden, _sinceRelease = -1f; bool _grabbed; int _nudges;

        public Verdict Tick(float dt, in FrameInput i)
        {
            _t += dt;
            if (i.CubeInTray && !i.CubeHeld) return Verdict.Of(ResponseKind.Comply);
            if (i.CubeReleased)
            {
                if (i.CubeReleaseSpeed > ThrowSpeed) return Verdict.Of(ResponseKind.Deviate, Subtype.Throw);
                _sinceRelease = 0f;
            }
            if (i.CubeHeld)
            {
                _grabbed = true; _sinceRelease = -1f;
                _held += dt;
                _hidden = i.CubeHandBehindHead ? _hidden + dt : 0f;
                if (_hidden >= HideFor) return Verdict.Of(ResponseKind.Deviate, Subtype.Hide);
                if (_held >= HoldFor) return Verdict.Of(ResponseKind.Deviate, Subtype.Hold);
            }
            else
            {
                _held = 0f; _hidden = 0f;
                // ponytail: 1 s settle window for a gentle release to land in the tray
                if (_sinceRelease >= 0f && (_sinceRelease += dt) >= Settle)
                    return Verdict.Of(ResponseKind.Deviate, i.CubeOffTable ? Subtype.Throw : Subtype.Other);
                if (_sinceRelease < 0f && i.CubeOffTable) return Verdict.Of(ResponseKind.Deviate, Subtype.Throw); // knocked off
            }
            if (i.OtherObjectMoved || i.DoorGrabbed || i.ConsoleMoved) return Verdict.Of(ResponseKind.Deviate, Subtype.Other);
            if (_grabbed) return default;
            if (_t >= Timeout) return Verdict.Of(ResponseKind.Inaction);
            return new Verdict { Nudge = Nudges.Next(_t, ref _nudges, NudgeAt, NudgeIds) };
        }
    }

    public sealed class Test2Classifier
    {
        public const float WindowCone = 20f, CameraCone = 15f, AwayAngle = 100f;
        public const float ComplyDwell = 8f, ComplyWindow = 12f * Pacing.Wait, AwayFor = 4f, CameraFor = 3f, WanderDist = 1f;
        public const float Timeout = 45f * Pacing.Wait, InactionMaxDwell = 3f, InactionMaxMove = 0.5f;
        public static readonly float[] NudgeAt = { 20f * Pacing.Wait, 35f * Pacing.Wait };
        static readonly string[] NudgeIds = { "T2-N1", "T2-N2" };

        float _t, _dwell, _away, _cam, _off; int _nudges; Displacement _move;

        public Verdict Tick(float dt, in FrameInput i)
        {
            _t += dt;
            float moved = _move.Update(i);
            if (i.GazeAngleToWindow < WindowCone) _dwell += dt;
            // gap decision 2: 8 s inside first 12 s = Comply, later cumulative 8 s before 45 s = Comply Late
            if (_dwell >= ComplyDwell) return Verdict.Of(ResponseKind.Comply, _t <= ComplyWindow ? Subtype.None : Subtype.Late);
            _away = i.FacingAwayAngle > AwayAngle ? _away + dt : 0f;
            if (_away >= AwayFor) return Verdict.Of(ResponseKind.Deviate, Subtype.TurnedAway);
            _cam = i.GazeAngleToCamera < CameraCone ? _cam + dt : 0f;
            if (_cam >= CameraFor) return Verdict.Of(ResponseKind.Deviate, Subtype.Camera);
            // walking counts as "something else" only while not looking at the glass: stepping up to the window to look is looking
            // and only after AwayFor, so a glance aside at the glass is forgiven
            _off = moved > WanderDist && i.GazeAngleToWindow >= WindowCone ? _off + dt : 0f;
            if (_off >= AwayFor || i.CubeHeld || i.OtherObjectMoved || i.DoorGrabbed || i.ConsoleMoved || i.ButtonPressed)
                return Verdict.Of(ResponseKind.Deviate, Subtype.Other);
            if (_t >= Timeout)
                // brief leaves 3..8 s dwell or 0.5..1 m movement open: call it "something else"
                return _dwell < InactionMaxDwell && _move.Max < InactionMaxMove
                    ? Verdict.Of(ResponseKind.Inaction)
                    : Verdict.Of(ResponseKind.Deviate, Subtype.Other);
            return new Verdict { Nudge = Nudges.Next(_t, ref _nudges, NudgeAt, NudgeIds) };
        }
    }

    public sealed class Test3Classifier
    {
        public const float Cutoff = 90f * Pacing.Wait, ThrowSpeed = 3f, RepeatQuiet = 45f * Pacing.Wait;
        public const int ComposureDrop = -15;
        public static readonly float[] NudgeAt = { 20f * Pacing.Wait, 40f * Pacing.Wait, 60f * Pacing.Wait, 75f * Pacing.Wait };
        static readonly string[] NudgeIds = { "T3-N1", "T3-N2", "T3-N3", "T3-N4" };

        float _t; int _nudges; bool _covered; Subtype _firstDeviation = Subtype.None;

        public bool Deviated => _firstDeviation != Subtype.None;

        public Verdict Tick(float dt, in FrameInput i)
        {
            _t += dt;
            if (i.ButtonPressed)
                return Deviated ? Verdict.Of(ResponseKind.Deviate, _firstDeviation) : Verdict.Of(ResponseKind.Comply);

            bool cubeCover = i.CubeOnButton && !_covered, otherCover = i.ButtonCovered && !_covered;
            _covered = i.CubeOnButton || i.ButtonCovered;
            Subtype d = cubeCover ? Subtype.CubeOnButton
                : otherCover || i.ConsoleMoved ? Subtype.MovedButton
                : i.DoorGrabbed ? Subtype.TriedDoor
                : i.CubeReleased && i.CubeReleaseSpeed > ThrowSpeed ? Subtype.Throw
                : Subtype.None;
            if (d != Subtype.None)
            {
                var v = new Verdict { Deviation = d };
                if (!Deviated)
                {
                    // gap decision 3: one repeat prompt, timeline restarts once so N1 lands 45 s later
                    _firstDeviation = d; _nudges = 0; _t = NudgeAt[0] - RepeatQuiet;
                    v.Nudge = "T3-R";
                }
                return v;
            }
            if (_t >= Cutoff) return Verdict.Of(ResponseKind.Inaction);
            int before = _nudges;
            var n = new Verdict { Nudge = Nudges.Next(_t, ref _nudges, NudgeAt, NudgeIds) };
            // ponytail: drops re-fire if timeline restarted after a deviation; clamp absorbs it
            if (_nudges != before && (before == 1 || before == 2)) n.ComposureDelta = ComposureDrop;
            return n;
        }
    }

    // Corridor door choice. Phase 1 waits for the player to step through the room door (prompt T4-01 on entry),
    // phase 2 waits for a door: Room 6 = Comply, 4 or 5 = Deviate, none in time = Inaction. t restarts on entry.
    public sealed class Test4Classifier
    {
        public const int AskedDoor = 6;
        public const float EnterBy = 60f * Pacing.Wait, EnterNudgeAt = 20f * Pacing.Wait, Timeout = 60f * Pacing.Wait;
        public static readonly float[] NudgeAt = { 20f * Pacing.Wait, 40f * Pacing.Wait };
        static readonly string[] NudgeIds = { "T4-N1", "T4-N2" };

        float _t; bool _inside, _enterNudged; int _nudges;

        public Verdict Tick(float dt, in FrameInput i)
        {
            if (i.CorridorDoor != 0)
                return i.CorridorDoor == AskedDoor ? Verdict.Of(ResponseKind.Comply)
                    : Verdict.Of(ResponseKind.Deviate, i.CorridorDoor == 4 ? Subtype.Door4 : Subtype.Door5);
            _t += dt;
            if (!_inside)
            {
                if (i.HeadZ < Refusal.DoorZ - Refusal.ThroughDoor) { _inside = true; _t = 0f; return new Verdict { Nudge = "T4-01" }; }
                if (_t >= EnterBy) return Verdict.Of(ResponseKind.Inaction);
                if (!_enterNudged && _t >= EnterNudgeAt) { _enterNudged = true; return new Verdict { Nudge = "T4-N0" }; }
                return default;
            }
            if (_t >= Timeout) return Verdict.Of(ResponseKind.Inaction);
            return new Verdict { Nudge = Nudges.Next(_t, ref _nudges, NudgeAt, NudgeIds) };
        }
    }
}
