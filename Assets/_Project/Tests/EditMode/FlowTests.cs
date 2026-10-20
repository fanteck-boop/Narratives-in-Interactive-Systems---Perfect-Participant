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
            m.Apply(ResponseKind.Comply); m.Apply(ResponseKind.Comply);
            Assert.AreEqual(EndingId.E1, m.PickEnding(ResponseKind.Comply, false));
            Assert.AreEqual(EndingId.E2, m.PickEnding(ResponseKind.Deviate, false));
            Assert.AreEqual(EndingId.E3, m.PickEnding(ResponseKind.Inaction, false));
            Assert.AreEqual(EndingId.E2, m.PickEnding(ResponseKind.Inaction, true));
            var one = new BehaviourModel(); one.Apply(ResponseKind.Comply);
            Assert.AreEqual(EndingId.E2, one.PickEnding(ResponseKind.Comply, false));
        }
    }

    public class FlowTests
    {
        const ResponseKind C = ResponseKind.Comply, D = ResponseKind.Deviate, I = ResponseKind.Inaction;
        static readonly ResponseKind[] Kinds = { C, D, I };

        // Drives a full run. Narrator lines are instant (NarratorBusy false).
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
                }
                log.AddRange(f.Tick(Dt, i));
            }
            Assert.IsTrue(f.Finished, "run did not finish");
            return log;
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
            Assert.AreEqual(plan.Count(k => k == C), f.Model.C, name + " C");
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
            Assert.AreEqual(3, f.Model.C + f.Model.D + f.Model.I);
        }

        [Test] public void Force_ResolvesCurrentTest()
        {
            var f = new ExperimentFlow();
            f.JumpTo(GameState.Test1);
            var cmds = f.Force(ResponseKind.Deviate, Subtype.Throw);
            Assert.IsTrue(cmds.Any(c => c.Kind == CommandKind.Classified && c.Subtype == Subtype.Throw));
            Assert.IsTrue(f.Model.Has(Flag.ThrewCube));
        }
    }
}
