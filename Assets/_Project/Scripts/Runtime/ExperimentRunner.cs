using System;
using PerfectParticipant.Core;
using UnityEngine;

namespace PerfectParticipant
{
    // Glue: sample input, tick Core flow, broadcast commands. Output components subscribe to CommandIssued.
    public class ExperimentRunner : MonoBehaviour
    {
        [SerializeField] InputSampler input;
        [SerializeField] DialogueManager dialogue;

        public ExperimentFlow Flow { get; private set; } = new ExperimentFlow();
        public event Action<Command> CommandIssued;

        void Update()
        {
            FrameInput i = input ? input.Sample() : default;
            i.NarratorBusy = dialogue && dialogue.Busy;
            Dispatch(Flow.Tick(Time.deltaTime, i));
        }

        // Debug panel entry points.
        public void Force(ResponseKind k, Subtype s = Subtype.None) => Dispatch(Flow.Force(k, s));
        public void JumpTo(GameState s) => Dispatch(Flow.JumpTo(s));
        public void SetComposure(int v) => Dispatch(Flow.SetComposure(v));
        // Reload resets room, dialogue, logger and props too. Lab.unity is in the build list.
        public void Restart() => UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);

        void Dispatch(System.Collections.Generic.List<Command> cmds)
        {
            foreach (var c in cmds) CommandIssued?.Invoke(c);
        }
    }
}
