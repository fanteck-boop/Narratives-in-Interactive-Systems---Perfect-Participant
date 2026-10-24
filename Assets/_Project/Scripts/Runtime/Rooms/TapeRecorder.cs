using UnityEngine;

namespace PerfectParticipant
{
    // Hall, voice desk: 0001's own first session on tape. Picking it up plays it once (restarts only when done).
    // Diegetic sound, not narration: it never goes through Core or the dialogue queue.
    [RequireComponent(typeof(AudioSource))]
    public class TapeRecorder : MonoBehaviour
    {
        AudioSource src;
        GrabTracker me;
        DialogueManager dm;
        bool captioned;

        void Awake() { src = GetComponent<AudioSource>(); me = GetComponent<GrabTracker>(); dm = FindFirstObjectByType<DialogueManager>(); }
        void OnEnable() => GrabTracker.Grabbed += On;
        void OnDisable() => GrabTracker.Grabbed -= On;

        void On(GrabTracker t)
        {
            if (t != me || src.isPlaying) return;
            src.Play();
            if (dm) { dm.Caption("<i>[tape] " + Core.Lines.Text["H-Tape"] + "</i>"); captioned = true; }
        }

        void Update() { if (captioned && !src.isPlaying) { captioned = false; dm.ClearCaption(); } }
    }
}
