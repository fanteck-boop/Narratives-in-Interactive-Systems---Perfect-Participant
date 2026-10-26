using UnityEngine;

namespace PerfectParticipant
{
    // Room 6 easter egg: a monitor showing the lab through its ceiling camera (point-of-view technologies, brief 17.3).
    // Low-res, 5 fps, and only while the player is close, so it costs nothing on Quest the rest of the time.
    public class CctvFeed : MonoBehaviour
    {
        [SerializeField] string source = "CeilingCamera";
        [SerializeField] float watchRange = 4f;
        Camera cam;
        float next;

        void Start()
        {
            var src = RoomUtil.Named(source);
            if (!src) { enabled = false; return; }
            var rt = new RenderTexture(256, 144, 16);
            cam = new GameObject("CctvCam").AddComponent<Camera>();
            cam.transform.SetParent(src, false);
            cam.transform.localPosition = new Vector3(0f, 0f, 0.12f); // just in front of the lens
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;                                // default 0.3 cut through a nearby wall and showed the next room
            cam.stereoTargetEye = StereoTargetEyeMask.None;          // never draws into the headset
            cam.targetTexture = rt;
            cam.enabled = false;                                      // rendered by hand below
            var m = GetComponent<Renderer>().material;
            m.SetTexture("_BaseMap", rt);
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", rt);
            m.SetColor("_EmissionColor", Color.white * 0.9f);         // a screen glows, it is not lit by the room
        }

        void Update()
        {
            var main = Camera.main;
            if (!main || Time.time < next || Vector3.Distance(main.transform.position, transform.position) > watchRange) return;
            next = Time.time + 0.2f;
            cam.Render();
        }
    }
}
