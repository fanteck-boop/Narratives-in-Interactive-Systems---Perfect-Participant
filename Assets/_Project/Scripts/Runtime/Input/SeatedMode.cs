using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace PerfectParticipant
{
    // Seated option. Moves mark, table (+cube, tray), console, chair to a front arc around the seat (Spawn).
    // Snap turn 45 only when seated. Window stays: it is straight ahead of the seat already. Classification untouched.
    // Toggle before the session starts (content note S / A / X, DebugPanel). ponytail: fixed anchors, tune on device.
    public class SeatedMode : MonoBehaviour
    {
        public static bool Seated { get; private set; }

        [SerializeField] bool startSeated;

        static readonly string[] Names = { "IntakeMark", "Table", "Console", "Chair" };
        static readonly Vector3[] SeatPos = { new Vector3(0f, 0f, -1.05f), new Vector3(0.6f, 0f, -0.9f), new Vector3(-0.6f, 0f, -0.9f), new Vector3(0f, 0f, -1.3f) };
        Transform[] t;
        Pose[] stand;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Seated = false;

        void Start()
        {
            t = new Transform[Names.Length];
            stand = new Pose[Names.Length];
            for (int i = 0; i < Names.Length; i++)
            {
                t[i] = RoomUtil.Named(Names[i]);
                if (t[i]) stand[i] = new Pose(t[i].position, t[i].rotation);
            }
            Set(startSeated);
        }

        public void Toggle() => Set(!Seated);

        public void Set(bool on)
        {
            Seated = on;
            if (t == null) return;
            var table = t[1];
            Matrix4x4 old = table ? table.localToWorldMatrix : default;
            for (int i = 0; i < t.Length; i++)
                if (t[i]) t[i].SetPositionAndRotation(on ? SeatPos[i] : stand[i].position, on ? Quaternion.identity : stand[i].rotation);
            if (table) // cube and tray are roots: carry them by the table's rigid change
            {
                Matrix4x4 m = table.localToWorldMatrix * old.inverse;
                foreach (var n in new[] { "Cube", "Tray" })
                {
                    var o = RoomUtil.Named(n);
                    if (!o) continue;
                    o.SetPositionAndRotation(m.MultiplyPoint3x4(o.position), m.rotation * o.rotation);
                    if (o.TryGetComponent<Rigidbody>(out var rb) && !rb.isKinematic) { rb.linearVelocity = default; rb.angularVelocity = default; }
                }
            }
            Physics.SyncTransforms();
            foreach (var g in GrabTracker.All) g.SetHome();
            FindFirstObjectByType<RoomStateController>()?.Rehome();
            foreach (var s in FindObjectsByType<SnapTurnProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { s.turnAmount = 45f; s.enabled = on; }
        }
    }
}
