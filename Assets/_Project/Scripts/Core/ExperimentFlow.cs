using System;
using System.Collections.Generic;

namespace PerfectParticipant.Core
{
    // Sole owner of state transitions. Feed one FrameInput per frame, apply returned commands in order.
    // Returned list is reused: consume before next call.
    public sealed partial class ExperimentFlow // corridor route (Test4) in Route.cs
    {
        public const float IntroDelay = 5f, AfterReaction = 4f, SlippedAt = 60; // 3 / 2 felt rushed (team, 2026-10-21)
        public const float EndLinger = 10f; // silence in the Hall before the card: time to look around (team 2026-10-23)
        public const int ClipboardComposure = -10; // per clipboard throw (team, 2026-10-21)
        int _clipThrows;

        public readonly BehaviourModel Model = new BehaviourModel();
        public GameState State { get; private set; }
        public float StateTime { get; private set; }
        public EndingId Ending { get; private set; }
        public bool Finished { get; private set; } // final card shown

        readonly List<Command> _out = new List<Command>();
        readonly Queue<(float delay, Action run)> _steps = new Queue<(float, Action)>();
        float _wait;
        bool _started, _listening, _e3Wait, _e3Anchored;
        float _stillT, _ax, _az;
        int _t3StartComposure, _lastComposure = 100;
        Tier _lastTier = Tier.Neutral;
        IntakeClassifier _intake; Test1Classifier _t1; Test2Classifier _t2; Test3Classifier _t3;
        ResponseKind _t3Result;           // ending is picked after Test4
        bool _pressedInRefusal, _refusalPath; // late consent / walked out after R-06 (refusal path still ends E3)

        public List<Command> Tick(float dt, in FrameInput i)
        {
            _out.Clear();
            if (!_started) { _started = true; Enter(GameState.Intake); return _out; }
            StateTime += dt;
            if (i.ReadingForm) Model.Flags |= Flag.ReadForm;
            if (i.DoorGrabbed) Model.Flags |= Flag.TriedDoor;
            // own event, any time the narrator still holds it together; the running test keeps going
            if (i.ClipboardThrown && !Finished && State != GameState.Refusal && State != GameState.EndE3)
            {
                Model.AddComposure(ClipboardComposure);
                Sync();
                Play(Lines.ClipboardThrow[_clipThrows++ % Lines.ClipboardThrow.Length]);
            }

            if (State == GameState.Refusal && i.ButtonPressed && !_pressedInRefusal) { LateConsent(); return _out; }

            // queued beats block classification; one step per tick so NarratorBusy reflects the last PlayLine
            if (_steps.Count > 0)
            {
                if (!i.NarratorBusy && (_wait += dt) >= _steps.Peek().delay) { _wait = 0f; _steps.Dequeue().run(); }
                return _out;
            }
            if (_e3Wait) { E3Wait(dt, i); return _out; }
            if (!_listening) return _out;

            Verdict v; TestId test;
            switch (State)
            {
                case GameState.Intake: v = _intake.Tick(dt, i); test = TestId.Intake; break;
                case GameState.Test1: v = _t1.Tick(dt, i); test = TestId.Test1; break;
                case GameState.Test2: v = _t2.Tick(dt, i); test = TestId.Test2; break;
                case GameState.Test3: v = _t3.Tick(dt, i); test = TestId.Test3; break;
                case GameState.Test4: RouteTick(dt, i); return _out;
                default: return _out;
            }
            if (v.Deviation != Subtype.None)
            {
                if (v.Deviation == Subtype.Throw) Model.Flags |= Flag.ThrewCube;
                Play(Lines.Reaction(TestId.Test3, ResponseKind.Deviate, v.Deviation));
            }
            if (v.Nudge != null) Play(v.Nudge);
            if (v.ComposureDelta != 0) { Model.AddComposure(v.ComposureDelta); Sync(); }
            if (v.Kind != ResponseKind.None) Resolve(test, v.Kind, v.Subtype);
            return _out;
        }

        // Debug: resolve the running test as if classified.
        public List<Command> Force(ResponseKind k, Subtype s = Subtype.None)
        {
            _out.Clear();
            var test = State == GameState.Intake ? TestId.Intake : State == GameState.Test1 ? TestId.Test1
                : State == GameState.Test2 ? TestId.Test2 : State == GameState.Test3 ? TestId.Test3 : TestId.Test4;
            if (_listening && State == GameState.Test4) { _steps.Clear(); RouteForce(k); }
            else if (_listening && State <= GameState.Test3) { _steps.Clear(); Resolve(test, k, s); }
            return _out;
        }

        // Debug: jump to a state. Model is kept as is.
        public List<Command> JumpTo(GameState s)
        {
            _out.Clear(); _started = true;
            if (s == GameState.Refusal) _t3StartComposure = Model.Composure;
            Enter(s);
            return _out;
        }

        // Debug: set composure and tell the room (SetComposure / SetTier on change).
        public List<Command> SetComposure(int v)
        {
            _out.Clear();
            Model.SetComposure(v);
            Sync();
            return _out;
        }

        void Resolve(TestId test, ResponseKind k, Subtype s)
        {
            _listening = false;
            if (test == TestId.Intake)
            {
                Model.PrologueResponse = k;
                Play(Lines.Reaction(test, k, s));
                _out.Add(Command.Class(test, k, s));
                Then(AfterReaction, () => Enter(GameState.Test1));
                return;
            }
            if (test == TestId.Test3)
            {
                bool late = k == ResponseKind.Comply && Model.I >= 2;
                Model.Apply(k);
                _out.Add(Command.Class(test, k, s));
                Sync();
                if (k == ResponseKind.Inaction) { Enter(GameState.Refusal); return; }
                if (late) Model.Flags |= Flag.LateConsent;
                Play(late ? "T3-Cl" : "T3-C");
                _t3Result = k;
                Then(AfterReaction, ToCorridor);
                return;
            }

            Model.Apply(k);
            if (s == Subtype.Throw) Model.Flags |= Flag.ThrewCube;
            if (s == Subtype.Hide) Model.Flags |= Flag.HidCube;
            if (s == Subtype.TurnedAway) Model.Flags |= Flag.TurnedAway;
            Play(Lines.Reaction(test, k, s));
            _out.Add(Command.Class(test, k, s));
            Sync();
            if (test == TestId.Test1) { Then(AfterReaction, () => Enter(GameState.Test2)); return; }

            _out.Add(Command.Room(k == ResponseKind.Comply ? RoomEvents.ObserversNod
                : k == ResponseKind.Deviate ? RoomEvents.ObserversWriteFast : RoomEvents.ObserversLeanIn));
            Then(AfterReaction, () => Play(Lines.ForTier("BR", Model.Tier))); // interlude
            Then(AfterReaction, () => Enter(GameState.Test3));
        }

        void Enter(GameState s)
        {
            State = s; StateTime = 0f; _steps.Clear(); _wait = 0f; _listening = false; _e3Wait = false;
            _out.Add(Command.To(s));
            switch (s)
            {
                case GameState.Intake:
                    _intake = new IntakeClassifier();
                    Then(IntroDelay, () => Play("P-01"));
                    Then(0f, () => { Play("P-02"); _listening = true; });
                    break;
                case GameState.Test1:
                    _t1 = new Test1Classifier(); _listening = true;
                    Play("T1-01");
                    if (Model.PrologueResponse == ResponseKind.Deviate) Play("T1-01b");
                    break;
                case GameState.Test2:
                    _t2 = new Test2Classifier(); _listening = true;
                    _out.Add(Command.Room(RoomEvents.ObserversIn));
                    Play(Lines.ForTier("T2-01", Model.Tier));
                    break;
                case GameState.Test3:
                    _t3 = new Test3Classifier(); _listening = true;
                    _t3StartComposure = Model.Composure;
                    Play(Lines.ForTier("T3-01", Model.Tier));
                    break;
                case GameState.Test4:
                    RouteStart();
                    if (!_refusalPath) _out.Add(Command.Room(RoomEvents.DoorUnlock)); // refusal: R-06 already opened it
                    break;
                case GameState.Refusal:
                    if (_t3StartComposure > 50) foreach (var st in Refusal.Stall) Beat(st);
                    for (int n = 0; n < Refusal.Steps.Length - 1; n++) Beat(Refusal.Steps[n]);
                    Then(Refusal.Steps[Refusal.Steps.Length - 1].Delay, () => Enter(GameState.EndE3));
                    break;
                case GameState.EndE1:
                    Ending = EndingId.E1;
                    Then(1f, () => Play("E1-01"));
                    Then(1f, () => Play("E1-02"));
                    Unmask();
                    Then(1f, () => Play("E1-03"));
                    Then(EndLinger, () => Card(Cards.AsExpected, null));
                    break;
                case GameState.EndE2:
                    Ending = EndingId.E2;
                    Then(1f, () => Play("E2-01"));
                    Then(1f, () => Play("E2-02"));
                    if (Model.D == 3) Then(1f, () => Play("E2-03a"));
                    if (Model.Has(Flag.LateConsent)) Then(1f, () => Play("E2-03b"));
                    Unmask();
                    Then(1f, () => Play("E2-03"));
                    Then(EndLinger, () => Card(Cards.AsExpected, null));
                    break;
                case GameState.EndE3:
                    Ending = EndingId.E3;
                    Do(Refusal.Steps[Refusal.Steps.Length - 1]);
                    _e3Wait = true; _e3Anchored = false; _stillT = 0f;
                    break;
            }
        }

        // The formal ending first, then the voice drops the mask, then the offer (E1-03 / E2-03). Only in the Hall.
        void Unmask()
        {
            if (!_inHall) return;
            Then(1f, () => Play("H-01"));
            Then(1f, () => Play("H-02"));
        }

        void LateConsent()
        {
            _steps.Clear(); _wait = 0f;
            _out.Add(Command.Stop());
            Model.SetComposure(100);
            Model.Flags |= Flag.LateConsent;
            Sync();
            Play("R-C");
            _pressedInRefusal = true; // ending E2, picked after Test4
            Then(AfterReaction, ToCorridor);
        }

        // Transition first: DialogueManager clears queued lines on Transition, T3-Go must come after it.
        void ToCorridor()
        {
            Enter(GameState.Test4);
            Play("T3-Go");
        }

        // E3 after the door opens: card once still for StillBeforeCard; walking out leads into the corridor (Test4).
        void E3Wait(float dt, in FrameInput i)
        {
            float dx = i.HeadX - _ax, dz = i.HeadZ - _az;
            if (!_e3Anchored || dx * dx + dz * dz > Refusal.StillMove * Refusal.StillMove)
            {
                _e3Anchored = true; _ax = i.HeadX; _az = i.HeadZ; _stillT = 0f;
            }
            else _stillT += dt;
            if (i.HeadZ < Refusal.DoorZ - Refusal.ThroughDoor) { _refusalPath = true; Enter(GameState.Test4); }
            else if (_stillT >= Refusal.StillBeforeCard) { _e3Wait = false; Card(Cards.None, Cards.NoneSub); }
        }

        void Then(float delay, Action run) => _steps.Enqueue((delay, run));
        void Beat(RefusalStep st) => Then(st.Delay, () => Do(st));

        void Do(RefusalStep st)
        {
            foreach (var e in st.Events) _out.Add(Command.Room(e));
            if (st.Line != null) Play(st.Line);
        }

        void Play(string id) => _out.Add(Command.Line(id, Model.Composure <= SlippedAt));

        void Card(string title, string sub) { _out.Add(Command.Card(title, sub)); Finished = true; }

        void Sync()
        {
            if (Model.Composure != _lastComposure) { _lastComposure = Model.Composure; _out.Add(Command.Composure(_lastComposure)); }
            if (Model.Tier != _lastTier) { _lastTier = Model.Tier; _out.Add(Command.TierCmd(_lastTier)); }
        }
    }
}
