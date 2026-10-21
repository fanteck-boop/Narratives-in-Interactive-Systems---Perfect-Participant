using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace PerfectParticipant
{
    // Desk fallback. Put on XR Origin root. Off when headset or XR simulator present.
    // WASD move, mouse look, E grab (what the crosshair points at) / release, Space throw, F poke button, Q hand behind head (hide test).
    public class DesktopRig : MonoBehaviour
    {
        [SerializeField] float speed = 1.5f, look = 0.1f, reach = 1.5f, toss = 6f;
        const float RayReach = 2.2f; // standing eye 1.6 m: reaches the floor about 1.5 m ahead

        public Transform Hand { get; private set; }
        Transform cam;
        CharacterController body;
        GrabTracker held;
        Quaternion heldRot; // held rotation relative to the camera
        float heldR;
        int heldLayer;
        ExperimentRunner runner;
        float pitch;
        static readonly Vector3 Ahead = new Vector3(0.25f, -0.25f, 0.5f), Behind = new Vector3(0.2f, -0.1f, -0.4f);
        const int IgnoreRaycast = 2;

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
            runner = FindFirstObjectByType<ExperimentRunner>();
            // walls, table, chair and the closed door block the player; stepOffset 0 so it never climbs onto furniture
            if (!TryGetComponent(out body)) body = gameObject.AddComponent<CharacterController>();
            body.height = 1.7f; body.radius = 0.25f; body.center = new Vector3(0f, 0.85f, 0f); body.stepOffset = 0f;
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

            if (runner && !runner.enabled) return; // content note showing: S toggles seated mode, no walking
            var dir = new Vector3((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), 0f, (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
            body.Move(transform.TransformDirection(dir.normalized) * (speed * Time.deltaTime));

            Hand.localPosition = k.qKey.isPressed ? Behind : Ahead;
            if (held) Carry();

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
            if (!Physics.Raycast(cam.position, cam.forward, out var hit, RayReach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
            // the button sits on the console: without this, E on it found the console's tracker and logged "moved the button"
            var press = hit.collider.GetComponentInParent<ButtonPress>();
            if (press) { press.Press(); return; }
            var t = hit.collider.GetComponentInParent<GrabTracker>();
            if (!t || t.Held) return;
            if (!t.Carryable) { t.Grab(Hand); t.Release(0f); return; } // console, chair, locked door: attempt only, as in XR
            held = t;
            if (held.Body) held.Body.isKinematic = true;
            heldRot = Quaternion.Inverse(cam.rotation) * held.transform.rotation;
            heldR = 0f;
            foreach (var c in held.GetComponentsInChildren<Collider>()) if (!c.isTrigger) heldR = Mathf.Max(heldR, c.bounds.extents.magnitude);
            IgnoreBody(held, true); // else the player walks into what they carry
            heldLayer = held.gameObject.layer;
            SetLayer(held.transform, IgnoreRaycast); // the carry cast must not hit the held object itself
            held.Grab(Hand);
            Carry();
        }

        // Follow the hand, but stop short of walls and furniture so nothing is pushed through them.
        void Carry()
        {
            Vector3 from = cam.position, to = Hand.position, d = to - from;
            float len = d.magnitude;
            if (len > 1e-4f && Physics.SphereCast(from, heldR * 0.5f, d / len, out var hit, len + heldR, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                to = from + d / len * Mathf.Max(0f, hit.distance - heldR * 0.5f);
            held.transform.SetPositionAndRotation(to, cam.rotation * heldRot);
        }

        void Drop(bool throwIt)
        {
            GrabTracker t = held; held = null;
            SetLayer(t.transform, heldLayer);
            IgnoreBody(t, false);
            Vector3 v = throwIt ? cam.forward * toss : t.Velocity;
            if (t.Body) { t.Body.isKinematic = false; t.Body.linearVelocity = v; } // a placed object falls and rests, never floats
            t.Release(v.magnitude);
        }

        void IgnoreBody(GrabTracker t, bool on)
        {
            foreach (var c in t.GetComponentsInChildren<Collider>()) if (!c.isTrigger) Physics.IgnoreCollision(body, c, on);
        }

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform c in t) SetLayer(c, layer);
        }

        void OnGUI()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;
            var c = new Rect(Screen.width * 0.5f - 3f, Screen.height * 0.5f - 3f, 6f, 6f);
            GUI.DrawTexture(c, Texture2D.whiteTexture); // crosshair: E grabs what it points at
        }
    }
}
