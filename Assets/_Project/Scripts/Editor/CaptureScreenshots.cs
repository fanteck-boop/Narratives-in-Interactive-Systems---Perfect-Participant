using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PerfectParticipant.Editor
{
    // Renders Lab.unity from fixed viewpoints to Docs/screenshots/*.png. Edit-mode scene, nothing saved.
    // Needs graphics: run batchmode WITHOUT -nographics.
    public static class CaptureScreenshots
    {
        const int W = 1600, H = 900;

        struct Shot { public string name; public Vector3 pos, look; public bool observers; public Shot(string n, Vector3 p, Vector3 l, bool o = false) { name = n; pos = p; look = l; observers = o; } }

        static readonly Shot[] Shots =
        {
            new Shot("01_player_view", new Vector3(0f, 1.6f, -1.2f), new Vector3(0f, 1.3f, 2f)),
            new Shot("02_overview", new Vector3(1.7f, 2.4f, -1.8f), new Vector3(-0.2f, 0.7f, 0.4f)),
            new Shot("03_table_cube_tray", new Vector3(0.45f, 1.35f, -0.55f), new Vector3(1.05f, 0.78f, 0f)),
            new Shot("04_console_form", new Vector3(-0.35f, 1.55f, -0.45f), new Vector3(-1.1f, 1.0f, 0.2f)),
            new Shot("05_door_exit", new Vector3(0f, 1.6f, 0.4f), new Vector3(0f, 1.1f, -2f)),
            new Shot("06_observers_in", new Vector3(0f, 1.6f, -0.2f), new Vector3(0f, 1.5f, 2.6f), true),
        };

        [MenuItem("Perfect Participant/Capture Screenshots")]
        static void Menu() => Run();

        // Unity.exe -batchmode -projectPath <p> -executeMethod PerfectParticipant.Editor.CaptureScreenshots.CaptureFromCommandLine -quit -logFile <f>
        public static void CaptureFromCommandLine()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Lab.unity");
            Run();
        }

        static GameObject Find(string n)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == n) return t.gameObject;
            return null;
        }

        static void Run()
        {
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "screenshots");
            Directory.CreateDirectory(dir);

            Find("ConsentForm")?.SetActive(true); // shown from Test 3 on
            var go = new GameObject("ScreenshotCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 80f; // ponytail: roughly Quest vertical FOV, not a stereo eye
            cam.nearClipPlane = 0.02f;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);

            foreach (var s in Shots)
            {
                SetObservers(s.observers);
                go.transform.position = s.pos;
                go.transform.LookAt(s.look);
                cam.Render();
                cam.Render(); // first render after a scene load can come out with blown emissive materials
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                string path = Path.Combine(dir, s.name + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Debug.Log("[Capture] " + path);
            }
            SetObservers(false);
            Find("ConsentForm")?.SetActive(false);
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
        }

        // Test 2 look: silhouettes on, blinds up (mirrors RoomStateController blinds math at t = 1).
        static Vector3 blindsScale, blindsPos;
        static bool blindsSaved;

        static void SetObservers(bool on)
        {
            for (int i = 1; i <= 3; i++) Find("Silhouette_0" + i)?.SetActive(on);
            var b = Find("WindowBlinds")?.transform;
            if (!b) return;
            if (!blindsSaved) { blindsScale = b.localScale; blindsPos = b.localPosition; blindsSaved = true; }
            float sy = blindsScale.y * (on ? 0.08f : 1f);
            b.localScale = new Vector3(blindsScale.x, sy, blindsScale.z);
            b.localPosition = blindsPos + Vector3.up * ((blindsScale.y - sy) * 0.5f);
        }
    }
}
