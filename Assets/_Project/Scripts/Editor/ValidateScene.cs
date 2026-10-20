using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PerfectParticipant.Editor
{
    // Logs missing expected objects (names from ARCHITECTURE.md) in the open scene.
    public static class ValidateScene
    {
        static readonly string[] Expected =
        {
            "Spawn", "XROrigin", "IntakeMark", "DoorHinge", "WindowBlinds", "LED_Tray", "LED_Console", "LED_Door", "LED_Camera", "LED_Clipboard", "RoomLight_Ceiling", "MarkZone", "ObservationWindow", "Silhouette_01", "Silhouette_02", "Silhouette_03",
            "WallTally", "CorkBoard", "Console", "ConsentButton", "ConsentForm", "ButtonZone", "Table", "TableZone", "Cube", "Tray", "TrayZone",
            "WallSouthL", "ExitBooth", "ExitSign", "Door", "DoorHandle", "DoorZone", "CardDisplay", "ClipboardShelf", "Clipboard", "Chair", "SpeakerGrille", "CeilingCamera", "TapeMarks",
            "Systems", "ExperimentRunner", "InputSampler", "DialogueManager", "RoomStateController", "TallyWall", "SessionLogger", "DebugPanel", "SeatedMode",
        };
        static readonly string[] Zones = { "MarkZone", "ButtonZone", "TableZone", "TrayZone", "DoorZone" };

        [MenuItem("Perfect Participant/Validate Scene")]
        static void Menu() => Run();

        // Unity.exe -batchmode -projectPath <p> -executeMethod PerfectParticipant.Editor.ValidateScene.ValidateFromCommandLine -quit -logFile <f>
        public static void ValidateFromCommandLine()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Lab.unity");
            if (!Run()) throw new System.Exception("Lab scene validation failed");
        }

        public static bool Run()
        {
            var all = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToLookup(t => t.name);
            int bad = 0;
            void Fail(string m) { Debug.LogError("[ValidateScene] " + m); bad++; }

            foreach (var n in Expected) if (!all.Contains(n)) Fail("missing object " + n);
            foreach (var n in Zones)
                foreach (var t in all[n])
                    if (!t.TryGetComponent<Collider>(out var c) || !c.isTrigger) Fail(n + " needs a trigger Collider");
            foreach (var t in all["Cube"])
            {
                if (!t.GetComponent<Rigidbody>()) Fail("Cube has no Rigidbody");
                if (!t.GetComponent<XRGrabInteractable>()) Fail("Cube has no XRGrabInteractable");
            }
            // components expected on Systems children (types come from packages A and B)
            foreach (var n in new[] { "ExperimentRunner", "InputSampler", "DialogueManager", "RoomStateController", "TallyWall", "SessionLogger", "DebugPanel", "SeatedMode" })
                foreach (var t in all[n])
                    if (t.GetComponents<MonoBehaviour>().All(m => !m || m.GetType().Name != n)) Fail(n + " object lacks its component");
            // wiring + required components
            var runner = Object.FindFirstObjectByType<ExperimentRunner>();
            if (runner)
            {
                var so = new SerializedObject(runner);
                if (!so.FindProperty("input").objectReferenceValue) Fail("ExperimentRunner.input null");
                if (!so.FindProperty("dialogue").objectReferenceValue) Fail("ExperimentRunner.dialogue null");
            }
            var dm = Object.FindFirstObjectByType<DialogueManager>();
            if (dm && new SerializedObject(dm).FindProperty("lines").arraySize == 0) Fail("DialogueManager.lines empty, run Generate Dialogue Lines");
            if (!Object.FindFirstObjectByType<DesktopRig>()) Fail("DesktopRig missing (belongs on XROrigin)");
            if (!Object.FindFirstObjectByType<CardDisplay>()) Fail("CardDisplay component missing");
            foreach (var n in new[] { "Cube", "Console", "DoorHandle", "Clipboard", "Chair" })
                foreach (var t in all[n]) if (!t.GetComponent<GrabTracker>()) Fail(n + " lacks GrabTracker");
            foreach (var t in all["ConsentButton"]) if (!t.GetComponent<ButtonPress>()) Fail("ConsentButton lacks ButtonPress");
            foreach (var t in all["TableZone"])
                if (!t.GetComponent<Collider>().bounds.Contains(all["Cube"].First().position)) Fail("Cube not inside TableZone at start");
            foreach (var t in all.SelectMany(g => g))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) Fail("missing script on " + t.name);

            Debug.Log(bad == 0 ? "[ValidateScene] OK" : $"[ValidateScene] {bad} problem(s)");
            return bad == 0;
        }
    }
}
