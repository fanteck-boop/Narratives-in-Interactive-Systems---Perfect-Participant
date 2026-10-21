using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PerfectParticipant
{
    public enum GrabKind { Cube, Console, Door, Other, CorridorDoor, Clipboard, Key, RoomButton }

    // On every grabbable (Cube, Console, DoorHandle, clipboard, chair). XRI feeds it; DesktopRig calls Grab/Release directly.
    public class GrabTracker : MonoBehaviour
    {
        public GrabKind kind;
        public int number; // CorridorDoor / RoomButton: room number

        public static GrabTracker Cube, ConsolePanel;
        public static readonly List<GrabTracker> All = new List<GrabTracker>();
        public static event Action<GrabTracker> Grabbed;
        public static event Action<GrabTracker, float> Released; // float = hand speed m/s at release

        public Transform Holder { get; private set; }   // interactor / hand while held
        public bool Held => Holder;
        public Vector3 Velocity { get; private set; }   // smoothed holder velocity
        public Rigidbody Body { get; private set; }
        public bool Carryable => xri is XRGrabInteractable; // false = simple interactable: grab attempt only

        XRBaseInteractable xri;
        Vector3 prev, homePos;
        Quaternion homeRot;
        bool freeOnRelease;

        // enter-play-mode without domain reload keeps statics
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Cube = ConsolePanel = null; All.Clear(); Grabbed = null; Released = null; }

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            transform.GetPositionAndRotation(out homePos, out homeRot);
        }

        void OnEnable()
        {
            All.Add(this);
            if (kind == GrabKind.Cube) Cube = this;
            if (kind == GrabKind.Console) ConsolePanel = this;
            xri = GetComponent<XRBaseInteractable>();
            if (xri) { xri.firstSelectEntered.AddListener(OnSel); xri.lastSelectExited.AddListener(OnDesel); }
        }

        void OnDisable()
        {
            All.Remove(this);
            if (Cube == this) Cube = null;
            if (ConsolePanel == this) ConsolePanel = null;
            if (xri) { xri.firstSelectEntered.RemoveListener(OnSel); xri.lastSelectExited.RemoveListener(OnDesel); }
            Holder = null;
        }

        void OnSel(SelectEnterEventArgs a) => Grab(a.interactorObject.transform);
        void OnDesel(SelectExitEventArgs a) => Release();

        public void Grab(Transform holder)
        {
            Holder = holder; prev = holder.position; Velocity = default;
            Grabbed?.Invoke(this);
        }

        // speed < 0 = use tracked holder speed. Own tracking because XRI applies throw velocity after the exit event.
        public void Release(float speed = -1f)
        {
            if (!Held) return;
            if (speed < 0f) speed = Velocity.magnitude;
            Holder = null; Velocity = default;
            freeOnRelease = Carryable && Body; // XRI restores the start-kinematic flag on detach: the clipboard would float
            Released?.Invoke(this, speed);
        }

        public void SetHome() => transform.GetPositionAndRotation(out homePos, out homeRot); // SeatedMode moved the anchors

        public void Respawn()
        {
            if (Body) { Body.linearVelocity = default; Body.angularVelocity = default; }
            transform.SetPositionAndRotation(homePos, homeRot);
        }

        void Update()
        {
            if (!Held)
            {
                if (freeOnRelease) { Body.isKinematic = false; freeOnRelease = false; } // one frame later, after XRI's detach
                return;
            }
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 p = Holder.position;
            Velocity = Vector3.Lerp(Velocity, (p - prev) / dt, 1f - Mathf.Exp(-25f * dt));
            prev = p;
        }
    }
}
