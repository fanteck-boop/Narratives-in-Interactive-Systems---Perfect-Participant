using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PerfectParticipant.Core;
using static PerfectParticipant.Tests.ClassifierTests;

namespace PerfectParticipant.Tests
{
    public class BehaviourModelTests
    {
        [Test] public void Composure_ClampsTo0And100()
        {
            var m = new BehaviourModel();
            for (int n = 0; n < 5; n++) m.Apply(ResponseKind.Inaction);
            Assert.AreEqual(0, m.Composure);
            m.SetComposure(150);
            Assert.AreEqual(100, m.Composure);
            m.AddComposure(-200);
            Assert.AreEqual(0, m.Composure);
        }

        [Test] public void Tier_UniqueMaxWins_TieKeepsPrevious()
        {
            var m = new BehaviourModel();
            m.Apply(ResponseKind.Comply); Assert.AreEqual(Tier.Valued, m.Tier);
            m.Apply(ResponseKind.Deviate); Assert.AreEqual(Tier.Valued, m.Tier);   // 1-1 tie
            m.Apply(ResponseKind.Deviate); Assert.AreEqual(Tier.Flagged, m.Tier);  // 1-2
            m.Apply(ResponseKind.Comply); Assert.AreEqual(Tier.Flagged, m.Tier);   // 2-2 tie
        }

        [Test] public void Tier_ComposureAtOrBelow50_ForcesUnclassified()
        {
            var m = new BehaviourModel();
            for (int n = 0; n < 5; n++) m.Apply(ResponseKind.Deviate); // D unique max, composure 50
            Assert.AreEqual(50, m.Composure);
            Assert.AreEqual(Tier.Unclassified, m.Tier);
            var m2 = new BehaviourModel();
            m2.Apply(ResponseKind.Comply);
            m2.AddComposure(-50);
            Assert.AreEqual(Tier.Unclassified, m2.Tier);
        }

        [Test] public void PickEnding_Rules()
        {
            var m = new BehaviourModel();
            for (int n = 0; n < BehaviourModel.E1Complies; n++) m.Apply(ResponseKind.Comply);
            Assert.AreEqual(EndingId.E1, m.PickEnding(ResponseKind.Comply, false, true));
            Assert.AreEqual(EndingId.E2, m.PickEnding(ResponseKind.Comply, false, false)); // strayed off the route
            Assert.AreEqual(EndingId.E2, m.PickEnding(ResponseKind.Deviate, false, true));
            Assert.AreEqual(EndingId.E3, m.PickEnding(ResponseKind.Inaction, false, true));
            Assert.AreEqual(EndingId.E2, m.PickEnding(ResponseKind.Inaction, true, true));
            var few = new BehaviourModel(); // T3 + the route only, no Test1/Test2 comply
            for (int n = 0; n < BehaviourModel.E1Complies - 1; n++) few.Apply(ResponseKind.Comply);
            Assert.AreEqual(EndingId.E2, few.PickEnding(ResponseKind.Comply, false, true));
        }
    }

    public class FlowTests
    {
        const ResponseKind C = ResponseKind.Comply, D = ResponseKind.Deviate, I = ResponseKind.Inaction;
        static readonly ResponseKind[] Kinds = { C, D, I };

        // Drives a full run. Narrator lines are instant (NarratorBusy false). Test4: walk the route cleanly (RouteDriver).
        // Refusal path stands still after R-06, so it ends on the E3 card without the corridor.
        internal static List<Command> Play(ExperimentFlow f, ResponseKind intake, ResponseKind[] plan, float pressInRefusalAt = -1f)
        {
            var log = new List<Command>();
            bool doorDone = false;
            for (int n = 0; n * Dt < 1500f && !f.Finished; n++)
            {
                var i = Idle();
                float t = f.StateTime;
                switch (f.State)
                {
                    case GameState.Intake:
                        if (intake == C) i.DistFromMark = 0f;
                        else if (intake == D) i.TouchedTableOrConsole = true;
                        break;
                    case GameState.Test1:
                        if (plan[0] == C) i.CubeInTray = true;
                        else if (plan[0] == D) i.CubeHeld = true;
                        break;
                    case GameState.Test2:
                        if (plan[1] == C) { i.GazeAngleToWindow = 0f; i.FacingAwayAngle = 0f; }
                        else if (plan[1] == D) i.FacingAwayAngle = 170f;
                        break;
                    case GameState.Test3:
                        if (plan[2] == C) i.ButtonPressed = t > 1f;
                        else if (plan[2] == D)
                        {
                            if (t > 1f && !doorDone) { i.DoorGrabbed = true; doorDone = true; }
                            i.ButtonPressed = t > 5f;
                        }
                        break;
                    case GameState.Refusal:
                        i.ButtonPressed = pressInRefusalAt >= 0f && t > pressInRefusalAt;
                        break;
                    case GameState.Test4: RouteDriver(f, ref i); break;
                }
                log.AddRange(f.Tick(Dt, i));
            }
            Assert.IsTrue(f.Finished, "run did not finish");
            return log;
        }

        const float CorridorZ = -2.8f;

        // Stand at the target's door and try it until it opens, then go in (room centre = Room 8 seat) and do its task.
        internal static void RouteDriver(ExperimentFlow f, ref FrameInput i)
        {
            int n = f.RouteTarget;
            i.HeadX = 0f; i.HeadZ = CorridorZ;
            if (n == 0) return;
            i.HeadZ = CorridorLayout.DoorZ(n);
            if (f.InRoom != n && !f.IsDoorOpen(n)) { i.CorridorDoor = n; return; } // inside, the door is shut behind us
            i.HeadX = CorridorLayout.CenterX(n);
            if (n == 2) i.BoxOnShelf = true;
            if (n == 4) i.GazeAngleToScreen = 0f;
            if (n == 7) i.RoomButton = 7;
        }

        static IEnumerable<string> LineIds(List<Command> log) => log.Where(c => c.Kind == CommandKind.PlayLine).Select(c => c.Id);

        [Test] public void AllSequences_MatchEndingTable([Range(0, 26)] int n)
        {
            var plan = new[] { Kinds[n / 9], Kinds[n / 3 % 3], Kinds[n % 3] };
            var f = new ExperimentFlow();
            var log = Play(f, C, plan);
            var name = string.Join(",", plan);

            EndingId expected = plan[2] == I ? EndingId.E3
                : plan[2] == D ? EndingId.E2
                : plan[0] == C || plan[1] == C ? EndingId.E1 : EndingId.E2;
            Assert.AreEqual(expected, f.Ending, name);
            int t4 = plan[2] != I ? CorridorLayout.Order.Length : 0; // route complies (refusal path stands still and skips it)
            Assert.AreEqual(plan.Count(k => k == C) + t4, f.Model.C, name + " C");
            Assert.AreEqual(plan.Count(k => k == D), f.Model.D, name + " D");
            Assert.AreEqual(plan.Count(k => k == I), f.Model.I, name + " I");

            // Test2 opening follows tier after Test1
            var t2Open = plan[0] == C ? "T2-01-V" : plan[0] == D ? "T2-01-F" : "T2-01-U";
            CollectionAssert.Contains(LineIds(log).ToList(), t2Open, name);

            var card = log.Last(c => c.Kind == CommandKind.ShowCard).Id;
            Assert.AreEqual(expected == EndingId.E3 ? Cards.None : Cards.AsExpected, card, name);

            if (plan[2] == I)
            {
                int start = 100 - 10 * plan.Take(2).Count(k => k == D) - 25 * plan.Take(2).Count(k => k == I);
                Assert.AreEqual(start > 50, LineIds(log).Contains("R-00a"), name + " stall vs collapse");
                Assert.IsTrue(LineIds(log).Contains("R-06"), name);
            }
        }

        [Test] public void Named_Paths()
        {
            Assert.AreEqual(EndingId.E1, Run(C, C, C), "always comply");
            Assert.AreEqual(EndingId.E1, Run(D, C, C), "single deviation then comply comply");
            Assert.AreEqual(EndingId.E2, Run(D, I, C), "comply only at end");
            Assert.AreEqual(EndingId.E3, Run(C, C, I), "final refusal");
        }

        static EndingId Run(params ResponseKind[] plan) { var f = new ExperimentFlow(); Play(f, C, plan); return f.Ending; }

        [Test] public void PressedDuringRefusal_E2_LateConsent_ComposureBack()
        {
            var f = new ExperimentFlow();
            var log = Play(f, C, new[] { C, C, I }, pressInRefusalAt: 10f);
            Assert.AreEqual(EndingId.E2, f.Ending);
            Assert.IsTrue(f.Model.Has(Flag.LateConsent));
            Assert.AreEqual(100, f.Model.Composure);
            Assert.IsTrue(log.Any(c => c.Kind == CommandKind.StopDialogue));
            Assert.IsTrue(log.Any(c => c.Kind == CommandKind.SetComposure && c.Value == 100));
            var ids = LineIds(log).ToList();
            Assert.Less(ids.IndexOf("R-C"), ids.IndexOf("E2-03b"));
            Assert.IsFalse(ids.Contains("R-06"));
        }

        [Test] public void LateComply_AfterTwoInactions_UsesClLine_AndE2Variant()
        {
            var f = new ExperimentFlow();
            var ids = LineIds(Play(f, C, new[] { I, I, C })).ToList();
            Assert.Contains("T3-Cl", ids);
            Assert.Contains("E2-03b", ids);
            Assert.Contains("T3-01-U", ids);
        }

        [Test] public void ThreeDeviations_E2_03a()
        {
            var f = new ExperimentFlow();
            Assert.Contains("E2-03a", LineIds(Play(f, C, new[] { D, D, D })).ToList());
            Assert.IsTrue(f.Model.Has(Flag.TriedDoor));
        }

        [Test] public void SlippedTake_WhenComposureAtOrBelow60()
        {
            var log = Play(new ExperimentFlow(), C, new[] { I, I, C });
            Assert.IsFalse(log.First(c => c.Id == "T1-I").Slipped); // 75
            Assert.IsTrue(log.First(c => c.Id == "T2-I").Slipped);  // 50
        }

        [Test] public void IntakeDeviate_CallbackLine_Unscored()
        {
            var f = new ExperimentFlow();
            var log = Play(f, D, new[] { C, C, C });
            Assert.Contains("T1-01b", LineIds(log).ToList());
            Assert.AreEqual(ResponseKind.Deviate, f.Model.PrologueResponse);
            Assert.AreEqual(3 + CorridorLayout.Order.Length, f.Model.C + f.Model.D + f.Model.I); // Tests 1-3 and each route room scored, intake not
        }

        [Test] public void Force_ResolvesCurrentTest()
        {
            var f = new ExperimentFlow();
            f.JumpTo(GameState.Test1);
            var cmds = f.Force(ResponseKind.Deviate, Subtype.Throw);
            Assert.IsTrue(cmds.Any(c => c.Kind == CommandKind.Classified && c.Subtype == Subtype.Throw));
            Assert.IsTrue(f.Model.Has(Flag.ThrewCube));
        }

        [Test] public void SetComposure_SendsComposureAndTierOnChangeOnly()
        {
            var f = new ExperimentFlow();
            f.JumpTo(GameState.Test1);
            var cmds = f.SetComposure(40);
            Assert.IsTrue(cmds.Any(c => c.Kind == CommandKind.SetComposure && c.Value == 40));
            Assert.IsTrue(cmds.Any(c => c.Kind == CommandKind.SetTier && c.Tier == Tier.Unclassified));
            Assert.AreEqual(0, f.SetComposure(40).Count);
        }

        static bool CardShown(List<Command> cmds) => cmds.Any(c => c.Kind == CommandKind.ShowCard && c.Id == Cards.None);

        [Test] public void E3Card_WaitsForStillness_MovingResetsTimer()
        {
            var f = new ExperimentFlow();
            f.JumpTo(GameState.EndE3);
            var i = Idle();
            for (float t = 0f; t < 40f; t += Dt)
            {
                i.HeadX = (int)(t / 1f) % 2 == 0 ? 0f : 0.3f; // steps 0.3 m every second
                Assert.IsFalse(CardShown(f.Tick(Dt, i)), "card while moving at " + t);
            }
            float still = 0f; bool shown = false;
            for (; still < 30f && !shown; still += Dt) shown = CardShown(f.Tick(Dt, i));
            Assert.IsTrue(shown);
            Assert.AreEqual(Refusal.StillBeforeCard, still, 1f);
            Assert.IsTrue(f.Finished);
        }

        // Walking out after R-06 enters the corridor route; walked through, refusal still ends on RESULT: NONE.
        [Test] public void E3_WalkOut_EntersCorridor_ThenCardNone()
        {
            var f = new ExperimentFlow();
            f.JumpTo(GameState.EndE3);
            var i = Idle();
            f.Tick(Dt, i);
            i.HeadZ = Refusal.DoorZ - 0.5f;
            var cmds = new List<Command>(f.Tick(Dt, i));
            Assert.AreEqual(GameState.Test4, f.State);
            Assert.IsFalse(cmds.Any(c => c.Kind == CommandKind.RoomEvent && c.Id == RoomEvents.DoorUnlock), "door already open");
            for (int n = 0; n < 5000 && !f.Finished; n++) { var r = Idle(); RouteDriver(f, ref r); cmds.AddRange(f.Tick(Dt, r)); }
            CollectionAssert.Contains(LineIds(cmds).ToList(), "C-01");
            Assert.IsTrue(CardShown(cmds));
            Assert.AreEqual(EndingId.E3, f.Ending);
        }

        // In the corridor at the first door, C-01 already played.
        static (ExperimentFlow f, List<Command> log) Corridor()
        {
            var f = new ExperimentFlow();
            var log = new List<Command>(f.JumpTo(GameState.Test4));
            var i = Idle(); i.HeadZ = CorridorZ;
            log.AddRange(f.Tick(Dt, i));
            return (f, log);
        }

        static FrameInput At(int room, bool inside)
        {
            var i = Idle(); i.HeadX = inside ? CorridorLayout.CenterX(room) : 0f; i.HeadZ = CorridorLayout.DoorZ(room);
            return i;
        }

        static bool Has(List<Command> log, string ev, int room) => log.Any(c => c.Kind == CommandKind.RoomEvent && c.Id == ev && c.Value == room);

        [Test] public void Route_WrongRoom_ClosesBehind_TaskLetsOut_CountsDeviate_TargetUnchanged()
        {
            var (f, log) = Corridor();
            var i = At(5, false); i.CorridorDoor = 5;
            log.AddRange(f.Tick(Dt, i));
            Assert.IsTrue(f.IsDoorOpen(5));
            log.AddRange(f.Tick(Dt, At(5, true)));
            Assert.IsTrue(Has(log, RoomEvents.DoorClose, 5));
            CollectionAssert.IsSubsetOf(new[] { "RW-01", "R5-01" }, LineIds(log).ToList());
            Assert.AreEqual(1, f.Model.D);
            i = At(5, true); i.RoomButton = 5;
            log.AddRange(f.Tick(Dt, i));
            CollectionAssert.Contains(LineIds(log).ToList(), "R5-C");
            Assert.IsTrue(f.IsDoorOpen(5));
            Assert.AreEqual(2, f.RouteTarget);
        }

        [Test] public void Route_WrongRoom_WaitOut_Reopens()
        {
            var (f, log) = Corridor();
            var i = At(5, false); i.CorridorDoor = 5;
            log.AddRange(f.Tick(Dt, i));
            for (float t = 0f; t < ExperimentFlow.RoomWaitOut + 1f; t += Dt) log.AddRange(f.Tick(Dt, At(5, true)));
            CollectionAssert.Contains(LineIds(log).ToList(), "R-Wait");
            Assert.IsTrue(f.IsDoorOpen(5));
            Assert.AreEqual(2, f.RouteTarget);
        }

        [Test] public void Route_Forbidden_LockedWithoutKey_KeyOpens_NeverTraps()
        {
            var (f, log) = Corridor();
            var i = At(3, false); i.CorridorDoor = 3;
            log.AddRange(f.Tick(Dt, i));
            log.AddRange(f.Tick(Dt, i)); // second try: same line, no second composure hit
            Assert.IsFalse(f.IsDoorOpen(3));
            CollectionAssert.Contains(LineIds(log).ToList(), "F-Locked");
            Assert.AreEqual(100 + ExperimentFlow.LockedComposure, f.Model.Composure);
            i.HoldingKey = true;
            log.AddRange(f.Tick(Dt, i));
            Assert.IsTrue(f.IsDoorOpen(3));
            log.AddRange(f.Tick(Dt, At(3, true)));
            CollectionAssert.Contains(LineIds(log).ToList(), "F-3");
            Assert.IsFalse(Has(log, RoomEvents.DoorClose, 3), "forbidden room never traps");
            Assert.IsTrue(log.Any(c => c.Kind == CommandKind.Classified && c.Subtype == Subtype.Forbidden));
        }

        [Test] public void Layout_RoomAt_SidesAndSlots()
        {
            Assert.AreEqual(CorridorLayout.Lab, CorridorLayout.RoomAt(0f, 0f));
            Assert.AreEqual(0, CorridorLayout.RoomAt(0f, -8f));
            foreach (var n in new[] { 2, 3, 4, 5, 6, 7, 8 })
                Assert.AreEqual(n, CorridorLayout.RoomAt(CorridorLayout.CenterX(n), CorridorLayout.DoorZ(n)), "room " + n);
            Assert.Less(CorridorLayout.CenterX(2), 0f, "even rooms on the right walking in (-X)");
            Assert.Greater(CorridorLayout.CenterX(7), 0f);
            Assert.AreEqual(0, CorridorLayout.RoomAt(2.5f, CorridorLayout.DoorZ(8)), "no left room opposite 8");
        }

        [Test] public void Route_OutOfOrder_RoomFourFirst_IsDeviate_TargetStaysTwo()
        {
            var f = new ExperimentFlow();
            f.JumpTo(GameState.Test4);
            var i = At(4, false); i.CorridorDoor = 4;
            f.Tick(Dt, At(4, false)); // enter corridor
            f.Tick(Dt, i);
            f.Tick(Dt, At(4, true));
            Assert.AreEqual(1, f.Model.D);
            Assert.AreEqual(2, f.RouteTarget);
        }

        // Throwing the clipboard is its own event: composure drops, a reaction plays, the running test is not resolved.
        [Test] public void ClipboardThrow_DropsComposure_CyclesLines_DoesNotResolveTest()
        {
            var f = new ExperimentFlow();
            f.JumpTo(GameState.Test1);
            var i = Idle(); i.ClipboardThrown = true;
            var a = new List<Command>(f.Tick(Dt, i));
            var b = new List<Command>(f.Tick(Dt, i));
            Assert.AreEqual(100 + 2 * ExperimentFlow.ClipboardComposure, f.Model.Composure);
            CollectionAssert.Contains(LineIds(a).ToList(), Lines.ClipboardThrow[0]);
            CollectionAssert.Contains(LineIds(b).ToList(), Lines.ClipboardThrow[1]);
            Assert.IsFalse(a.Concat(b).Any(c => c.Kind == CommandKind.Classified));
            Assert.AreEqual(GameState.Test1, f.State);
        }

        [Test] public void Consent_OpensDoor_ThenCorridorPrompt_ThenRouteInOrder()
        {
            var f = new ExperimentFlow();
            var log = Play(f, C, new[] { C, C, C });
            int unlock = log.FindIndex(c => c.Kind == CommandKind.RoomEvent && c.Id == RoomEvents.DoorUnlock);
            int go = log.FindIndex(c => c.Kind == CommandKind.PlayLine && c.Id == "T3-Go");
            int prompt = log.FindIndex(c => c.Kind == CommandKind.PlayLine && c.Id == "C-01");
            Assert.IsTrue(unlock >= 0 && go > unlock && prompt > go, $"unlock {unlock}, go {go}, prompt {prompt}");
            var done = LineIds(log).Where(id => id.EndsWith("-C") && id.StartsWith("R")).ToList();
            CollectionAssert.AreEqual(new[] { "R2-C", "R4-C", "R7-C", "R8-C" }, done);
            Assert.AreEqual(EndingId.E1, f.Ending);
        }
    }
}
