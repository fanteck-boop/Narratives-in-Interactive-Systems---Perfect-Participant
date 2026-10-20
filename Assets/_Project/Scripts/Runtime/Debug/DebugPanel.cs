using System.Collections.Generic;
using PerfectParticipant.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PerfectParticipant
{
    // Desk-test panel. F1 toggles. Editor and Development builds only (empty class in release).
    public class DebugPanel : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static readonly GameState[] Jumps = { GameState.Intake, GameState.Test1, GameState.Test2, GameState.Test3, GameState.Refusal, GameState.EndE1, GameState.EndE2, GameState.EndE3 };
        static readonly float[] Scales = { 1f, 2f, 5f };

        ExperimentRunner runner;
        DialogueManager dialogue;
        readonly Queue<string> log = new Queue<string>();
        string line = "";
        bool show = true;

        void OnEnable()
        {
            runner = FindFirstObjectByType<ExperimentRunner>();
            dialogue = FindFirstObjectByType<DialogueManager>();
            if (runner) runner.CommandIssued += On;
        }

        void OnDisable()
        {
            if (runner) runner.CommandIssued -= On;
            Time.timeScale = 1f;
        }

        void On(Command c)
        {
            if (c.Kind == CommandKind.PlayLine) line = c.Id;
            log.Enqueue(Time.time.ToString("F1") + " " + c);
            if (log.Count > 8) log.Dequeue();
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k != null && k.f1Key.wasPressedThisFrame) show = !show;
        }

        void OnGUI()
        {
            if (!show || !runner) return;
            var f = runner.Flow;
            var m = f.Model;
            GUILayout.BeginArea(new Rect(10, 10, 360, Screen.height - 20), GUI.skin.box);
            GUILayout.Label($"{f.State}  t={f.StateTime:F1}s  end={f.Ending}  done={f.Finished}");
            GUILayout.Label($"C{m.C} D{m.D} I{m.I}  tier={m.Tier}  flags={m.Flags}");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Composure " + m.Composure, GUILayout.Width(95));
            int c = Mathf.RoundToInt(GUILayout.HorizontalSlider(m.Composure, 0, 100));
            if (c != m.Composure) runner.SetComposure(c);
            GUILayout.EndHorizontal();

            GUILayout.Label("Force current test");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Comply")) runner.Force(ResponseKind.Comply);
            if (GUILayout.Button("Deviate")) runner.Force(ResponseKind.Deviate);
            if (GUILayout.Button("Inaction")) runner.Force(ResponseKind.Inaction);
            GUILayout.EndHorizontal();

            GUILayout.Label("Jump to");
            for (int i = 0; i < Jumps.Length; i += 4)
            {
                GUILayout.BeginHorizontal();
                for (int j = i; j < i + 4; j++) if (GUILayout.Button(Jumps[j].ToString())) runner.JumpTo(Jumps[j]);
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            foreach (var s in Scales) if (GUILayout.Button(s + "x")) Time.timeScale = s;
            GUILayout.Label("scale " + Time.timeScale);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restart")) { Time.timeScale = 1f; runner.Restart(); }
            var seat = FindFirstObjectByType<SeatedMode>();
            if (seat && GUILayout.Button("Seated " + (SeatedMode.Seated ? "on" : "off"))) seat.Toggle();
            if (dialogue)
            {
                if (GUILayout.Button("Subs " + (dialogue.subtitlesOn ? "on" : "off"))) dialogue.subtitlesOn = !dialogue.subtitlesOn;
                if (GUILayout.Button("Skip line")) dialogue.Skip();
            }
            GUILayout.EndHorizontal();

            Lines.Text.TryGetValue(line, out var text);
            GUILayout.Label($"Narrator{(dialogue && dialogue.Busy ? " (busy)" : "")}: {line} {text}");
            GUILayout.Label("Last commands");
            foreach (var s in log) GUILayout.Label(s);
            GUILayout.EndArea();
        }
#endif
    }
}
