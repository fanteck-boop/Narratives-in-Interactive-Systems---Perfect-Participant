using System;
using System.IO;
using PerfectParticipant.Core;
using UnityEngine;

namespace PerfectParticipant
{
    // CSV: time,state,event,subtype,C,D,I,composure. One row per Command, plus LogEvent() calls from the input layer.
    // event = CommandKind (or PlayerEventType); subtype = line id / tier / value / event name / state / "Test:Response:Subtype".
    public class SessionLogger : MonoBehaviour
    {
        [SerializeField] ExperimentRunner runner;
        StreamWriter w;
        float t0;

        void OnEnable()
        {
            if (!runner) runner = FindFirstObjectByType<ExperimentRunner>();
            if (!runner) return;
            runner.CommandIssued += On;
            try
            {
                var path = Path.Combine(Application.persistentDataPath, $"session_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                w = new StreamWriter(path) { AutoFlush = true };
                w.WriteLine("time,state,event,subtype,C,D,I,composure");
                t0 = Time.time;
                Debug.Log("SessionLogger: " + path);
            }
            catch (Exception e) { Debug.LogWarning("SessionLogger off: " + e.Message); w = null; }
        }

        void OnDisable()
        {
            if (runner) runner.CommandIssued -= On;
            w?.Dispose();
            w = null;
        }

        static string Esc(string s) => s != null && s.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

        void On(Command c)
        {
            string sub;
            switch (c.Kind)
            {
                case CommandKind.PlayLine: sub = c.Id + (c.Slipped ? "/slipped" : ""); break;
                case CommandKind.SetTier: sub = c.Tier.ToString(); break;
                case CommandKind.SetComposure: sub = c.Value.ToString(); break;
                case CommandKind.Transition: sub = c.State.ToString(); break;
                case CommandKind.Classified: sub = c.Test + ":" + c.Response + ":" + c.Subtype; break;
                case CommandKind.ShowCard: sub = c.Id; break;
                default: sub = c.Id; break; // RoomEvent id, StopDialogue null
            }
            Row(c.Kind.ToString(), sub);
        }

        // Optional hook for package A: logger.LogEvent(new PlayerEvent(Time.time, type, name)).
        public void LogEvent(PlayerEvent e) => Row(e.Type.ToString(), e.Payload);

        void Row(string ev, string sub)
        {
            if (w == null || !runner) return;
            var f = runner.Flow;
            var m = f.Model;
            w.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F2},{1},{2},{3},{4},{5},{6},{7}", Time.time - t0, f.State, ev, Esc(sub), m.C, m.D, m.I, m.Composure));
        }
    }
}
