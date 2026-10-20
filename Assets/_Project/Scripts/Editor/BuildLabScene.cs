using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PerfectParticipant.Editor
{
    // Builds Scenes/Lab.unity from primitives. Re-runnable: overwrites scene and materials.
    // Metres, origin = mark on floor, +Z north (see ARCHITECTURE.md). Quad/TMP face -Z, so yaw = wall facing the viewer.
    public static class BuildLabScene
    {
        const string ScenePath = "Assets/_Project/Scenes/Lab.unity";
        const string MatDir = "Assets/_Project/Materials";
        const string RigCopy = "Assets/_Project/Prefabs/XROrigin.prefab";
        const float YawN = 0, YawW = -90, YawS = 180, YawE = 90; // viewer-facing yaw per wall
        static readonly Dictionary<string, Material> mats = new();
        static readonly Color Ink = new Color(0.08f, 0.08f, 0.08f);

        [MenuItem("Perfect Participant/Build Lab Scene")]
        static void Menu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build();
        }

        // Unity.exe -batchmode -projectPath <p> -executeMethod PerfectParticipant.Editor.BuildLabScene.BuildFromCommandLine -quit -logFile <f>
        public static void BuildFromCommandLine()
        {
            Build();
            ValidateScene.Run(); // log only: package A/B types may not exist yet
        }

        public static void Build()
        {
            mats.Clear();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.19f, 0.18f);

            Shell(); Window(); Tally(); Cork(); ConsoleObj(); TableObj(); DoorObj(); Shelf(); ChairObj();
            CeilingProps(); Tape(); Lights(); Rig(); Systems();
            AutoWire();
            var tso = new SerializedObject(Object.FindFirstObjectByType<TallyWall>()); // chart already has row labels
            tso.FindProperty("makeLabels").boolValue = false;
            tso.ApplyModifiedPropertiesWithoutUndo();
            DialogueLineTools.Generate(); // assets + fills DialogueManager.lines

            EditorSceneManager.SaveScene(scene, ScenePath);
            // ponytail: build list = Lab only (package D wants this), re-add template scenes by hand if needed
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[BuildLab] saved " + ScenePath);
        }

        // ---------- helpers ----------
        static Material M(string n, Color c, float smooth = 0.15f, Color? emis = null, bool glass = false)
        {
            string key = "M_" + n;
            if (mats.TryGetValue(key, out var m)) return m;
            string p = $"{MatDir}/{key}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0f);
            if (emis.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emis.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); }
            if (glass)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetShaderPassEnabled("ShadowCaster", false);
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(m);
            return mats[key] = m;
        }

        static Color G(float v) => new Color(v, v, v);

        static GameObject E(string n, Transform parent, Vector3 pos, Vector3 rot = default)
        {
            var g = new GameObject(n);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localEulerAngles = rot;
            return g;
        }

        // primitive; collider kept only when col
        static GameObject P(PrimitiveType t, string n, Transform parent, Vector3 pos, Vector3 scale, Material m, bool col = false, Vector3 rot = default)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = n;
            var tr = g.transform;
            tr.SetParent(parent, false);
            tr.localPosition = pos; tr.localEulerAngles = rot; tr.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            if (!col) Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }

        static GameObject Cb(string n, Transform p, Vector3 pos, Vector3 s, Material m, bool col = false, Vector3 rot = default) => P(PrimitiveType.Cube, n, p, pos, s, m, col, rot);
        static GameObject Qd(string n, Transform p, Vector3 pos, Vector2 s, Material m, Vector3 rot = default) => P(PrimitiveType.Quad, n, p, pos, new Vector3(s.x, s.y, 1), m, false, rot);

        static GameObject Zone(string n, Transform parent, Vector3 pos, Vector3 size)
        {
            var g = E(n, parent, pos);
            var b = g.AddComponent<BoxCollider>();
            b.isTrigger = true; b.size = size;
            return g;
        }

        // emissive indicator, driven by RoomStateController (looks up renderers named LED_*)
        static GameObject Led(string n, Transform parent, Vector3 pos, Vector3 size) => Cb(n, parent, pos, size, M("Led", G(0.12f), 0.5f, Color.black));

        // world-space TMP. em = glyph em height in metres (TMP world scale is fontSize * 0.1).
        static TextMeshPro T(string n, Transform parent, Vector3 pos, Vector3 rot, string s, float em, Vector2 box, Color c,
            TextAlignmentOptions a = TextAlignmentOptions.Center)
        {
            var t = E(n, parent, pos, rot).AddComponent<TextMeshPro>();
            t.text = s; t.fontSize = em * 10f; t.color = c; t.alignment = a;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.rectTransform.sizeDelta = box;
            return t;
        }

        static void Static(GameObject g) => g.isStatic = true;

        // ---------- scene parts ----------
        static void Shell()
        {
            var room = E("Room", null, Vector3.zero).transform;
            var wall = M("Wall", G(0.62f)); var floor = M("Floor", G(0.26f), 0.3f); var ceil = M("Ceiling", G(0.72f));
            void W(string n, Vector3 p, Vector3 s, Material m) => Static(Cb(n, room, p, s, m, true));
            W("Floor", new Vector3(0, -0.05f, 0), new Vector3(4.2f, 0.1f, 4.2f), floor);
            W("Ceiling", new Vector3(0, 2.65f, 0), new Vector3(4.2f, 0.1f, 4.2f), ceil);
            // south wall with doorway x +-0.45, y 0..2.1 (Door leaf closes it, ExitBooth behind)
            W("WallSouthL", new Vector3(-1.275f, 1.3f, -2.05f), new Vector3(1.65f, 2.6f, 0.1f), wall);
            W("WallSouthR", new Vector3(1.275f, 1.3f, -2.05f), new Vector3(1.65f, 2.6f, 0.1f), wall);
            W("WallSouthHigh", new Vector3(0, 2.35f, -2.05f), new Vector3(0.9f, 0.5f, 0.1f), wall);
            W("WallWest", new Vector3(-2.05f, 1.3f, 0), new Vector3(0.1f, 2.6f, 4f), wall);
            W("WallEast", new Vector3(2.05f, 1.3f, 0), new Vector3(0.1f, 2.6f, 4f), wall);
            // north wall with window opening x +-0.75, y 0.9..2.1
            W("WallNorthL", new Vector3(-1.425f, 1.3f, 2.05f), new Vector3(1.35f, 2.6f, 0.1f), wall);
            W("WallNorthR", new Vector3(1.425f, 1.3f, 2.05f), new Vector3(1.35f, 2.6f, 0.1f), wall);
            W("WallNorthLow", new Vector3(0, 0.45f, 2.05f), new Vector3(1.5f, 0.9f, 0.1f), wall);
            W("WallNorthHigh", new Vector3(0, 2.35f, 2.05f), new Vector3(1.5f, 0.5f, 0.1f), wall);

            // observer booth behind the window + silhouettes (inactive until used)
            var booth = E("ObservationBooth", null, Vector3.zero).transform;
            var dark = M("Dark", G(0.05f), 0.05f);
            Cb("BoothBack", booth, new Vector3(0, 1.3f, 3.35f), new Vector3(4.2f, 2.6f, 0.1f), dark);
            Cb("BoothFloor", booth, new Vector3(0, -0.05f, 2.7f), new Vector3(4.2f, 0.1f, 1.2f), dark);
            Cb("BoothCeiling", booth, new Vector3(0, 2.65f, 2.7f), new Vector3(4.2f, 0.1f, 1.2f), dark);
            Cb("BoothWest", booth, new Vector3(-2.05f, 1.3f, 2.7f), new Vector3(0.1f, 2.6f, 1.2f), dark);
            Cb("BoothEast", booth, new Vector3(2.05f, 1.3f, 2.7f), new Vector3(0.1f, 2.6f, 1.2f), dark);
            var sil = M("Silhouette", new Color(0.02f, 0.02f, 0.025f), 0f);
            float[] sx = { -0.55f, 0f, 0.55f }, sh = { 0.85f, 0.8f, 0.9f };
            for (int i = 0; i < 3; i++)
                P(PrimitiveType.Capsule, $"Silhouette_0{i + 1}", booth, new Vector3(sx[i], sh[i], 2.6f), new Vector3(0.5f, sh[i], 0.35f), sil).SetActive(false);
        }

        static void Window()
        {
            var w = E("ObservationWindow", null, new Vector3(0, 1.5f, 2f)).transform;
            Qd("Glass", w, new Vector3(0, 0, -0.012f), new Vector2(1.5f, 1.2f), M("Glass", new Color(0.03f, 0.05f, 0.07f, 0.88f), 0.9f, null, true));
            var f = M("Trim", G(0.12f));
            Cb("FrameTop", w, new Vector3(0, 0.62f, -0.02f), new Vector3(1.62f, 0.04f, 0.04f), f);
            Cb("FrameBottom", w, new Vector3(0, -0.62f, -0.02f), new Vector3(1.62f, 0.04f, 0.04f), f);
            Cb("FrameLeft", w, new Vector3(-0.77f, 0, -0.02f), new Vector3(0.04f, 1.2f, 0.04f), f);
            Cb("FrameRight", w, new Vector3(0.77f, 0, -0.02f), new Vector3(0.04f, 1.2f, 0.04f), f);
            Cb("Sill", w, new Vector3(0, -0.64f, -0.07f), new Vector3(1.7f, 0.03f, 0.12f), f);
            Cb("WindowBlinds", w, new Vector3(0, 0, -0.045f), new Vector3(1.5f, 1.2f, 0.01f), M("Blind", G(0.45f))); // RoomStateController raises it
            // dents at cube height, previous participants threw it
            var d = M("Dent", G(0.38f));
            for (int i = 0; i < 3; i++) Qd($"Dent_{i + 1}", null, new Vector3(-0.4f + 0.35f * i, 0.8f + 0.02f * i, 1.995f), new Vector2(0.07f, 0.07f), d, new Vector3(0, 0, 20 * i));
        }

        static void Tally()
        {
            var t = E("WallTally", null, new Vector3(-1.2f, 1.5f, 1.98f)).transform;
            Qd("Panel", t, Vector3.zero, new Vector2(0.7f, 0.9f), M("Paper", G(0.88f), 0.05f));
            T("TallyTitle", t, new Vector3(0, 0.38f, -0.004f), Vector3.zero, "OBSERVATION RECORD", 0.035f, new Vector2(0.66f, 0.08f), Ink);
            string[] rows = { "Cooperation", "Initiative", "Observation" };
            for (int i = 0; i < 3; i++)
            {
                float y = 0.2f - 0.2f * i;
                var r = E("Row_" + rows[i], t, new Vector3(0, y, -0.004f)).transform;
                T("Label", r, new Vector3(-0.15f, 0, 0), Vector3.zero, rows[i], 0.04f, new Vector2(0.36f, 0.08f), Ink, TextAlignmentOptions.Left);
                Qd("Rule", r, new Vector3(0, -0.07f, 0), new Vector2(0.64f, 0.004f), M("Rule", G(0.3f)));
            }
        }

        static void Cork()
        {
            var c = E("CorkBoard", null, new Vector3(1.2f, 1.5f, 1.98f)).transform;
            Qd("Frame", c, new Vector3(0, 0, 0.001f), new Vector2(0.96f, 0.76f), M("Trim", G(0.12f)));
            Qd("Cork", c, Vector3.zero, new Vector2(0.9f, 0.7f), M("Cork", new Color(0.6f, 0.45f, 0.28f), 0.05f));
            var rnd = new System.Random(7);
            var paper = M("Tag", G(0.9f), 0.05f); var blank = M("TagBlank", new Color(0.95f, 0.85f, 0.6f), 0.05f);
            for (int i = 0; i < 20; i++)
            {
                bool me = i == 19;
                var p = new Vector3(-0.32f + 0.16f * (i % 5) + (float)(rnd.NextDouble() - 0.5) * 0.03f,
                                    0.24f - 0.15f * (i / 5) + (float)(rnd.NextDouble() - 0.5) * 0.03f, -0.002f);
                var tag = E(me ? "Tag_0417" : $"Tag_{i + 1:00}", c, p, new Vector3(0, 0, (float)(rnd.NextDouble() - 0.5) * 16f)).transform;
                Qd("Paper", tag, Vector3.zero, new Vector2(0.08f, 0.05f), me ? blank : paper);
                string txt = me ? "0417" : $"{397 + i:0000}\n{(i % 6 == 4 ? "B" : "A")}";
                T("Label", tag, new Vector3(0, 0, -0.001f), Vector3.zero, txt, 0.011f, new Vector2(0.08f, 0.05f), Ink);
            }
        }

        static void ConsoleObj()
        {
            var c = E("Console", null, new Vector3(-1.1f, 0, 0.2f)).transform;
            Cb("Body", c, new Vector3(0, 0.5f, 0), new Vector3(0.6f, 1f, 0.5f), M("Console", G(0.3f), 0.25f), true);
            var scr = M("Screen", new Color(0.02f, 0.05f, 0.04f), 0.5f, new Color(0.03f, 0.1f, 0.07f));
            Qd("ConsoleScreen", c, new Vector3(0.301f, 0.72f, 0), new Vector2(0.4f, 0.16f), scr, new Vector3(0, YawW, 0));
            Led("LED_Console", c, new Vector3(0.302f, 0.52f, 0f), new Vector3(0.01f, 0.03f, 0.03f));

            var btn = E("ConsentButton", c, new Vector3(0.15f, 1.0f, 0)).transform;
            P(PrimitiveType.Cylinder, "Cap", btn, new Vector3(0, 0.015f, 0), new Vector3(0.18f, 0.015f, 0.18f), M("ConsentOrange", new Color(1f, 0.42f, 0.05f), 0.55f, Color.black), true);
            T("Label", btn, new Vector3(0, 0.032f, 0), new Vector3(90, -90, 0), "I CONSENT", 0.018f, new Vector2(0.17f, 0.04f), Ink);
            Zone("ButtonZone", c, new Vector3(0.15f, 1.06f, 0), new Vector3(0.2f, 0.1f, 0.2f));
            // press = poke only (ButtonPress), physics never presses
            var bi = btn.gameObject.AddComponent<XRSimpleInteractable>();
            var pf = btn.gameObject.AddComponent<XRPokeFilter>();
            pf.pokeCollider = btn.GetComponentInChildren<Collider>();
            pf.pokeConfiguration.Value.pokeDirection = PokeAxis.NegativeY; // XRI: NegativeY = axis normal +up, poker approaches from above and pushes down onto the cap
            bi.colliders.Add(pf.pokeCollider); // explicit, so no other child collider joins the button
            btn.gameObject.AddComponent<ButtonPress>();
            // Console grab uses only its body collider, else a button poke would also select the console
            var ci = c.gameObject.AddComponent<XRSimpleInteractable>();
            ci.colliders.Add(c.Find("Body").GetComponent<Collider>());
            c.gameObject.AddComponent<GrabTracker>().kind = GrabKind.Console;

            var form = E("ConsentForm", c, new Vector3(-0.1f, 1.002f, 0), new Vector3(0, YawW, 0)).transform;
            // flat on top, rotated so text reads from the east (player side)
            Qd("Paper", form, Vector3.zero, new Vector2(0.21f, 0.297f), M("Paper", G(0.88f), 0.05f), new Vector3(90, 0, 0));
            T("Text", form, new Vector3(0, 0.001f, 0), new Vector3(90, 0, 0), "<size=140%><b>PARTICIPANT CONSENT</b></size>\n\nI agree to follow the procedure as explained by the facility. I understand the session may be recorded.\n\n<size=60%>Study 3B: Obedience to Procedure</size>", 0.011f, new Vector2(0.19f, 0.27f), Ink, TextAlignmentOptions.TopLeft);
        }

        static void TableObj()
        {
            var t = E("Table", null, new Vector3(1.1f, 0, 0)).transform;
            var wood = M("Table", new Color(0.45f, 0.36f, 0.27f), 0.2f);
            Cb("Top", t, new Vector3(0, 0.725f, 0), new Vector3(0.7f, 0.05f, 1.2f), wood, true);
            foreach (var sx in new[] { -0.3f, 0.3f }) foreach (var sz in new[] { -0.55f, 0.55f })
                Cb("Leg", t, new Vector3(sx, 0.35f, sz), new Vector3(0.05f, 0.7f, 0.05f), wood, true);
            Zone("TableZone", t, new Vector3(0, 0.81f, 0), new Vector3(0.7f, 0.12f, 1.2f));

            var cube = P(PrimitiveType.Cube, "Cube", null, new Vector3(1.0f, 0.79f, -0.1f), Vector3.one * 0.08f, M("Cube", new Color(0.93f, 0.93f, 0.9f), 0.1f), true, new Vector3(0, 17, 0));
            var rb = cube.AddComponent<Rigidbody>();
            rb.mass = 0.2f; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            cube.AddComponent<XRGrabInteractable>().movementType = XRBaseInteractable.MovementType.VelocityTracking;
            cube.AddComponent<GrabTracker>().kind = GrabKind.Cube;

            var tray = E("Tray", null, new Vector3(1.15f, 0.76f, 0.15f)).transform;
            var tm = M("Tray", G(0.35f), 0.3f);
            Cb("Base", tray, Vector3.zero, new Vector3(0.24f, 0.02f, 0.3f), tm, true);
            Cb("RimW", tray, new Vector3(-0.115f, 0.02f, 0), new Vector3(0.01f, 0.03f, 0.3f), tm, true);
            Cb("RimE", tray, new Vector3(0.115f, 0.02f, 0), new Vector3(0.01f, 0.03f, 0.3f), tm, true);
            Cb("RimS", tray, new Vector3(0, 0.02f, -0.145f), new Vector3(0.24f, 0.03f, 0.01f), tm, true);
            Cb("RimN", tray, new Vector3(0, 0.02f, 0.145f), new Vector3(0.24f, 0.03f, 0.01f), tm, true);
            T("ReturnLabel", tray, new Vector3(0, 0.0115f, 0), new Vector3(90, 90, 0), "RETURN", 0.03f, new Vector2(0.2f, 0.06f), G(0.9f));
            Led("LED_Tray", tray, new Vector3(0, 0.02f, -0.16f), new Vector3(0.03f, 0.015f, 0.01f));
            Zone("TrayZone", tray, new Vector3(0, 0.05f, 0), new Vector3(0.2f, 0.1f, 0.26f));
        }

        static void DoorObj()
        {
            var d = E("Door", null, new Vector3(0, 0, -2f)).transform; // swings about DoorHinge (RoomStateController rotates this root)
            var leaf = M("Door", G(0.4f), 0.2f); var trim = M("Trim", G(0.12f));
            Cb("Leaf", d, new Vector3(0, 1.05f, 0.03f), new Vector3(0.9f, 2.1f, 0.06f), leaf, true);
            d.gameObject.AddComponent<Rigidbody>().isKinematic = true; // moved by transform (RoomStateController), physics must not fight it
            var fr = E("DoorFrame", null, new Vector3(0, 0, -2f)).transform;
            Cb("FrameTop", fr, new Vector3(0, 2.13f, 0.03f), new Vector3(1.06f, 0.06f, 0.08f), trim);
            Cb("FrameL", fr, new Vector3(-0.5f, 1.05f, 0.03f), new Vector3(0.06f, 2.1f, 0.08f), trim);
            Cb("FrameR", fr, new Vector3(0.5f, 1.05f, 0.03f), new Vector3(0.06f, 2.1f, 0.08f), trim);
            E("DoorHinge", null, new Vector3(-0.45f, 0, -2f));
            var s = new Vector3(0, YawS, 0);
            Qd("SignPlate", d, new Vector3(0, 1.75f, 0.064f), new Vector2(0.38f, 0.1f), M("Paper", G(0.88f), 0.05f), s);
            T("Sign", d, new Vector3(0, 1.75f, 0.066f), s, "Please do not exit during session.", 0.022f, new Vector2(0.36f, 0.09f), Ink);

            var h = E("DoorHandle", d, new Vector3(0.33f, 1f, 0.06f)).transform;
            var metal = M("Metal", G(0.55f), 0.7f);
            P(PrimitiveType.Cylinder, "Stem", h, new Vector3(0, 0, 0.05f), new Vector3(0.03f, 0.05f, 0.03f), metal, false, new Vector3(90, 0, 0));
            P(PrimitiveType.Cylinder, "Grip", h, new Vector3(0, 0, 0.1f), new Vector3(0.03f, 0.08f, 0.03f), metal, true);
            h.gameObject.AddComponent<XRSimpleInteractable>(); // grab attempt only, handle never moves (locked)
            h.gameObject.AddComponent<GrabTracker>().kind = GrabKind.Door;
            Cb("DoorLock", d, new Vector3(0.33f, 0.88f, 0.075f), new Vector3(0.07f, 0.09f, 0.03f), M("Dark", G(0.05f), 0.05f));
            Led("LED_Door", d, new Vector3(0.33f, 0.92f, 0.092f), new Vector3(0.012f, 0.012f, 0.004f));
            Zone("DoorZone", fr, new Vector3(0, 1.05f, 0.45f), new Vector3(1.2f, 2.1f, 0.9f));

            var card = E("CardDisplay", d, new Vector3(0, 1.4f, 0.07f), s).transform;
            T("CardTitle", card, new Vector3(0, 0.05f, 0), Vector3.zero, "", 0.05f, new Vector2(0.8f, 0.12f), G(0.95f));
            T("CardSubtitle", card, new Vector3(0, -0.05f, 0), Vector3.zero, "", 0.025f, new Vector2(0.8f, 0.08f), G(0.95f));
            card.gameObject.AddComponent<CardDisplay>();

            // dark unlit booth behind the doorway: the unlocked door reveals an exit
            var b = E("ExitBooth", null, Vector3.zero).transform;
            var dark = M("Dark", G(0.05f), 0.05f);
            Cb("ExitFloor", b, new Vector3(0, -0.05f, -2.7f), new Vector3(2.4f, 0.1f, 1.2f), dark);
            Cb("ExitCeiling", b, new Vector3(0, 2.65f, -2.7f), new Vector3(2.4f, 0.1f, 1.2f), dark);
            Cb("ExitBack", b, new Vector3(0, 1.3f, -3.35f), new Vector3(2.4f, 2.6f, 0.1f), dark);
            Cb("ExitWest", b, new Vector3(-1.25f, 1.3f, -2.7f), new Vector3(0.1f, 2.6f, 1.2f), dark);
            Cb("ExitEast", b, new Vector3(1.25f, 1.3f, -2.7f), new Vector3(0.1f, 2.6f, 1.2f), dark);
            var es = E("ExitSign", b, new Vector3(0, 2.1f, -3.29f), new Vector3(0, YawS, 0)).transform; // faces +Z, readable from the room
            Qd("Plate", es, Vector3.zero, new Vector2(0.4f, 0.14f), M("ExitGlow", new Color(0.05f, 0.25f, 0.1f), 0.2f, new Color(0.1f, 0.6f, 0.25f)));
            T("Label", es, new Vector3(0, 0, -0.001f), Vector3.zero, "EXIT", 0.07f, new Vector2(0.38f, 0.12f), G(0.9f));
        }

        static void Shelf()
        {
            var s = E("ClipboardShelf", null, new Vector3(-1.95f, 1.4f, 1f)).transform;
            Cb("Board", s, Vector3.zero, new Vector3(0.2f, 0.03f, 0.6f), M("Table", new Color(0.45f, 0.36f, 0.27f), 0.2f), true);
            // root level so package A can grab it; leans on the west wall
            var c = E("Clipboard", null, new Vector3(-1.92f, 1.575f, 1f), new Vector3(0, 0, 12)).transform;
            Cb("Board", c, Vector3.zero, new Vector3(0.015f, 0.31f, 0.23f), M("Trim", G(0.12f)), true);
            var crb = c.gameObject.AddComponent<Rigidbody>();
            crb.isKinematic = true; // RoomStateController frees it on R-03
            c.gameObject.AddComponent<XRGrabInteractable>();
            c.gameObject.AddComponent<GrabTracker>().kind = GrabKind.Other;
            Led("LED_Clipboard", c, new Vector3(0.01f, 0.15f, 0.09f), new Vector3(0.006f, 0.012f, 0.012f));
            var y = new Vector3(0, YawW, 0);
            Qd("Paper", c, new Vector3(0.0085f, -0.01f, 0), new Vector2(0.2f, 0.27f), M("Paper", G(0.88f), 0.05f), y);
            T("Text", c, new Vector3(0.0095f, -0.01f, 0), y, "Conditions:\nA. Compliance.\nB. Spontaneous Initiative.\nC.", 0.02f, new Vector2(0.19f, 0.26f), Ink, TextAlignmentOptions.TopLeft);
        }

        static void ChairObj()
        {
            var c = E("Chair", null, new Vector3(-1.4f, 0, -1.4f), new Vector3(0, 45, 0)).transform;
            var m = M("Chair", G(0.2f), 0.2f);
            Cb("Seat", c, new Vector3(0, 0.45f, 0), new Vector3(0.42f, 0.05f, 0.42f), m, true);
            Cb("Back", c, new Vector3(0, 0.75f, -0.19f), new Vector3(0.42f, 0.5f, 0.04f), m, true);
            foreach (var sx in new[] { -0.17f, 0.17f }) foreach (var sz in new[] { -0.17f, 0.17f })
                Cb("Leg", c, new Vector3(sx, 0.215f, sz), new Vector3(0.04f, 0.43f, 0.04f), m, true);
            c.gameObject.AddComponent<XRSimpleInteractable>(); // grabbable, immovable
            c.gameObject.AddComponent<GrabTracker>().kind = GrabKind.Other;
        }

        static void CeilingProps()
        {
            var mk = E("IntakeMark", null, Vector3.zero).transform;
            P(PrimitiveType.Cylinder, "Disc", mk, new Vector3(0, 0.005f, 0), new Vector3(0.7f, 0.005f, 0.7f), M("Mark", new Color(0.85f, 0.8f, 0.55f), 0.1f));
            var cc = E("MarkZone", mk, Vector3.zero).AddComponent<CapsuleCollider>();
            cc.isTrigger = true; cc.radius = 0.35f; cc.height = 2f; cc.center = new Vector3(0, 1f, 0);

            var sp = E("SpeakerGrille", null, new Vector3(0, 2.58f, 0)).gameObject;
            var a = sp.AddComponent<AudioSource>();
            a.playOnAwake = false; a.spatialBlend = 1f; a.minDistance = 1f; a.maxDistance = 8f;
            P(PrimitiveType.Cylinder, "Disc", sp.transform, Vector3.zero, new Vector3(0.3f, 0.008f, 0.3f), M("Grille", G(0.4f), 0.4f));
            var dk = M("Dark", G(0.05f), 0.05f);
            for (int i = -2; i <= 2; i++)
            {
                float x = 0.045f * i, len = 2f * Mathf.Sqrt(0.15f * 0.15f - x * x) * 0.9f;
                Cb("Slit", sp.transform, new Vector3(x, -0.005f, 0), new Vector3(0.012f, 0.004f, len), dk);
            }

            Cam("CeilingCamera", new Vector3(1.6f, 2.5f, -1.6f));
            Cam("CeilingCameraB", new Vector3(-1.6f, 2.5f, 1.6f));
        }

        static void Cam(string n, Vector3 pos)
        {
            var c = E(n, null, pos).transform;
            Cb("Body", c, Vector3.zero, new Vector3(0.1f, 0.09f, 0.18f), M("Cam", G(0.18f), 0.3f));
            P(PrimitiveType.Cylinder, "Lens", c, new Vector3(0, 0, 0.1f), new Vector3(0.05f, 0.02f, 0.05f), M("Dark", G(0.05f), 0.05f), false, new Vector3(90, 0, 0));
            Led(n == "CeilingCamera" ? "LED_Camera" : "RecLed", c, new Vector3(0.03f, 0.03f, 0.091f), new Vector3(0.012f, 0.012f, 0.004f));
            c.LookAt(Vector3.zero);
        }

        static void Tape()
        {
            var t = E("TapeMarks", null, Vector3.zero).transform;
            var m = M("Tape", new Color(0.72f, 0.66f, 0.4f), 0.05f);
            var r = new System.Random(3);
            float R(float a, float b) => a + (float)r.NextDouble() * (b - a);
            for (int i = 0; i < 16; i++)
            {
                float ang = R(0, 6.28f), rad = R(0.5f, 1.7f);
                Qd("Strip", t, new Vector3(Mathf.Cos(ang) * rad, 0.003f, Mathf.Sin(ang) * rad), new Vector2(R(0.2f, 0.4f), 0.025f), m, new Vector3(90, R(0, 180), 0));
            }
            for (int k = 0; k < 5; k++) // X marks where others stood
            {
                float ang = R(0, 6.28f), rad = R(0.7f, 1.5f), yaw = R(0, 90);
                var p = new Vector3(Mathf.Cos(ang) * rad, 0.003f, Mathf.Sin(ang) * rad);
                Qd("X1", t, p, new Vector2(0.14f, 0.022f), m, new Vector3(90, yaw + 45, 0));
                Qd("X2", t, p, new Vector2(0.14f, 0.022f), m, new Vector3(90, yaw - 45, 0));
            }
        }

        static void Lights()
        {
            var l = E("Lights", null, Vector3.zero).transform;
            Light L(string n, Vector3 p, Color c, float range, float intensity)
            {
                var li = E(n, l, p).AddComponent<Light>();
                li.type = LightType.Point; li.color = c; li.range = range; li.intensity = intensity; li.shadows = LightShadows.None;
                return li;
            }
            L("RoomLight_Ceiling", new Vector3(0, 2.35f, 0), new Color(1f, 0.85f, 0.65f), 6.5f, 2f); // warm default
            L("BoothLight", new Vector3(0, 2.2f, 2.7f), new Color(0.6f, 0.75f, 1f), 2.5f, 0.8f);
        }

        static void Rig()
        {
            E("Spawn", null, new Vector3(0, 0, -1.2f));
            var src = AssetDatabase.FindAssets("\"XR Origin (XR Rig)\" t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => p.Contains("Starter Assets") && p.EndsWith("XR Origin (XR Rig).prefab"));
            if (src == null) Debug.LogWarning("[BuildLab] Starter Assets 'XR Origin (XR Rig)' prefab not found, add the rig by hand at Spawn");
            else
            {
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(RigCopy)) AssetDatabase.CopyAsset(src, RigCopy); // copy, never edit Samples
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RigCopy));
                rig.name = "XROrigin";
                rig.transform.SetPositionAndRotation(new Vector3(0, 0, -1.2f), Quaternion.identity); // facing +Z
                rig.AddComponent<DesktopRig>(); // self-disables when a headset or XR simulator is present
            }
        }

        static void Systems()
        {
            var s = E("Systems", null, Vector3.zero).transform;
            foreach (var n in new[] { "ExperimentRunner", "InputSampler", "DialogueManager", "RoomStateController", "TallyWall", "SessionLogger", "DebugPanel", "SeatedMode" })
                TryAdd(E(n, s, Vector3.zero), n);
        }

        // ---------- reflection-safe component add + wiring ----------
        static Component TryAdd(GameObject g, string typeName)
        {
            var t = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(x => x.Name == typeName && !x.IsAbstract);
            if (t == null) { Debug.LogWarning($"[BuildLab] type {typeName} not found, add it to {g.name} by hand"); return null; }
            var c = g.GetComponent(t);
            return c ? c : g.AddComponent(t);
        }

        static string Norm(string s) => s.ToLowerInvariant().Replace("_", "").Replace(" ", "").TrimStart('m');

        // fill null object refs on PerfectParticipant* components: by scene object name, else by component type
        static void AutoWire()
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var tr = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
            var byName = new Dictionary<string, Transform>();
            foreach (var t in tr) byName.TryAdd(Norm(t.name), t);
            var mine = roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(c => c && (c.GetType().Namespace ?? "").StartsWith("PerfectParticipant")).ToList();
            foreach (var c in mine)
            {
                var so = new SerializedObject(c);
                for (var it = so.GetIterator(); it.NextVisible(true);)
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue || it.name == "m_Script") continue;
                    var ft = FieldOf(c.GetType(), it.name)?.FieldType;
                    if (ft == null) continue;
                    Object v = null;
                    if (byName.TryGetValue(Norm(it.name), out var hit))
                        v = ft == typeof(GameObject) ? hit.gameObject : ft == typeof(Transform) ? hit : typeof(Component).IsAssignableFrom(ft) ? hit.GetComponent(ft) : null;
                    if (!v && typeof(Component).IsAssignableFrom(ft) && (ft.Namespace ?? "").StartsWith("PerfectParticipant"))
                        v = mine.FirstOrDefault(m => ft.IsInstanceOfType(m));
                    if (v) it.objectReferenceValue = v;
                    else if (typeof(Component).IsAssignableFrom(ft) || ft == typeof(GameObject))
                        Debug.LogWarning($"[BuildLab] unwired {c.GetType().Name}.{it.name} ({ft.Name})");
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static FieldInfo FieldOf(System.Type t, string n)
        {
            for (; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                var f = t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f != null) return f;
            }
            return null;
        }
    }
}
