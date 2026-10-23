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

        void Awake() { src = GetComponent<AudioSource>(); me = GetComponent<GrabTracker>(); }
        void OnEnable() => GrabTracker.Grabbed += On;
        void OnDisable() => GrabTracker.Grabbed -= On;
        void On(GrabTracker t) { if (t == me && !src.isPlaying) src.Play(); }
    }
}
