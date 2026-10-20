using System;
using System.Collections.Generic;
using UnityEngine;

namespace PerfectParticipant
{
    // Put on MarkZone, TrayZone, TableZone, ButtonZone, DoorZone (trigger colliders). Tracks occupants.
    [RequireComponent(typeof(Collider))]
    public class Zone : MonoBehaviour
    {
        public event Action<Zone, Collider> Entered, Exited;
        readonly HashSet<Collider> inside = new HashSet<Collider>();
        Collider col;

        void Awake() { col = GetComponent<Collider>(); col.isTrigger = true; }

        // already-overlapping objects send no enter event
        void Start()
        {
            Physics.SyncTransforms();
            Bounds b = col.bounds; // ponytail: AABB seed, slight over-count on rotated zones
            foreach (var c in Physics.OverlapBox(b.center, b.extents)) if (c != col) inside.Add(c);
        }

        void OnTriggerEnter(Collider o) { if (inside.Add(o)) Entered?.Invoke(this, o); }
        void OnTriggerExit(Collider o) { if (inside.Remove(o)) Exited?.Invoke(this, o); }

        // disabled / destroyed colliders send no exit
        void Prune() => inside.RemoveWhere(c => !c || !c.enabled || !c.gameObject.activeInHierarchy);

        public bool Has(Transform t)
        {
            Prune();
            foreach (var c in inside) if (c.transform.IsChildOf(t)) return true;
            return false;
        }

        public bool HasKind(GrabKind k)
        {
            Prune();
            foreach (var c in inside) { var g = c.GetComponentInParent<GrabTracker>(); if (g && g.kind == k) return true; }
            return false;
        }

        // convex colliders only (box, capsule, sphere)
        public bool HasPoint(Vector3 p) => (col.ClosestPoint(p) - p).sqrMagnitude < 1e-6f;
    }
}
