using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace PerfectParticipant
{
    // Desk fallback. Put on XR Origin root. Off when headset or XR simulator present.
    // WASD move, mouse look, E grab/release, Space throw, F poke button, Q hand behind head (hide test).
    public class DesktopRig : MonoBehaviour
    {
        [SerializeField] float speed = 1.5f, look = 0.1f, reach = 1.5f, toss = 6f;
        const float Room = 1.9f; // ponytail: clamp to 4 m room, no collision

        public Transform Hand { get; private set; }
        Transform cam, oldParent;
        GrabTracker held;
        bool wasKinematic;
        float pitch;
        static readonly Vector3 Ahead = new Vector3(0.25f, -0.25f, 0.5f), Behind = new Vector3(0.2f, -0.1f, -0.4f);

        void Awake()
        {
            // Hand must exist before InputSampler first Sample()
            enabled = !XRSettings.isDeviceActive && !GameObject.Find("XR Interaction Simulator") && !GameObject.Find("XR Device Simulator") && Camera.main != null;
            if (!enabled) return;
            cam = Camera.main.transform;
            foreach (var d in cam.GetComponents<TrackedPoseDriver>()) d.enabled = false;
            cam.position = transform.position + Vector3.up * 1.6f;
            Hand = new GameObject("DesktopHand").transform;
            Hand.SetParent(cam, false);
            Hand.localPosition = Ahead;
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            Keyboard k = Keyboard.current; Mouse m = Mouse.current;
            if (k == null || m == null) return;

            if (k.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
            else if (m.leftButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 d = m.delta.ReadValue() * look;
                transform.Rotate(0f, d.x, 0f);
                pitch = Mathf.Clamp(pitch - d.y, -80f, 80f);
                cam.localEulerAngles = new Vector3(pitch, 0f, 0f);
            }

            var dir = new Vector3((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), 0f, (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
            Vector3 p = transform.position + transform.TransformDirection(dir.normalized) * (speed * Time.deltaTime);
            transform.position = new Vector3(Mathf.Clamp(p.x, -Room, Room), p.y, Mathf.Clamp(p.z, -Room, Room));

            Hand.localPosition = k.qKey.isPressed ? Behind : Ahead;

            if (k.eKey.wasPressedThisFrame) { if (held) Drop(false); else TryGrab(); }
            if (k.spaceKey.wasPressedThisFrame && held) Drop(true);
            if (k.fKey.wasPressedThisFrame)
            {
                var b = FindFirstObjectByType<ButtonPress>();
                if (b && Vector3.Distance(b.transform.position, cam.position) < reach) b.Press();
            }
        }

        void TryGrab()
        {
            GrabTracker best = null; float bd = reach;
            foreach (var t in GrabTracker.All)
            {
                float d = Vector3.Distance(t.transform.position, cam.position);
                if (!t.Held && d < bd) { bd = d; best = t; }
            }
            if (!best) return;
            if (best.kind == GrabKind.Door) { best.Grab(Hand); best.Release(0f); return; } // door locked, attempt only
            held = best;
            oldParent = held.transform.parent;
            if (held.Body) { wasKinematic = held.Body.isKinematic; held.Body.isKinematic = true; }
            held.transform.SetParent(Hand, true);
            held.transform.position = Hand.position;
            held.Grab(Hand);
        }

        void Drop(bool throwIt)
        {
            GrabTracker t = held; held = null;
            Vector3 v = throwIt ? cam.forward * toss : t.Velocity;
            t.transform.SetParent(oldParent, true);
            if (t.Body) { t.Body.isKinematic = wasKinematic; t.Body.linearVelocity = v; }
            t.Release(v.magnitude);
        }
    }
}
