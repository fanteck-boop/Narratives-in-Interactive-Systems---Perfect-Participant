using TMPro;
using UnityEngine;

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
    }
}
