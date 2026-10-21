using System;

namespace PerfectParticipant.Core
{
    // Brief 8.1 to 8.4. No UnityEngine.
    public sealed class BehaviourModel
    {
        public int C { get; private set; }
        public int D { get; private set; }
        public int I { get; private set; }
        public int Composure { get; private set; } = 100;
        public Tier Tier { get; private set; } = Tier.Neutral;
        public Flag Flags;
        public ResponseKind PrologueResponse;

        public bool Has(Flag f) => (Flags & f) != 0;

        public void Apply(ResponseKind r)
        {
            if (r == ResponseKind.Comply) C++;
            else if (r == ResponseKind.Deviate) { D++; Composure -= 10; }
            else if (r == ResponseKind.Inaction) { I++; Composure -= 25; }
            Composure = Math.Max(0, Math.Min(100, Composure));
            Tier = Composure <= 50 ? Tier.Unclassified : PickTier();
        }

        public void AddComposure(int delta) => SetComposure(Composure + delta);

        public void SetComposure(int v)
        {
            Composure = Math.Max(0, Math.Min(100, v));
            if (Composure <= 50) Tier = Tier.Unclassified;
        }

        // Unique max wins, tie keeps previous tier.
        public Tier PickTier()
        {
            int max = Math.Max(C, Math.Max(D, I));
            if (C == max && D < max && I < max) return Tier.Valued;
            if (D == max && C < max && I < max) return Tier.Flagged;
            if (I == max && C < max && D < max) return Tier.Unclassified;
            return Tier;
        }

        // Route complies (rooms 2, 4, 7, 8) + Test3 + one of Test1/Test2.
        public const int E1Complies = 6;

        // Called after the corridor route. E1 = consented, walked the route cleanly, and complied in Test1 or Test2.
        public EndingId PickEnding(ResponseKind t3, bool pressedDuringRefusal, bool routeClean)
        {
            if (pressedDuringRefusal) return EndingId.E2;
            if (t3 == ResponseKind.Inaction) return EndingId.E3;
            return t3 == ResponseKind.Comply && routeClean && C >= E1Complies ? EndingId.E1 : EndingId.E2;
        }
    }
}
