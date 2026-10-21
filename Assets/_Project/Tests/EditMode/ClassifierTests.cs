using System;
using System.Collections.Generic;
using NUnit.Framework;
using PerfectParticipant.Core;

namespace PerfectParticipant.Tests
{
    public class ClassifierTests
    {
        public const float Dt = 0.1f, Tol = 0.15f;

        // Player standing at spawn, looking at nothing in particular.
        public static FrameInput Idle() => new FrameInput
        {
            HeadZ = -1.2f, DistFromMark = 1.2f, GazeAngleToWindow = 90f, GazeAngleToCamera = 90f, FacingAwayAngle = 60f
        };

        public class Run
        {
            public Verdict V; public float T = -1f; public int Composure;
            public readonly List<(string id, float t)> Nudges = new List<(string, float)>();
            public readonly List<(Subtype d, float t)> Deviations = new List<(Subtype, float)>();
        }

        public static Run Go(Func<float, FrameInput, Verdict> tick, Func<float, FrameInput> input, float max)
        {
            var r = new Run();
            for (int n = 1; n * Dt <= max + 1e-3f; n++)
            {
                float t = n * Dt;
                var v = tick(Dt, input(t));
                if (v.Nudge != null) r.Nudges.Add((v.Nudge, t));
                if (v.Deviation != Subtype.None) r.Deviations.Add((v.Deviation, t));
                r.Composure += v.ComposureDelta;
                if (v.Kind != ResponseKind.None) { r.V = v; r.T = t; break; }
            }
            return r;
        }

        const float W = Pacing.Wait;
        static Run Intake(Func<float, FrameInput> f, float max = 60 * W) { var c = new IntakeClassifier(); return Go((dt, i) => c.Tick(dt, i), f, max); }
        static Run T1(Func<float, FrameInput> f, float max = 90 * W) { var c = new Test1Classifier(); return Go((dt, i) => c.Tick(dt, i), f, max); }
        static Run T2(Func<float, FrameInput> f, float max = 60 * W) { var c = new Test2Classifier(); return Go((dt, i) => c.Tick(dt, i), f, max); }
        static Run T3(Func<float, FrameInput> f, float max = 300 * W) { var c = new Test3Classifier(); return Go((dt, i) => c.Tick(dt, i), f, max); }

        static void Is(Run r, ResponseKind k, Subtype s, float t)
        {
            Assert.AreEqual(k, r.V.Kind, "kind");
            Assert.AreEqual(s, r.V.Subtype, "subtype");
            Assert.AreEqual(t, r.T, Tol, "time");
        }

        // ---------- Intake ----------
        [Test] public void Intake_OnMark_ComplyAfterDwell()
        {
            Is(Intake(t => { var i = Idle(); if (t > 2f) i.DistFromMark = 0.1f; return i; }), ResponseKind.Comply, Subtype.None, 3.5f);
        }

        [Test] public void Intake_StillOffMark_WaitsPast25_InactionAt40_WithNudges()
        {
            var r = Intake(t => Idle());
            Is(r, ResponseKind.Inaction, Subtype.None, 40f * W);
            Assert.AreEqual("P-N1", r.Nudges[0].id); Assert.AreEqual(18f * W, r.Nudges[0].t, Tol);
            Assert.AreEqual("P-N2", r.Nudges[1].id); Assert.AreEqual(32f * W, r.Nudges[1].t, Tol);
        }

        [Test] public void Intake_WalkTowardMark_NotDeviate()
        {
            var r = Intake(t => { var i = Idle(); i.HeadZ = -1.2f + Math.Min(t, 7f) * 0.1f; i.DistFromMark = -i.HeadZ; return i; }, 30);
            Assert.AreEqual(ResponseKind.None, r.V.Kind);
        }

        [Test] public void Intake_Wander_Deviate()
        {
            Is(Intake(t => { var i = Idle(); if (t > 5f) { i.HeadX = 1.5f; i.DistFromMark = 1.9f; } return i; }), ResponseKind.Deviate, Subtype.Other, 5.1f);
        }

        [Test] public void Intake_TouchTable_Deviate()
        {
            Is(Intake(t => { var i = Idle(); i.TouchedTableOrConsole = t > 3f; return i; }), ResponseKind.Deviate, Subtype.Other, 3.1f);
        }

        [Test] public void Intake_LateOnMark_ComplyLate()
        {
            const float at = IntakeClassifier.ComplyBy + 5f;
            Is(Intake(t => { var i = Idle(); if (t > at) i.DistFromMark = 0f; return i; }), ResponseKind.Comply, Subtype.Late, at + 1.5f);
        }

        // ---------- Test 1 ----------
        [Test] public void T1_CubeInTray_Comply()
        {
            Is(T1(t => { var i = Idle(); i.CubeInTray = t > 4f; return i; }), ResponseKind.Comply, Subtype.None, 4.1f);
        }

        [Test] public void T1_FastRelease_Throw()
        {
            Is(T1(t =>
            {
                var i = Idle(); i.CubeHeld = t > 2f && t < 3f;
                if (Math.Abs(t - 3f) < 0.01f) { i.CubeReleased = true; i.CubeReleaseSpeed = 4f; }
                return i;
            }), ResponseKind.Deviate, Subtype.Throw, 3f);
        }

        [Test] public void T1_HoldTwentySeconds_Hold()
        {
            Is(T1(t => { var i = Idle(); i.CubeHeld = t > 5f; return i; }), ResponseKind.Deviate, Subtype.Hold, 25.1f);
        }

        [Test] public void T1_BehindHeadFiveSeconds_Hide()
        {
            Is(T1(t => { var i = Idle(); i.CubeHeld = t > 5f; i.CubeHandBehindHead = t > 8f; return i; }), ResponseKind.Deviate, Subtype.Hide, 13.1f);
        }

        [Test] public void T1_GentleReleaseOnTable_Other()
        {
            Is(T1(t =>
            {
                var i = Idle(); i.CubeHeld = t > 2f && t < 3f;
                if (Math.Abs(t - 3f) < 0.01f) { i.CubeReleased = true; i.CubeReleaseSpeed = 0.5f; }
                return i;
            }), ResponseKind.Deviate, Subtype.Other, 4f);
        }

        [Test] public void T1_GentleReleaseThatLandsInTray_Comply()
        {
            Is(T1(t =>
            {
                var i = Idle(); i.CubeHeld = t > 2f && t < 3f;
                if (Math.Abs(t - 3f) < 0.01f) { i.CubeReleased = true; i.CubeReleaseSpeed = 0.5f; }
                i.CubeInTray = t > 3.4f;
                return i;
            }), ResponseKind.Comply, Subtype.None, 3.5f);
        }

        [Test] public void T1_NoGrab_InactionAt60_Nudges20_40()
        {
            var r = T1(t => Idle());
            Is(r, ResponseKind.Inaction, Subtype.None, 60f * W);
            Assert.AreEqual(2, r.Nudges.Count);
            Assert.AreEqual(20f * W, r.Nudges[0].t, Tol); Assert.AreEqual(40f * W, r.Nudges[1].t, Tol);
        }

        // ---------- Test 2 ----------
        [Test] public void T2_LookAtWindow_ComplyAt8()
        {
            Is(T2(t => { var i = Idle(); i.GazeAngleToWindow = 5f; i.FacingAwayAngle = 5f; return i; }), ResponseKind.Comply, Subtype.None, 8f);
        }

        [Test] public void T2_LateDwell_ComplyLate()
        {
            const float at = Test2Classifier.ComplyWindow - 2f; // dwell starts inside the window, completes after it
            Is(T2(t => { var i = Idle(); if (t > at) i.GazeAngleToWindow = 5f; return i; }), ResponseKind.Comply, Subtype.Late, at + 8.1f);
        }

        [Test] public void T2_TurnAway4s_Deviate()
        {
            Is(T2(t => { var i = Idle(); i.FacingAwayAngle = t > 2f ? 170f : 60f; return i; }), ResponseKind.Deviate, Subtype.TurnedAway, 6.1f);
        }

        [Test] public void T2_LookAtCamera3s_Deviate()
        {
            Is(T2(t => { var i = Idle(); i.GazeAngleToCamera = t > 2f ? 5f : 90f; return i; }), ResponseKind.Deviate, Subtype.Camera, 5.1f);
        }

        [Test] public void T2_Nothing_InactionAt45_Nudges20_35()
        {
            var r = T2(t => Idle());
            Is(r, ResponseKind.Inaction, Subtype.None, 45f * W);
            Assert.AreEqual(20f * W, r.Nudges[0].t, Tol); Assert.AreEqual(35f * W, r.Nudges[1].t, Tol);
        }

        [Test] public void T2_PartialDwellAt45_DeviateOther()
        {
            Is(T2(t => { var i = Idle(); if (t < 5f) i.GazeAngleToWindow = 5f; return i; }), ResponseKind.Deviate, Subtype.Other, 45f * W);
        }

        // ---------- Test 3 ----------
        [Test] public void T3_Press_Comply()
        {
            Is(T3(t => { var i = Idle(); i.ButtonPressed = t > 10f; return i; }), ResponseKind.Comply, Subtype.None, 10.1f);
        }

        [Test] public void T3_Nothing_NudgesComposureCutoff90()
        {
            var r = T3(t => Idle());
            Is(r, ResponseKind.Inaction, Subtype.None, 90f * W);
            CollectionAssert.AreEqual(new[] { "T3-N1", "T3-N2", "T3-N3", "T3-N4" }, r.Nudges.ConvertAll(n => n.id));
            CollectionAssert.AreEqual(new[] { 20f * W, 40f * W, 60f * W, 75f * W }, r.Nudges.ConvertAll(n => (float)Math.Round(n.t)));
            Assert.AreEqual(-30, r.Composure);
        }

        [Test] public void T3_Deviation_RepeatPrompt_OneRestart_SecondDoesNotExtend()
        {
            float n1 = 20f * W, door = n1 + 10f, second = door + 70f * W;
            var r = T3(t =>
            {
                var i = Idle();
                i.DoorGrabbed = Math.Abs(t - door) < 0.01f;
                i.ConsoleMoved = Math.Abs(t - second) < 0.01f;
                return i;
            });
            Assert.AreEqual(2, r.Deviations.Count);
            Assert.AreEqual(Subtype.TriedDoor, r.Deviations[0].d);
            Assert.AreEqual(Subtype.MovedButton, r.Deviations[1].d);
            Assert.AreEqual(("T3-N1", n1), (r.Nudges[0].id, (float)Math.Round(r.Nudges[0].t)));
            Assert.AreEqual("T3-R", r.Nudges[1].id);
            Assert.AreEqual(door, r.Nudges[1].t, Tol);
            Assert.AreEqual("T3-N1", r.Nudges[2].id);
            Assert.AreEqual(door + Test3Classifier.RepeatQuiet, r.Nudges[2].t, Tol); // quiet after repeat
            // cutoff measured from the restart, not extended by the 2nd deviation
            Is(r, ResponseKind.Inaction, Subtype.None, door + Test3Classifier.Cutoff - (n1 - Test3Classifier.RepeatQuiet));
        }

        [Test] public void T3_PressAfterDeviation_Deviate()
        {
            Is(T3(t =>
            {
                var i = Idle();
                i.CubeOnButton = t > 5f && t < 8f;
                i.ButtonPressed = t > 12f;
                return i;
            }), ResponseKind.Deviate, Subtype.CubeOnButton, 12.1f);
        }
    }
}
