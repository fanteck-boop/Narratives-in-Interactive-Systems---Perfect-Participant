using UnityEngine;

namespace PerfectParticipant
{
    // A step sound every stride of head travel on the floor plane (continuous move is otherwise silent).
    // Cosmetic only; the Hall's reverb zone makes them echo there.
    [RequireComponent(typeof(AudioSource))]
    public class Footsteps : MonoBehaviour
    {
        const float Stride = 0.7f, Jump = 1f, MinSpeed = 0.4f; // more than 1 m in one frame is a reload or recentre, not a step
        AudioSource src;
        Vector3 last;
        float travelled;
        bool has;

        void Awake() { src = GetComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0f; }

        void Update()
        {
            var cam = Camera.main;
            if (!cam) return;
            var p = cam.transform.position; p.y = 0f;
            float d = has ? Vector3.Distance(p, last) : 0f;
            last = p; has = true;
            if (d > Jump) { travelled = 0f; return; }
            if (d < MinSpeed * Time.deltaTime) return; // head sway while standing still is not walking
            travelled += d;
            if (travelled < Stride) return;
            travelled = 0f;
            src.pitch = Random.Range(0.85f, 1.1f);
            src.PlayOneShot(PlaceholderAudio.Step(), 0.35f);
        }
    }
}
