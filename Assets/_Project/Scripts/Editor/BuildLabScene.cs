using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PerfectParticipant.Core;
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
            RenderSettings.ambientLight = new Color(0.45f, 0.44f, 0.42f); // gamma value; 0.2 is ~0.03 linear and rendered near black

            Shell(); LabDetails(); Window(); Tally(); Cork(); ConsoleObj(); TableObj(); DoorObj(); Corridor(); Shelf(); ChairObj();
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
                // no reflection probe in the room: default sky reflection painted a horizon on the glass
                m.SetFloat("_EnvironmentReflections", 0f);
                m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            }
            EditorUtility.SetDirty(m);
            return mats[key] = m;
        }

        static Color G(float v) => new Color(v, v, v);

        // Cube faces map UV 0..1, so tiling = face size / texture span.
        static void Tex(Material m, Texture2D t, float faceMetres, float faceMetresV = -1f)
        {
            m.SetTexture("_BaseMap", t);
            m.SetTextureScale("_BaseMap", new Vector2(faceMetres, faceMetresV < 0f ? faceMetres : faceMetresV) / LabTextures.Span);
            EditorUtility.SetDirty(m);
        }

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
            var wall = M("Wall", new Color(0.8f, 0.8f, 0.77f)); var floor = M("Floor", G(1f), 0.3f); var ceil = M("Ceiling", G(1f)); // wall below paper (0.88) so posters read
            Tex(floor, LabTextures.Floor(), 4.2f); Tex(ceil, LabTextures.Ceiling(), 4.2f); Tex(wall, LabTextures.Wall(), 2.4f);
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
            Cb("BoothBack", booth, new Vector3(0, 1.3f, 3.35f), new Vector3(4.2f, 2.6f, 0.1f), M("BoothBack", G(0.4f), 0.05f)); // lit backdrop: dark silhouettes read against it
            Cb("BoothFloor", booth, new Vector3(0, -0.05f, 2.7f), new Vector3(4.2f, 0.1f, 1.2f), dark);
            Cb("BoothCeiling", booth, new Vector3(0, 2.65f, 2.7f), new Vector3(4.2f, 0.1f, 1.2f), dark);
            Cb("BoothWest", booth, new Vector3(-2.05f, 1.3f, 2.7f), new Vector3(0.1f, 2.6f, 1.2f), dark);
            Cb("BoothEast", booth, new Vector3(2.05f, 1.3f, 2.7f), new Vector3(0.1f, 2.6f, 1.2f), dark);
            var sil = M("Silhouette", new Color(0.02f, 0.02f, 0.025f), 0f);
            float[] sx = { -0.55f, 0f, 0.55f }, sh = { 0.85f, 0.8f, 0.9f };
            for (int i = 0; i < 3; i++)
                P(PrimitiveType.Capsule, $"Silhouette_0{i + 1}", booth, new Vector3(sx[i], sh[i], 2.6f), new Vector3(0.5f, sh[i], 0.35f), sil).SetActive(false);
        }

        // Institutional lab finish: mint-grey dado to 0.9 m, dark rubber skirting, two recessed light panels.
        static void LabDetails()
        {
            var d = E("LabDetails", null, Vector3.zero).transform;
            var band = M("WallBand", new Color(0.66f, 0.76f, 0.72f), 0.25f);
            var skirt = M("Skirting", G(0.12f), 0.35f);
            const float off = 0.003f; // in front of the 2.0 m inner wall faces, behind the dents (1.992)
            void Run(string n, Vector3 c, float w, float yaw)
            {
                var r = Quaternion.Euler(0, yaw, 0);
                Static(Qd(n + "_Band", d, c + Vector3.up * 0.45f, new Vector2(w, 0.9f), band, new Vector3(0, yaw, 0)));
                Static(Cb(n + "_Skirt", d, c + Vector3.up * 0.05f + r * Vector3.back * 0.01f, new Vector3(w, 0.1f, 0.02f), skirt, false, new Vector3(0, yaw, 0)));
            }
            Run("North", new Vector3(0, 0, 2f - off), 4f, YawN);
            Run("West", new Vector3(-2f + off, 0, 0), 4f, YawW);
            Run("East", new Vector3(2f - off, 0, 0), 4f, YawE);
            Run("SouthL", new Vector3(-1.265f, 0, -2f + off), 1.47f, YawS); // door frame outer edge at x +-0.53
            Run("SouthR", new Vector3(1.265f, 0, -2f + off), 1.47f, YawS);

            var panel = M("LightPanel", G(0.9f), 0.1f, new Color(1f, 0.96f, 0.9f) * 1.6f);
            foreach (float z in new[] { -0.9f, 0.9f })
                Static(Cb("LightPanel", d, new Vector3(0, 2.594f, z), new Vector3(1.2f, 0.012f, 0.6f), panel));
        }

        static void Window()
        {
            var w = E("ObservationWindow", null, new Vector3(0, 1.5f, 2f)).transform;
            Qd("Glass", w, new Vector3(0, 0, -0.012f), new Vector2(1.5f, 1.2f), M("Glass", new Color(0.03f, 0.05f, 0.07f, 0.7f), 0.9f, null, true));
            var f = M("Trim", G(0.12f));
            Cb("FrameTop", w, new Vector3(0, 0.62f, -0.02f), new Vector3(1.62f, 0.04f, 0.04f), f);
            Cb("FrameBottom", w, new Vector3(0, -0.62f, -0.02f), new Vector3(1.62f, 0.04f, 0.04f), f);
            Cb("FrameLeft", w, new Vector3(-0.77f, 0, -0.02f), new Vector3(0.04f, 1.2f, 0.04f), f);
            Cb("FrameRight", w, new Vector3(0.77f, 0, -0.02f), new Vector3(0.04f, 1.2f, 0.04f), f);
            Cb("Sill", w, new Vector3(0, -0.64f, -0.07f), new Vector3(1.7f, 0.03f, 0.12f), f);
            Cb("WindowBlinds", w, new Vector3(0, 0, -0.045f), new Vector3(1.5f, 1.2f, 0.01f), M("Blind", G(0.45f))); // RoomStateController raises it
            var pane = E("GlassCollider", w, new Vector3(0, 0, -0.012f)).AddComponent<BoxCollider>(); // the glass is a quad: without this, thrown things fly into the booth
            pane.size = new Vector3(1.5f, 1.2f, 0.03f);
            // dents at cube height, previous participants threw it
            var d = M("Dent", G(0.38f));
            for (int i = 0; i < 3; i++) Qd($"Dent_{i + 1}", null, new Vector3(-0.4f + 0.35f * i, 0.8f + 0.02f * i, 1.992f), new Vector2(0.07f, 0.07f), d, new Vector3(0, 0, 20 * i));
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
            // easter egg: key taped under the top (underside y 0.70); opens the forbidden Rooms 3 and 6. Crouch (C) to see it.
            Qd("KeyTape", t, new Vector3(0.05f, 0.699f, 0.35f), new Vector2(0.09f, 0.04f), M("Tape", new Color(0.72f, 0.66f, 0.4f), 0.05f), new Vector3(-90, 0, 0));
            var key = Cb("Key", null, new Vector3(1.15f, 0.69f, 0.35f), new Vector3(0.07f, 0.008f, 0.025f), M("Metal", G(0.55f), 0.7f), true);
            var krb = key.AddComponent<Rigidbody>(); krb.isKinematic = true; krb.mass = 0.05f; // taped: stays until grabbed, then GrabTracker frees it
            krb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            key.AddComponent<XRGrabInteractable>().movementType = XRBaseInteractable.MovementType.VelocityTracking;
            key.AddComponent<GrabTracker>().kind = GrabKind.Key;

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

        }

        // Corridor straight on from the lab door (-Z) with Rooms 2..8 on both sides. Layout numbers live in Core's
        // CorridorLayout, so the flow and the scene agree. Lab = Room One (P-01). Route 2 > 4 > 7 > 8; 3 and 6 locked.
        static void Corridor()
        {
            var c = E("Corridor", null, Vector3.zero).transform;
            var wall = M("Wall", new Color(0.8f, 0.8f, 0.77f));
            float h = CorridorLayout.CorridorHalf, w = CorridorLayout.Wall, z0 = CorridorLayout.LabDoorZ - w, z1 = CorridorLayout.EndZ;
            float len = z0 - z1, cz = (z0 + z1) * 0.5f;
            var floor = M("CorridorFloor", G(1f), 0.3f); Tex(floor, LabTextures.Floor(), 2f * h, len);
            var ceil = M("CorridorCeiling", G(1f)); Tex(ceil, LabTextures.Ceiling(), 2f * h, len);
            void W(string n, Vector3 p, Vector3 s, Material m) => Static(Cb(n, c, p, s, m, true));
            W("CorridorFloor", new Vector3(0, -0.05f, cz), new Vector3(2f * h, 0.1f, len), floor);
            W("CorridorCeiling", new Vector3(0, 2.65f, cz), new Vector3(2f * h, 0.1f, len), ceil);
            W("CorridorEnd", new Vector3(0, 1.3f, z1 - w * 0.5f), new Vector3(2f * h + 2f * w, 2.6f, w), wall);

            // side walls: lintel over the doorways, wall pieces between them
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * (h + w * 0.5f);
                W("CorridorLintel", new Vector3(x, 2.35f, cz), new Vector3(w, 0.5f, len), wall);
                float top = z0;
                for (int n = side < 0 ? 2 : 3; n <= CorridorLayout.Last + 2; n += 2)
                {
                    bool door = n <= CorridorLayout.Last;
                    float bottom = door ? CorridorLayout.DoorZ(n) + 0.45f : z1;
                    W("CorridorPier", new Vector3(x, 1.05f, (top + bottom) * 0.5f), new Vector3(w, 2.1f, top - bottom), wall);
                    if (!door) break;
                    top = CorridorLayout.DoorZ(n) - 0.45f;
                }
            }

            var panel = M("LightPanel", G(0.9f), 0.1f, new Color(1f, 0.96f, 0.9f) * 1.6f);
            for (int k = 0; k < CorridorLayout.Slots; k++)
            {
                float z = CorridorLayout.FirstDoorZ - CorridorLayout.SlotLen * k;
                Static(Cb("LightPanel", c, new Vector3(0, 2.594f, z), new Vector3(0.6f, 0.012f, 1.2f), panel));
                CLight(c, new Vector3(0, 2.4f, z), 4f, 3f);
            }

            // Room One sign beside the lab door, corridor side (viewer faces +Z: right is +X)
            var paper = M("Paper", G(0.88f), 0.05f);
            Qd("RoomSign_1", c, new Vector3(0.78f, 1.5f, -2.104f), new Vector2(0.34f, 0.12f), paper);
            T("RoomSignText_1", c, new Vector3(0.78f, 1.5f, -2.106f), Vector3.zero, "ROOM 1", 0.06f, new Vector2(0.32f, 0.1f), Ink);

            Rooms(c, wall);
            for (int n = CorridorLayout.First; n <= CorridorLayout.Last; n++) RoomDoor(c, n, paper);
        }

        static void CLight(Transform p, Vector3 pos, float range, float intensity)
        {
            var li = E("CorridorLight", p, pos).AddComponent<Light>(); // not RoomLight*: tier and brown-out leave it alone
            li.type = LightType.Point; li.color = new Color(0.95f, 0.97f, 1f); li.range = range; li.intensity = intensity; li.shadows = LightShadows.None;
        }

        // Yaw that turns local +Z toward the corridor (room interior = local -Z) for a door / item on this side.
        static float FaceCorridor(int n) => CorridorLayout.Side(n) < 0 ? 90f : -90f;

        // Numbered door in the corridor side wall. Local +Z = corridor. Leaf_n pivots at the hinge
        // (RoomStateController swings it 90 deg into the room).
        static void RoomDoor(Transform c, int n, Material paper)
        {
            var leafM = M("Door", G(0.4f), 0.2f); var trim = M("Trim", G(0.12f)); var metal = M("Metal", G(0.55f), 0.7f);
            var s = new Vector3(0, YawS, 0); // faces local +Z, toward the player in the corridor
            float x = CorridorLayout.Side(n) * (CorridorLayout.CorridorHalf + CorridorLayout.Wall * 0.5f);
            var d = E("CorridorDoor_" + n, c, new Vector3(x, 0, CorridorLayout.DoorZ(n)), new Vector3(0, FaceCorridor(n), 0)).transform;
            Cb("FrameTop", d, new Vector3(0, 2.13f, 0.06f), new Vector3(1.06f, 0.06f, 0.04f), trim);
            Cb("FrameL", d, new Vector3(-0.5f, 1.05f, 0.06f), new Vector3(0.06f, 2.1f, 0.04f), trim);
            Cb("FrameR", d, new Vector3(0.5f, 1.05f, 0.06f), new Vector3(0.06f, 2.1f, 0.04f), trim);
            var pivot = E("Leaf_" + n, d, new Vector3(-0.45f, 0, 0)).transform;
            Cb("Leaf", pivot, new Vector3(0.45f, 1.05f, 0), new Vector3(0.9f, 2.1f, 0.06f), leafM, true);
            Qd("NumberPlate", pivot, new Vector3(0.45f, 1.5f, 0.032f), new Vector2(0.3f, 0.34f), paper, s);
            T("Number", pivot, new Vector3(0.45f, 1.5f, 0.034f), s, n.ToString(), 0.26f, new Vector2(0.28f, 0.32f), Ink);
            P(PrimitiveType.Cylinder, "Grip", pivot, new Vector3(0.78f, 1f, 0.08f), new Vector3(0.03f, 0.08f, 0.03f), metal, true);
            // room sign to the right of the door, as the player sees it (facing local -Z, right is local -X)
            Qd("RoomSign", d, new Vector3(-0.8f, 1.5f, 0.055f), new Vector2(0.3f, 0.12f), paper, s);
            T("RoomSignText", d, new Vector3(-0.8f, 1.5f, 0.057f), s, "ROOM " + n, 0.055f, new Vector2(0.28f, 0.1f), Ink);
            if (CorridorLayout.Forbidden(n))
            {
                Qd("NoEntry", d, new Vector3(-0.8f, 1.32f, 0.055f), new Vector2(0.3f, 0.1f), M("Warning", new Color(0.75f, 0.12f, 0.08f), 0.2f), s);
                T("NoEntryText", d, new Vector3(-0.8f, 1.32f, 0.057f), s, "STAFF ONLY", 0.045f, new Vector2(0.28f, 0.08f), G(0.95f));
            }
            d.gameObject.AddComponent<XRSimpleInteractable>(); // a grab attempt on leaf or handle = try the door; the flow opens it
            var g = d.gameObject.AddComponent<GrabTracker>();
            g.kind = GrabKind.CorridorDoor; g.number = n;
        }

        // Rooms 2..8, 3 m along the corridor and 3.5 m deep, one light each, with their props / tasks.
        // Walls between rooms sit inside each slot, so slot 0 never doubles the lab's south wall (z-fighting).
        static void Rooms(Transform c, Material wall)
        {
            var r = E("Rooms", c, Vector3.zero).transform;
            float h = CorridorLayout.CorridorHalf, w = CorridorLayout.Wall, depth = CorridorLayout.RoomDepth, slot = CorridorLayout.SlotLen;
            var rfloor = M("RoomFloor", G(1f), 0.3f); Tex(rfloor, LabTextures.Floor(), depth, slot);
            var rceil = M("RoomCeiling", G(1f)); Tex(rceil, LabTextures.Ceiling(), depth, slot);
            void W(string n, Vector3 p, Vector3 s, Material m) => Static(Cb(n, r, p, s, m, true));
            var paper = M("Paper", G(0.88f), 0.05f); var dk = M("Dark", G(0.05f), 0.05f); var wood = M("Table", new Color(0.45f, 0.36f, 0.27f), 0.2f);
            var chairM = M("Chair", G(0.2f), 0.2f);

            for (int n = CorridorLayout.First; n <= CorridorLayout.Last; n++)
            {
                int side = CorridorLayout.Side(n);
                float cx = CorridorLayout.CenterX(n), cz = CorridorLayout.DoorZ(n);
                float back = side * (h + w + depth);              // inner face of the far wall
                float yaw = FaceCorridor(n);                       // + 180 for items on the far wall that face the door
                W("RoomFloor_" + n, new Vector3(cx, -0.05f, cz), new Vector3(depth, 0.1f, slot), rfloor);
                W("RoomCeiling_" + n, new Vector3(cx, 2.65f, cz), new Vector3(depth, 0.1f, slot), rceil);
                W("RoomBack_" + n, new Vector3(back + side * w * 0.5f, 1.3f, cz), new Vector3(w, 2.6f, slot), wall);
                W("RoomSideN_" + n, new Vector3(cx, 1.3f, cz + (slot - w) * 0.5f), new Vector3(depth, 2.6f, w), wall);
                W("RoomSideS_" + n, new Vector3(cx, 1.3f, cz - (slot - w) * 0.5f), new Vector3(depth, 2.6f, w), wall);
                CLight(r, new Vector3(cx, 2.35f, cz), 3.5f, CorridorLayout.Forbidden(n) ? 0.8f : 2.5f); // forbidden rooms dim
                Vector3 onBack(float y, float off) => new Vector3(back - side * off, y, cz);

                switch (n)
                {
                    case 2: // shelf on the far wall, a box on the floor near the door
                        Cb("Shelf", r, onBack(1.1f, 0.17f), new Vector3(0.3f, 0.03f, 1.2f), wood, true);
                        Zone("ShelfZone", r, onBack(1.25f, 0.17f), new Vector3(0.3f, 0.26f, 1.2f));
                        var box = Cb("Box", null, new Vector3(cx - side * 0.9f, 0.1f, cz + 0.8f), Vector3.one * 0.2f, M("Box", new Color(0.62f, 0.48f, 0.3f), 0.15f), true);
                        var brb = box.AddComponent<Rigidbody>(); brb.mass = 0.5f; brb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                        box.AddComponent<XRGrabInteractable>().movementType = XRBaseInteractable.MovementType.VelocityTracking;
                        box.AddComponent<GrabTracker>().kind = GrabKind.Other;
                        break;
                    case 3: // forbidden: shelves of participant files
                        for (int row = 0; row < 3; row++)
                        {
                            Cb("FileShelf", r, onBack(0.5f + 0.6f * row, 0.2f), new Vector3(0.35f, 0.03f, 2.4f), wood, true);
                            for (int k = 0; k < 10; k++)
                                Cb("FileBox", r, onBack(0.64f + 0.6f * row, 0.2f) + new Vector3(0, 0, -1.1f + 0.24f * k), new Vector3(0.3f, 0.25f, 0.18f), paper);
                        }
                        T("FilesLabel", r, onBack(2.1f, 0.01f), new Vector3(0, yaw + 180f, 0), "PARTICIPANTS 0001 - 0416", 0.06f, new Vector2(2.4f, 0.12f), Ink);
                        break;
                    case 4: // the screen to look at
                        var scr = Qd("Room4Screen", null, onBack(1.5f, 0.01f), new Vector2(1.4f, 0.8f),
                            M("Screen", new Color(0.02f, 0.05f, 0.04f), 0.5f, new Color(0.03f, 0.1f, 0.07f)), new Vector3(0, yaw + 180f, 0));
                        T("ScreenText", scr.transform, new Vector3(0, 0, -0.002f), Vector3.zero, "PARTICIPANT 0417\n<size=60%>PLEASE KEEP LOOKING</size>", 0.1f, new Vector2(1.3f, 0.7f), new Color(0.5f, 1f, 0.7f));
                        break;
                    case 5: RoomButton(r, cx, cz, 5, "I DISAGREE"); break;
                    case 6: // forbidden: where the observers sit, chairs facing a dark pane
                        Qd("ObserverPane", r, onBack(1.5f, 0.01f), new Vector2(2f, 1f), dk, new Vector3(0, yaw + 180f, 0));
                        for (int k = 0; k < 3; k++)
                        {
                            var ch = E("ObserverChair", r, new Vector3(back - side * 1.1f, 0, cz - 0.7f + 0.7f * k), new Vector3(0, yaw + 180f, 0)).transform;
                            Cb("Seat", ch, new Vector3(0, 0.45f, 0), new Vector3(0.38f, 0.05f, 0.38f), chairM, true);
                            Cb("Back", ch, new Vector3(0, 0.72f, -0.17f), new Vector3(0.38f, 0.45f, 0.04f), chairM, true);
                            Qd("ObserverNotes", ch, new Vector3(0, 0.476f, 0), new Vector2(0.18f, 0.24f), paper, new Vector3(90, 0, 0));
                        }
                        break;
                    case 7: RoomButton(r, cx, cz, 7, "I AGREE AGAIN"); break;
                    case 8: // the last chair at the room centre, facing the door: sit (stand in its seat) to finish
                        var c8 = E("Room8Chair", r, new Vector3(cx, 0, cz), new Vector3(0, yaw, 0)).transform;
                        Cb("Seat", c8, new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.05f, 0.45f), chairM);
                        Cb("Back", c8, new Vector3(0, 0.75f, -0.21f), new Vector3(0.45f, 0.5f, 0.04f), chairM);
                        break;
                }
            }
        }

        // Pedestal with a labelled cap at the room centre; a grab attempt on it = RoomButton n (same desk/XR path as the doors).
        static void RoomButton(Transform r, float x, float z, int n, string label)
        {
            Cb("Pedestal", r, new Vector3(x, 0.5f, z), new Vector3(0.4f, 1f, 0.4f), M("Console", G(0.3f), 0.25f), true);
            var b = E("RoomButton_" + n, r, new Vector3(x, 1.0f, z)).transform;
            P(PrimitiveType.Cylinder, "Cap", b, new Vector3(0, 0.015f, 0), new Vector3(0.16f, 0.015f, 0.16f), M("ConsentOrange", new Color(1f, 0.42f, 0.05f), 0.55f, Color.black), true);
            T("Label", b, new Vector3(0, 0.032f, 0), new Vector3(90, FaceCorridor(n) + 180f, 0), label, 0.016f, new Vector2(0.15f, 0.05f), Ink);
            b.gameObject.AddComponent<XRSimpleInteractable>();
            var g = b.gameObject.AddComponent<GrabTracker>();
            g.kind = GrabKind.RoomButton; g.number = n;
        }

        static void Shelf()
        {
            var s = E("ClipboardShelf", null, new Vector3(-1.95f, 1.4f, 1f)).transform;
            Cb("Board", s, Vector3.zero, new Vector3(0.2f, 0.03f, 0.6f), M("Table", new Color(0.45f, 0.36f, 0.27f), 0.2f), true);
            // root level so package A can grab it; leans on the west wall
            var c = E("Clipboard", null, new Vector3(-1.92f, 1.575f, 1f), new Vector3(0, 0, 12)).transform;
            Cb("Board", c, Vector3.zero, new Vector3(0.015f, 0.31f, 0.23f), M("Trim", G(0.12f)), true);
            var crb = c.gameObject.AddComponent<Rigidbody>();
            crb.isKinematic = true; // RoomStateController frees it on R-03, GrabTracker on release
            crb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // thin board tunnelled through the 0.1 m wall
            crb.mass = 0.4f;
            c.gameObject.AddComponent<XRGrabInteractable>().movementType = XRBaseInteractable.MovementType.VelocityTracking; // collides with walls while held
            c.gameObject.AddComponent<GrabTracker>().kind = GrabKind.Clipboard; // own kind: not "other object moved" in the tests
            Led("LED_Clipboard", c, new Vector3(0.01f, 0.15f, 0.09f), new Vector3(0.006f, 0.012f, 0.012f));
            var y = new Vector3(0, YawW, 0);
            // brief: "The line for C is torn off". "C." survives on a stub at the left; the rest of its line is ripped away,
            // so the eye reads that something should follow. Local z = across the sheet (-0.1 left), y = up.
            var paper = M("Paper", G(0.88f), 0.05f);
            Qd("Paper", c, new Vector3(0.0085f, 0.0775f, 0), new Vector2(0.2f, 0.095f), paper, y); // top 0.125 down to 0.03, the top of the C line
            Qd("Stub", c, new Vector3(0.0085f, 0.012f, -0.083f), new Vector2(0.034f, 0.036f), paper, y); // carries "C."
            for (int i = 0; i < 7; i++) // ragged edge along the rip, right of the stub
            {
                float ts = 0.014f + 0.006f * ((i * 5) % 3);
                Qd("Tear", c, new Vector3(0.0085f, 0.03f - 0.003f * ((i * 3) % 4), -0.055f + 0.025f * i), new Vector2(ts, ts), paper, new Vector3(0, YawW, 45));
            }
            for (int i = 0; i < 2; i++) // and under the stub
                Qd("Tear", c, new Vector3(0.0085f, -0.006f - 0.003f * i, -0.093f + 0.017f * i), new Vector2(0.014f, 0.014f), paper, new Vector3(0, YawW, 45));
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
            Led(n == "CeilingCamera" ? "LED_Camera" : "LED_CameraB", c, new Vector3(0.03f, 0.03f, 0.091f), new Vector3(0.018f, 0.018f, 0.006f)); // blinked by RoomStateController
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
            L("RoomLight_Ceiling", new Vector3(0, 2.35f, 0), new Color(1f, 0.85f, 0.65f), 7f, 4f); // warm default; 2 left the walls near black (Docs/screenshots)
            L("BoothLight", new Vector3(0, 2.2f, 2.9f), new Color(0.6f, 0.75f, 1f), 1.6f, 2.5f); // short range: no shadows, keep it out of the room
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
