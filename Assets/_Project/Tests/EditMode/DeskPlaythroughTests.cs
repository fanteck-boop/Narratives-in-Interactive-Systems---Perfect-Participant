using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PerfectParticipant.Core;

namespace PerfectParticipant.Tests
{
    // Desk playthrough: same abstract inputs the scene produces, every ending, and the command stream only names real ids.
    public class DeskPlaythroughTests
    {
        const ResponseKind C = ResponseKind.Comply, D = ResponseKind.Deviate, I = ResponseKind.Inaction;
        static readonly ResponseKind[] Kinds = { C, D, I };

        static readonly string[] Events = typeof(RoomEvents).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()).ToArray();

        static void AssertStream(System.Collections.Generic.List<Command> log, string name)
        {
            foreach (var c in log)
            {
                if (c.Kind == CommandKind.PlayLine) Assert.IsTrue(Lines.Text.ContainsKey(c.Id), $"{name}: unknown line '{c.Id}'");
                if (c.Kind == CommandKind.RoomEvent) CollectionAssert.Contains(Events, c.Id, $"{name}: unknown room event");
            }
            foreach (var s in Refusal.Steps.Concat(Refusal.Stall))
            {
                if (s.Line != null) Assert.IsTrue(Lines.Text.ContainsKey(s.Line), "refusal line " + s.Line);
                foreach (var e in s.Events ?? new string[0]) CollectionAssert.Contains(Events, e);
            }
        }

        static ExperimentFlow Run(string name, ResponseKind[] plan, EndingId end, float press = -1f)
        {
            var f = new ExperimentFlow();
            AssertStream(FlowTests.Play(f, C, plan, press), name);
            Assert.AreEqual(end, f.Ending, name);
            return f;
        }

        [Test] public void E1_ComplyPath() => Run("E1", new[] { C, C, C }, EndingId.E1);
        [Test] public void E2_DeviatePath() => Run("E2", new[] { C, C, D }, EndingId.E2);
        [Test] public void E3_RefusalPath() => Run("E3", new[] { C, C, I }, EndingId.E3);
        [Test] public void E2_PressDuringRefusal() => Run("E2 late", new[] { C, C, I }, EndingId.E2, press: 10f);

        [Test] public void AllSequences_StreamReferencesOnlyKnownIds([Range(0, 26)] int n)
        {
            var plan = new[] { Kinds[n / 9], Kinds[n / 3 % 3], Kinds[n % 3] };
            AssertStream(FlowTests.Play(new ExperimentFlow(), C, plan), string.Join(",", plan));
        }
    }
}
