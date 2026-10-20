using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace PerfectParticipant
{
    internal static class RoomUtil
    {
        // Scene lookup by name incl. inactive. Linear scan, call from Awake only.
        public static Transform Named(string n)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == n) return t;
            return null;
        }

        // World-space TMP sized in metres. Font size is fixed, GameObject scale is calibrated from measured line height,
        // so it does not depend on TMP's internal unit factor.
        public static TextMeshPro Text(string name, Transform parent, Vector3 localPos, Quaternion localRot, float lineMeters, float widthMeters)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = 10f;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            float s = lineMeters / Mathf.Max(0.01f, t.GetPreferredValues("Ag").y);
            go.transform.localScale = Vector3.one * s;
            t.rectTransform.sizeDelta = new Vector2(widthMeters / s, lineMeters * 6f / s);
            return t;
        }

        // Screen UI layer. Desktop: Screen Space Overlay. Headset: overlay never reaches the eyes, so Screen Space Camera
        // at dist metres (text needs ~1 m for comfortable stereo). Higher order draws on top. Returns a child rect to fill.
        public static RectTransform ScreenLayer(string name, Camera cam, float dist, int order)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
            var cv = go.GetComponent<Canvas>();
            cv.renderMode = XRSettings.isDeviceActive ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            cv.worldCamera = cam;
            cv.planeDistance = Mathf.Max(dist, cam.nearClipPlane * 2f);
            cv.sortingOrder = order;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920f, 1080f);
            s.matchWidthOrHeight = 0.5f;
            var r = new GameObject(name.Replace("Canvas", ""), typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(go.transform, false);
            return r;
        }

        // Anchor a rect to a fraction of the screen (0..1), no offsets.
        public static void Fill(RectTransform r, float xMin, float yMin, float xMax, float yMax)
        {
            r.anchorMin = new Vector2(xMin, yMin);
            r.anchorMax = new Vector2(xMax, yMax);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        public static TextMeshProUGUI ScreenText(RectTransform r, float size)
        {
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.alignment = TextAlignmentOptions.Center;
            t.fontSize = size;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }
    }
}
