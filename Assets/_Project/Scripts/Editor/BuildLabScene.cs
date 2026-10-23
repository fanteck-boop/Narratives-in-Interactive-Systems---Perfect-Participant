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

            Shell(); LabDetails(); Window(); Tally(); Cork(); ConsoleObj(); TableObj(); DoorObj(); Corridor(); Shelf(); ChairObj(); MoreLore();
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

        const float Lift = 0.15f; // playtest: table top 0.75 -> 0.90 m, so a crouching player gets under it to the keycard
        static void TableObj()
        {
            var t = E("Table", null, new Vector3(1.1f, 0, 0)).transform;
            var wood = M("Table", new Color(0.45f, 0.36f, 0.27f), 0.2f);
            Cb("Top", t, new Vector3(0, 0.725f + Lift, 0), new Vector3(0.7f, 0.05f, 1.2f), wood, true);
            foreach (var sx in new[] { -0.3f, 0.3f }) foreach (var sz in new[] { -0.55f, 0.55f })
                Cb("Leg", t, new Vector3(sx, (0.7f + Lift) * 0.5f, sz), new Vector3(0.05f, 0.7f + Lift, 0.05f), wood, true);
            Zone("TableZone", t, new Vector3(0, 0.81f + Lift, 0), new Vector3(0.7f, 0.12f, 1.2f));
            // easter egg: staff keycard taped under the top (underside y 0.70); swipe it at the readers of Rooms 3 and 6.
            // Crouch (C) to see it. The two tape strips belong to the table and stay when the card is taken.
            var tape = M("Tape", new Color(0.72f, 0.66f, 0.4f), 0.05f);
            foreach (float tz in new[] { 0.31f, 0.39f })
                Qd("KeyTape", t, new Vector3(0.05f, 0.6985f + Lift, tz), new Vector2(0.11f, 0.018f), tape, new Vector3(-90, 0, 0));
            var key = Cb("Key", null, new Vector3(1.15f, 0.694f + Lift, 0.35f), new Vector3(0.086f, 0.004f, 0.054f), M("Keycard", G(0.92f), 0.4f), true);
            Qd("Stripe", key.transform, new Vector3(0, -0.6f, 0.25f), new Vector2(1f, 0.2f), M("Dark", G(0.05f), 0.05f), new Vector3(-90, 0, 0)); // mag stripe, underside
            T("CardText", key.transform, new Vector3(0, 0.6f, 0), new Vector3(90, 0, 0), "STAFF", 0.25f, new Vector2(0.9f, 0.4f), Ink);
            var krb = key.AddComponent<Rigidbody>(); krb.isKinematic = true; krb.mass = 0.05f; // taped: stays until grabbed, then GrabTracker frees it
            krb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            key.AddComponent<XRGrabInteractable>().movementType = XRBaseInteractable.MovementType.VelocityTracking;
            key.AddComponent<GrabTracker>().kind = GrabKind.Key;

            var cube = P(PrimitiveType.Cube, "Cube", null, new Vector3(1.0f, 0.79f + Lift, -0.1f), Vector3.one * 0.08f, M("Cube", new Color(0.93f, 0.93f, 0.9f), 0.1f), true, new Vector3(0, 17, 0));
            var rb = cube.AddComponent<Rigidbody>();
            rb.mass = 0.2f; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            cube.AddComponent<XRGrabInteractable>().movementType = XRBaseInteractable.MovementType.VelocityTracking;
            cube.AddComponent<GrabTracker>().kind = GrabKind.Cube;

            var tray = E("Tray", null, new Vector3(1.15f, 0.76f + Lift, 0.15f)).transform;
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
                Speaker(c, new Vector3(0, 2.58f, z - 1.05f)); // between the light panels, one per corridor section
            }

            // Room One sign beside the lab door, corridor side (viewer faces +Z: right is +X)
            var paper = M("Paper", G(0.88f), 0.05f);
            Qd("RoomSign_1", c, new Vector3(0.78f, 1.5f, -2.104f), new Vector2(0.34f, 0.12f), paper);
            T("RoomSignText_1", c, new Vector3(0.78f, 1.5f, -2.106f), Vector3.zero, "ROOM 1", 0.06f, new Vector2(0.32f, 0.1f), Ink);

            // notices: the purpose stays at the edge of notice (AAD, brief 17.5); the left wall past Room 7 has no doors
            void Notice(Vector3 p, float yawN, string s)
            {
                Qd("Notice", c, p, new Vector2(0.6f, 0.42f), paper, new Vector3(0, yawN, 0));
                T("NoticeText", c, p + Quaternion.Euler(0, yawN, 0) * new Vector3(0, 0, -0.002f), new Vector3(0, yawN, 0), s, 0.045f, new Vector2(0.54f, 0.38f), Ink);
            }
            float lw = h - 0.001f;
            Notice(new Vector3(lw, 1.5f, -12.0f), YawE, "PARTICIPATION IS\nVOLUNTARY\n<size=55%>Please remain in the corridor between rooms.</size>");
            Notice(new Vector3(lw, 1.5f, -13.3f), YawE, "THANK YOU\nFOR YOUR\nCOOPERATION");
            Cam("CorridorCamera", new Vector3(0.55f, 2.5f, z1 + 0.15f)); // watches the whole corridor, follows the head (RoomStateController)

            Rooms(c, wall);
            for (int n = CorridorLayout.First; n <= CorridorLayout.Hall; n++) RoomDoor(c, n, paper);
            Hall(c, wall, paper);
        }

        // The hall past the corridor end, opened after Room 8: rows of empty chairs, one kept for 0417, and the first
        // time the study names itself (brief 17.5: the purpose arrives only at the end). Its near wall closes the corridor.
        static void Hall(Transform c, Material wall, Material paper)
        {
            var hl = E("Hall", c, Vector3.zero).transform;
            float hw = CorridorLayout.HallHalf, d = CorridorLayout.HallDepth, ht = CorridorLayout.HallHeight, w = CorridorLayout.Wall;
            float z0 = CorridorLayout.EndZ - w, z1 = z0 - d, cz = (z0 + z1) * 0.5f, wz = CorridorLayout.EndZ - w * 0.5f;
            var floor = M("HallFloor", G(1f), 0.35f); Tex(floor, LabTextures.Floor(), 2f * hw, d);
            var ceil = M("HallCeiling", G(1f)); Tex(ceil, LabTextures.Ceiling(), 2f * hw, d);
            void W(string n, Vector3 p, Vector3 s, Material m) => Static(Cb(n, hl, p, s, m, true));
            W("HallFloor", new Vector3(0, -0.05f, cz), new Vector3(2f * hw, 0.1f, d), floor);
            W("HallCeiling", new Vector3(0, ht + 0.05f, cz), new Vector3(2f * hw, 0.1f, d), ceil);
            W("HallWallFar", new Vector3(0, ht * 0.5f, z1 - w * 0.5f), new Vector3(2f * hw + 2f * w, ht, w), wall);
            foreach (int s in new[] { -1, 1 })
            {
                if (s < 0) W("HallWallSide", new Vector3(s * (hw + w * 0.5f), ht * 0.5f, cz), new Vector3(w, ht, d), wall);
                float inner = 0.5f, outer = hw + w; // near wall either side of the 1 m doorway (frame at +-0.5)
                W("HallWallNear", new Vector3(s * (inner + outer) * 0.5f, ht * 0.5f, wz), new Vector3(outer - inner, ht, w), wall);
            }
            W("HallWallNearTop", new Vector3(0, (2.1f + ht) * 0.5f, wz), new Vector3(1f, ht - 2.1f, w), wall);

            var panel = M("LightPanel", G(0.9f), 0.1f, new Color(1f, 0.96f, 0.9f) * 1.6f);
            foreach (float lx in new[] { -3.5f, 3.5f })
                foreach (float lz in new[] { z0 - 3.5f, z0 - 10.5f })
                {
                    Static(Cb("LightPanel", hl, new Vector3(lx, ht - 0.006f, lz), new Vector3(1.2f, 0.012f, 1.2f), panel));
                    CLight(hl, new Vector3(lx, ht - 0.4f, lz), 12f, 6f);
                }
            Speaker(hl, new Vector3(0, ht - 0.02f, z0 - 5f));

            // 8 x 6 chairs facing the far wall; all empty, front row centre-left is tagged for the player
            var chairM = M("Chair", G(0.2f), 0.2f);
            int seat = 0;
            for (int row = 0; row < 6; row++)
                for (int col = 0; col < 8; col++)
                {
                    var ch = E("HallChair", hl, new Vector3(-3.5f + col, 0, z1 + 4f + row * 1.2f)).transform;
                    Cb("Seat", ch, new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.05f, 0.45f), chairM, true);
                    Cb("Back", ch, new Vector3(0, 0.72f, 0.2f), new Vector3(0.45f, 0.5f, 0.04f), chairM, true);
                    Cb("Stem", ch, new Vector3(0, 0.22f, 0), new Vector3(0.05f, 0.44f, 0.05f), M("Metal", G(0.55f), 0.7f));
                    Static(ch.gameObject);
                    if (row == 0 && col == 3)
                    {
                        Qd("ReservedTag", ch, new Vector3(0, 0.476f, 0), new Vector2(0.2f, 0.12f), paper, new Vector3(90, 180, 0));
                        T("ReservedText", ch, new Vector3(0, 0.477f, 0), new Vector3(90, 180, 0), "RESERVED\n<b>0417</b>", 0.035f, new Vector2(0.19f, 0.11f), Ink);
                        continue;
                    }
                    // every other seat already had its participant: number on the back, read from the entrance (+Z)
                    int id = 370 + seat++;
                    T("ChairNumber", ch, new Vector3(0, 0.85f, 0.222f), new Vector3(0, YawS, 0), id.ToString("0000"), 0.05f, new Vector2(0.3f, 0.08f), G(0.75f));
                    if (id == 0402) Cb("Jacket", ch, new Vector3(0, 0.78f, 0.24f), new Vector3(0.46f, 0.3f, 0.03f), M("Jacket", new Color(0.12f, 0.16f, 0.28f), 0.1f)); // over the back, left behind
                    if (id == 0411) Grabbable(Cb("BroughtCube", ch, new Vector3(0, 0.53f, 0), new Vector3(0.1f, 0.1f, 0.1f), M("BroughtCube", new Color(0.85f, 0.85f, 0.8f), 0.3f), true), 0.3f);
                    if (id == 0388) foreach (float sx in new[] { -0.07f, 0.07f }) Cb("Shoe", ch, new Vector3(sx, 0.05f, -0.1f), new Vector3(0.09f, 0.1f, 0.26f), M("Shoe", G(0.08f), 0.4f));
                }

            // the twist (team 2026-10-23): the voice was a participant. Its desk faces the chairs, its own chair says 0001.
            var wood = M("Table", new Color(0.45f, 0.36f, 0.27f), 0.2f); var dk = M("Dark", G(0.05f), 0.05f);
            float dz = z1 + 1.4f;
            Cb("VoiceDesk", hl, new Vector3(0, 0.375f, dz), new Vector3(1.4f, 0.75f, 0.6f), wood, true);
            P(PrimitiveType.Cylinder, "MicStand", hl, new Vector3(0.25f, 0.85f, dz + 0.1f), new Vector3(0.015f, 0.1f, 0.015f), M("Metal", G(0.55f), 0.7f));
            P(PrimitiveType.Sphere, "Mic", hl, new Vector3(0.25f, 0.97f, dz + 0.12f), new Vector3(0.06f, 0.06f, 0.06f), dk);
            Folder(hl, new Vector3(-0.3f, 0.756f, dz + 0.05f), 180f, "<b>PARTICIPANT 0001</b>\nResult: AS EXPECTED\nRetained as: VOICE\n<size=70%>Contract: until replaced.</size>");
            var vc = E("VoiceChair", hl, new Vector3(0, 0, dz - 0.65f)).transform;
            Cb("Seat", vc, new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.05f, 0.45f), chairM, true);
            Cb("Back", vc, new Vector3(0, 0.72f, -0.2f), new Vector3(0.45f, 0.5f, 0.04f), chairM, true);
            Cb("Stem", vc, new Vector3(0, 0.22f, 0), new Vector3(0.05f, 0.44f, 0.05f), M("Metal", G(0.55f), 0.7f));
            T("VoiceChairTag", vc, new Vector3(0, 0.8f, -0.178f), new Vector3(0, YawS, 0), "0001", 0.06f, new Vector2(0.3f, 0.1f), G(0.9f));
            T("OnAir", hl, new Vector3(0, 1.35f, z1 + 0.01f), new Vector3(0, YawS, 0), "ON AIR", 0.3f, new Vector2(2f, 0.4f), new Color(0.9f, 0.1f, 0.08f));
            var lamp = E("VoiceLamp", hl, new Vector3(0, 2.2f, dz)).AddComponent<Light>();
            lamp.type = LightType.Spot; lamp.spotAngle = 50f; lamp.range = 4f; lamp.intensity = 6f; lamp.color = new Color(1f, 0.85f, 0.6f);
            lamp.transform.rotation = Quaternion.Euler(90, 0, 0);

            // staff board on the side wall (faces +X, read left to right along +Z): every staff member was a participant
            float bx = -hw + 0.01f, bz = z1 + 7f;
            Qd("StaffBoard", hl, new Vector3(bx, 2f, bz + 1.5f), new Vector2(4.4f, 1.8f), M("Cork", new Color(0.6f, 0.45f, 0.3f), 0.05f), new Vector3(0, YawW, 0));
            CLight(hl, new Vector3(bx + 1.5f, 3f, bz + 1.5f), 4f, 2.5f);
            T("StaffTitle", hl, new Vector3(bx + 0.002f, 2.7f, bz + 1.5f), new Vector3(0, YawW, 0), "OUR STAFF", 0.12f, new Vector2(4.2f, 0.4f), Ink);
            string[] staff = { "0001\n<size=70%>VOICE</size>", "0391\n<size=70%>OBSERVER</size>", "0416\n<size=70%>OBSERVER</size>", "0417\n<size=70%>?</size>" };
            var portrait = M("Portrait", G(0.25f), 0.1f);
            for (int k = 0; k < staff.Length; k++)
            {
                var p = new Vector3(bx + 0.002f, 1.9f, bz + 0.15f + 0.9f * k);
                if (k < 3) Qd("Portrait", hl, p, new Vector2(0.5f, 0.6f), portrait, new Vector3(0, YawW, 0)); // the last frame is empty
                T("StaffName", hl, p + new Vector3(0.001f, -0.5f, 0), new Vector3(0, YawW, 0), staff[k], 0.1f, new Vector2(0.8f, 0.3f), Ink);
            }

            HallRightWall(hl, wall, paper, z0, z1);

            // the name, at last, across the far wall
            T("HallTitle", hl, new Vector3(0, 3.6f, z1 + 0.01f), new Vector3(0, YawS, 0), "STUDY 3B\n<size=45%>OBEDIENCE TO PROCEDURE</size>", 0.9f, new Vector2(12f, 3f), Ink);
            T("HallSub", hl, new Vector3(0, 1.9f, z1 + 0.01f), new Vector3(0, YawS, 0), "Thank you for your participation. Participant 0418 is waiting.", 0.35f, new Vector2(11f, 0.6f), G(0.15f));
        }

        // Hall right wall (+X) with two openings: the 0418 waiting room at eye height (the cycle goes on) and, high up,
        // the gallery where Condition B watches from behind glass (HallObserver_n turn after the head, RoomStateController).
        static void HallRightWall(Transform hl, Material wall, Material paper, float z0, float z1)
        {
            float hw = CorridorLayout.HallHalf, ht = CorridorLayout.HallHeight, w = CorridorLayout.Wall, x = hw + w * 0.5f;
            void W(string n, float zTop, float zBot, float yLo, float yHi, Transform p = null) =>
                Static(Cb(n, p ? p : hl, new Vector3(x, (yLo + yHi) * 0.5f, (zTop + zBot) * 0.5f), new Vector3(w, yHi - yLo, zTop - zBot), wall, true));
            const float wA = -16f, wB = -17.4f, wLo = 0.9f, wHi = 2.1f;   // waiting room window
            const float gA = -20f, gB = -24f, gLo = 3.2f, gHi = 4.6f;     // gallery window
            W("HallWallSide", z0, wA, 0, ht); W("HallWallSide", wA, wB, 0, wLo); W("HallWallSide", wA, wB, wHi, ht);
            W("HallWallSide", wB, gA, 0, ht); W("HallWallSide", gA, gB, 0, gLo); W("HallWallSide", gA, gB, gHi, ht);
            W("HallWallSide", gB, z1, 0, ht);
            var glass = M("Glass", new Color(0.03f, 0.05f, 0.07f, 0.7f), 0.9f, null, true);
            var dark = M("Dark", G(0.05f), 0.05f); var sil = M("Silhouette", new Color(0.02f, 0.02f, 0.025f), 0f);

            // a small lit box behind an opening: floor, ceiling, back and end walls
            Transform Box(string n, float zA, float zB, float yLo, float yHi, float back, Color light)
            {
                var b = E(n, hl, Vector3.zero).transform;
                float bx = hw + w, cx = (bx + back) * 0.5f, cz = (zA + zB) * 0.5f, dx = back - bx;
                Static(Cb("Floor", b, new Vector3(cx, yLo - 0.1f, cz), new Vector3(dx, 0.2f, zA - zB + 0.2f), wall, true));
                Static(Cb("Ceiling", b, new Vector3(cx, yHi + 0.05f, cz), new Vector3(dx, 0.1f, zA - zB + 0.2f), wall, true));
                Static(Cb("Back", b, new Vector3(back + w * 0.5f, (yLo + yHi) * 0.5f, cz), new Vector3(w, yHi - yLo, zA - zB + 0.2f), wall, true));
                foreach (float ez in new[] { zA + 0.05f, zB - 0.05f }) Static(Cb("End", b, new Vector3(cx, (yLo + yHi) * 0.5f, ez), new Vector3(dx, yHi - yLo, w), wall, true));
                var li = E("BoxLight", b, new Vector3(cx, yHi - 0.3f, cz)).AddComponent<Light>();
                li.type = LightType.Point; li.range = 4f; li.intensity = 2.5f; li.color = light; li.shadows = LightShadows.None;
                return b;
            }

            // 0418, waiting its turn. Seen through the glass, it sits as the player sat at the start.
            var wr = Box("WaitingRoom", wA + 0.8f, wB - 0.8f, 0f, 2.6f, hw + w + 2.4f, new Color(1f, 0.92f, 0.8f));
            Qd("WaitingGlass", wr, new Vector3(hw - 0.001f, (wLo + wHi) * 0.5f, (wA + wB) * 0.5f), new Vector2(wA - wB, wHi - wLo), glass, new Vector3(0, YawE, 0));
            float sx = hw + 1.6f, sz = (wA + wB) * 0.5f;
            Cb("Seat", wr, new Vector3(sx, 0.45f, sz), new Vector3(0.45f, 0.05f, 0.45f), M("Chair", G(0.2f), 0.2f), true);
            Cb("Back", wr, new Vector3(sx + 0.2f, 0.72f, sz), new Vector3(0.04f, 0.5f, 0.45f), M("Chair", G(0.2f), 0.2f), true);
            P(PrimitiveType.Capsule, "Waiting_0418", wr, new Vector3(sx, 0.95f, sz), new Vector3(0.42f, 0.5f, 0.34f), sil);
            P(PrimitiveType.Sphere, "Waiting_0418_Head", wr, new Vector3(sx - 0.03f, 1.55f, sz), new Vector3(0.22f, 0.25f, 0.22f), sil);
            Qd("WaitingTag", wr, new Vector3(hw + w + 2.39f, 1.7f, sz), new Vector2(0.4f, 0.18f), paper, new Vector3(0, YawE, 0));
            T("WaitingTagText", wr, new Vector3(hw + w + 2.388f, 1.7f, sz), new Vector3(0, YawE, 0), "0418\n<size=55%>PLEASE WAIT TO BE CALLED</size>", 0.06f, new Vector2(0.38f, 0.16f), Ink);
            Qd("WaitingSign", hl, new Vector3(hw - 0.002f, 2.35f, sz), new Vector2(0.7f, 0.14f), paper, new Vector3(0, YawE, 0));
            T("WaitingSignText", hl, new Vector3(hw - 0.004f, 2.35f, sz), new Vector3(0, YawE, 0), "WAITING ROOM", 0.07f, new Vector2(0.66f, 0.12f), Ink);

            // the gallery: three observers who were Condition B, notepads up, turning after the player
            var gl = Box("Gallery", gA + 0.4f, gB - 0.4f, gLo - 0.2f, 5.4f, hw + w + 1.8f, new Color(0.8f, 0.85f, 1f));
            Qd("GalleryGlass", gl, new Vector3(hw - 0.001f, (gLo + gHi) * 0.5f, (gA + gB) * 0.5f), new Vector2(gA - gB, gHi - gLo), glass, new Vector3(0, YawE, 0));
            Static(Cb("GalleryRail", hl, new Vector3(hw - 0.06f, gLo - 0.05f, (gA + gB) * 0.5f), new Vector3(0.12f, 0.1f, gA - gB + 0.2f), dark));
            float[] oz = { -21f, -22f, -23f }, oh = { 0.85f, 0.8f, 0.9f };
            for (int i = 0; i < 3; i++)
            {
                var o = E("HallObserver_" + (i + 1), gl, new Vector3(hw + 0.9f, gLo - 0.2f, oz[i]), new Vector3(0, -90f, 0)).transform; // local +Z = into the hall
                P(PrimitiveType.Capsule, "Body", o, new Vector3(0, oh[i], 0), new Vector3(0.5f, oh[i], 0.35f), sil);
                Cb("Notepad", o, new Vector3(0.12f, oh[i] + 0.15f, 0.22f), new Vector3(0.16f, 0.22f, 0.01f), paper);
            }
        }

        // Ceiling speaker look (disc + slits). The lab's SpeakerGrille carries the narrator AudioSource; corridor and
        // room ones ("NarratorSpeaker") are only spots RoomStateController moves that source to when the player is out there.
        static void Grille(Transform sp)
        {
            P(PrimitiveType.Cylinder, "Disc", sp, Vector3.zero, new Vector3(0.3f, 0.008f, 0.3f), M("Grille", G(0.4f), 0.4f));
            var dk = M("Dark", G(0.05f), 0.05f);
            for (int i = -2; i <= 2; i++)
            {
                float x = 0.045f * i, len = 2f * Mathf.Sqrt(0.15f * 0.15f - x * x) * 0.9f;
                Cb("Slit", sp, new Vector3(x, -0.005f, 0), new Vector3(0.012f, 0.004f, len), dk);
            }
        }

        static void Speaker(Transform p, Vector3 pos) => Grille(E("NarratorSpeaker", p, pos).transform);

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
            bool hall = n == CorridorLayout.Hall; // in the corridor end wall, swings into the hall
            var d = E("CorridorDoor_" + n, c, hall ? new Vector3(0, 0, CorridorLayout.EndZ - CorridorLayout.Wall * 0.5f) : new Vector3(x, 0, CorridorLayout.DoorZ(n)), new Vector3(0, hall ? 0f : FaceCorridor(n), 0)).transform;
            Cb("FrameTop", d, new Vector3(0, 2.13f, 0.06f), new Vector3(1.06f, 0.06f, 0.04f), trim);
            Cb("FrameL", d, new Vector3(-0.5f, 1.05f, 0.06f), new Vector3(0.06f, 2.1f, 0.04f), trim);
            Cb("FrameR", d, new Vector3(0.5f, 1.05f, 0.06f), new Vector3(0.06f, 2.1f, 0.04f), trim);
            var pivot = E("Leaf_" + n, d, new Vector3(-0.45f, 0, 0)).transform;
            Cb("Leaf", pivot, new Vector3(0.45f, 1.05f, 0), new Vector3(0.9f, 2.1f, 0.06f), leafM, true);
            Qd("NumberPlate", pivot, new Vector3(0.45f, 1.5f, 0.032f), new Vector2(0.3f, 0.34f), paper, s);
            T("Number", pivot, new Vector3(0.45f, 1.5f, 0.034f), s, n.ToString(), 0.26f, new Vector2(0.28f, 0.32f), Ink);
            P(PrimitiveType.Cylinder, "Grip", pivot, new Vector3(0.78f, 1f, 0.08f), new Vector3(0.03f, 0.08f, 0.03f), metal, true);
            // room sign to the right of the door, as the player sees it (facing local -Z, right is local -X)
            if (!hall) Qd("RoomSign", d, new Vector3(-0.8f, 1.5f, 0.055f), new Vector2(0.3f, 0.12f), paper, s);
            if (!hall) T("RoomSignText", d, new Vector3(-0.8f, 1.5f, 0.057f), s, "ROOM " + n, 0.055f, new Vector2(0.28f, 0.1f), Ink);
            if (CorridorLayout.Forbidden(n))
            {
                Qd("NoEntry", d, new Vector3(-0.8f, 1.32f, 0.055f), new Vector2(0.3f, 0.1f), M("Warning", new Color(0.75f, 0.12f, 0.08f), 0.2f), s);
                T("NoEntryText", d, new Vector3(-0.8f, 1.32f, 0.057f), s, "STAFF ONLY", 0.045f, new Vector2(0.28f, 0.08f), G(0.95f));
                // keycard reader on the handle side; InputSampler watches CardReader_n, RoomStateController lights LED_Reader<n>
                var rd = E("CardReader_" + n, d, new Vector3(0.72f, 1.2f, 0.07f)).transform;
                Cb("Body", rd, Vector3.zero, new Vector3(0.08f, 0.12f, 0.03f), M("Cam", G(0.18f), 0.3f));
                Cb("Slot", rd, new Vector3(0, -0.02f, 0.016f), new Vector3(0.06f, 0.005f, 0.004f), M("Dark", G(0.05f), 0.05f));
                Led("LED_Reader" + n, rd, new Vector3(0, 0.04f, 0.016f), new Vector3(0.015f, 0.015f, 0.004f));
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
                Speaker(r, new Vector3(cx, 2.58f, cz + 0.9f));
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
                        // Madeleine effect (brief 17.4): earlier participants' boxes already on the shelf, the free end is yours
                        for (int k = 0; k < 2; k++)
                        {
                            var old = Grabbable(Cb("PastBox", r, onBack(1.215f, 0.17f) + new Vector3(0, 0, -0.45f + 0.24f * k), Vector3.one * 0.2f, M("Box", new Color(0.62f, 0.48f, 0.3f), 0.15f), true), 0.5f);
                            T("PastBoxLabel", old.transform, new Vector3(0, 0, 0.51f), new Vector3(0, 180, 0), "041" + (5 + k), 0.12f, new Vector2(0.9f, 0.3f), Ink);
                            old.transform.rotation = Quaternion.Euler(0, yaw, 0);
                        }
                        break;
                    case 3: // forbidden: shelves of participant files
                        for (int row = 0; row < 3; row++)
                        {
                            Cb("FileShelf", r, onBack(0.5f + 0.6f * row, 0.2f), new Vector3(0.35f, 0.03f, 2.4f), wood, true);
                            for (int k = 0; k < 10; k++)
                                { var fb = Grabbable(Cb("FileBox", r, onBack(0.64f + 0.6f * row, 0.2f) + new Vector3(0, 0, -1.1f + 0.24f * k), new Vector3(0.18f, 0.25f, 0.3f), paper, true), 0.4f); fb.transform.rotation = Quaternion.Euler(0, yaw, 0); int lo = (row * 10 + k) * 14 + 1; T("FileBoxLabel", fb.transform, new Vector3(0, 0.1f, 0.51f), new Vector3(0, 180, 0), lo.ToString("0000") + "-" + Mathf.Min(lo + 13, 416).ToString("0000"), 0.09f, new Vector2(0.95f, 0.3f), Ink); }
                        }
                        T("FilesLabel", r, onBack(2.1f, 0.01f), new Vector3(0, yaw + 180f, 0), "PARTICIPANTS 0002 - 0416", 0.06f, new Vector2(2.4f, 0.12f), Ink);
                        ArchiveLore(r, cx, cz, side, yaw, paper, wood);
                        break;
                    case 4: // the screen to look at
                        var scr = Qd("Room4Screen", null, onBack(1.5f, 0.01f), new Vector2(1.4f, 0.8f),
                            M("Screen", new Color(0.02f, 0.05f, 0.04f), 0.5f, new Color(0.03f, 0.1f, 0.07f)), new Vector3(0, yaw + 180f, 0));
                        // RoomStateController rewrites it with the live tally (productive interactivity, the facility's view of you)
                        T("ScreenText", scr.transform, new Vector3(0, 0, -0.002f), Vector3.zero, "PARTICIPANT 0417\n<size=60%>PLEASE KEEP LOOKING</size>", 0.08f, new Vector2(1.3f, 0.7f), new Color(0.5f, 1f, 0.7f));
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
                            T("ObserverNoteText", ch, new Vector3(0, 0.478f, 0), new Vector3(90, 0, 0), ObserverNotes[k], 0.014f, new Vector2(0.16f, 0.22f), Ink, TextAlignmentOptions.TopLeft);
                        }
                        ObserverLore(r, cx, cz, side, yaw, wood);
                        break;
                    case 7:
                        RoomButton(r, cx, cz, 7, "I AGREE AGAIN");
                        // a desk with the stack of agreements signed before you
                        Cb("FormDesk", r, onBack(0.375f, 0.35f), new Vector3(0.6f, 0.75f, 1.0f), wood, true);
                        for (int k = 0; k < 6; k++)
                            Cb("SignedForm", r, onBack(0.755f + 0.006f * k, 0.35f) + new Vector3(0, 0, 0.01f * (k % 3)), new Vector3(0.21f, 0.004f, 0.297f), paper);
                        T("FormsLabel", r, onBack(1.4f, 0.01f), new Vector3(0, yaw + 180f, 0), "AGREEMENTS RECEIVED: 416", 0.06f, new Vector2(1.6f, 0.12f), Ink);
                        break;
                    case 8: // the last chair at the room centre, facing the door: sit (stand in its seat) to finish
                        var c8 = E("Room8Chair", r, new Vector3(cx, 0, cz), new Vector3(0, yaw, 0)).transform;
                        Cb("Seat", c8, new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.05f, 0.45f), chairM);
                        Cb("Back", c8, new Vector3(0, 0.75f, -0.21f), new Vector3(0.45f, 0.5f, 0.04f), chairM);
                        // the seat is already labelled for the next one: closure that points past the player
                        Qd("SeatTag", c8, new Vector3(0, 0.85f, -0.188f), new Vector2(0.2f, 0.08f), paper, new Vector3(0, 180, 0));
                        T("SeatTagText", c8, new Vector3(0, 0.85f, -0.186f), new Vector3(0, 180, 0), "RESERVED 0418", 0.035f, new Vector2(0.19f, 0.07f), Ink);
                        break;
                }
            }
        }

        // ---------- traces of earlier participants and corridor dressing (Madeleine effect, spatial immersion, brief 17.4) ----------
        static void MoreLore()
        {
            var paper = M("Paper", G(0.88f), 0.05f); var tape = M("Tape", new Color(0.72f, 0.66f, 0.4f), 0.05f);
            var note = M("StickyNote", new Color(0.98f, 0.9f, 0.35f), 0.05f); var wood = M("Table", new Color(0.45f, 0.36f, 0.27f), 0.2f);

            // lab: a note under the chair seat (crouch to see it) and a scratch behind the console
            var chair = GameObject.Find("Chair").transform;
            Qd("UnderSeatNote", chair, new Vector3(0, 0.424f, 0), new Vector2(0.09f, 0.09f), note, new Vector3(-90, 0, 0));
            T("UnderSeatNoteText", chair, new Vector3(0, 0.423f, 0), new Vector3(-90, 0, 0), "Don't press it.\n<size=70%>- 0203</size>", 0.014f, new Vector2(0.085f, 0.085f), Ink);
            var console = GameObject.Find("Console").transform;
            T("ConsoleScratch", console, new Vector3(-0.302f, 0.35f, 0.05f), new Vector3(0, YawE, -4), "0203 WAS HERE", 0.03f, new Vector2(0.4f, 0.06f), G(0.55f));

            float h = CorridorLayout.CorridorHalf, w = CorridorLayout.Wall, slot = CorridorLayout.SlotLen, depth = CorridorLayout.RoomDepth;
            var c = GameObject.Find("Corridor").transform;

            // floor tape steering the route: arrows at 2, 4, 7, 8 point at the door, crosses at the forbidden 3 and 6
            for (int n = CorridorLayout.First; n <= CorridorLayout.Last; n++)
            {
                int side = CorridorLayout.Side(n);
                var at = new Vector3(side * 0.3f, 0.003f, CorridorLayout.DoorZ(n));
                bool onRoute = System.Array.IndexOf(CorridorLayout.Order, n) >= 0;
                if (CorridorLayout.Forbidden(n))
                    foreach (float a in new[] { 45f, -45f })
                        Qd("FloorCross", c, at, new Vector2(0.3f, 0.04f), tape, new Vector3(90, a, 0));
                else if (onRoute)
                {
                    float yaw = side < 0 ? -90f : 90f; // shaft along X toward the door
                    Qd("FloorArrow", c, at, new Vector2(0.04f, 0.32f), tape, new Vector3(90, yaw, 0));
                    var tip = at + new Vector3(side * 0.16f, 0, 0);
                    foreach (float a in new[] { 45f, -45f })
                        Qd("FloorArrowHead", c, tip - new Vector3(side * 0.05f, 0, a > 0 ? 0.05f : -0.05f), new Vector2(0.04f, 0.15f), tape, new Vector3(90, yaw + a, 0));
                }
            }

            // corridor dressing: bench under the notices, a plant by the lab door, an extinguisher near the end
            float lx = h - 0.25f;
            Cb("BenchSeat", c, new Vector3(lx, 0.45f, -12.65f), new Vector3(0.4f, 0.05f, 1.4f), wood, true);
            foreach (float bz in new[] { -12.05f, -13.25f }) Cb("BenchLeg", c, new Vector3(lx, 0.22f, bz), new Vector3(0.36f, 0.44f, 0.05f), M("Metal", G(0.55f), 0.7f), true);
            P(PrimitiveType.Cylinder, "PlantPot", c, new Vector3(-0.6f, 0.2f, -2.35f), new Vector3(0.3f, 0.2f, 0.3f), M("Pot", new Color(0.35f, 0.3f, 0.28f), 0.2f), true);
            P(PrimitiveType.Sphere, "Plant", c, new Vector3(-0.6f, 0.62f, -2.35f), new Vector3(0.45f, 0.55f, 0.45f), M("Plant", new Color(0.18f, 0.35f, 0.2f), 0.1f));
            P(PrimitiveType.Cylinder, "Extinguisher", c, new Vector3(-(h - 0.09f), 0.45f, -13.6f), new Vector3(0.14f, 0.25f, 0.14f), M("Extinguisher", new Color(0.75f, 0.08f, 0.06f), 0.5f));
            // lost property by the hall door, out of the doorway
            var lp = Cb("LostProperty", c, new Vector3(-0.5f, 0.15f, -13.3f), new Vector3(0.45f, 0.3f, 0.35f), M("Box", new Color(0.62f, 0.48f, 0.3f), 0.15f), true);
            T("LostPropertyLabel", c, new Vector3(-0.5f, 0.18f, -13.124f), new Vector3(0, YawS, 0), "LOST PROPERTY\n<size=60%>0391: one watch, still running</size>", 0.03f, new Vector2(0.43f, 0.2f), Ink);

            // Room 5: where disagreements go
            {
                int n = 5, side = CorridorLayout.Side(n);
                float back = side * (h + w + depth), cz = CorridorLayout.DoorZ(n);
                var bin = new Vector3(back - side * 0.4f, 0, cz + 1.0f);
                P(PrimitiveType.Cylinder, "Bin", c, bin + new Vector3(0, 0.22f, 0), new Vector3(0.32f, 0.22f, 0.32f), M("Cam", G(0.18f), 0.3f), true);
                for (int k = 0; k < 5; k++)
                    Grabbable(P(PrimitiveType.Sphere, "Crumpled", c, bin + new Vector3(0.07f * Mathf.Cos(k * 1.3f), 0.46f + 0.03f * (k % 2), 0.07f * Mathf.Sin(k * 1.3f)), Vector3.one * 0.08f, paper, true), 0.02f);
                Folder(c, bin + new Vector3(-side * 0.45f, 0.006f, -0.2f), 200f, @"I disagree.
<size=70%>- 0288</size>");
                T("BinLabel", c, bin +new Vector3(0, 0.3f, -0.165f), Vector3.zero, "DISAGREEMENTS", 0.022f, new Vector2(0.3f, 0.05f), G(0.85f));
            }

            // Room 8: the one before you
            {
                int n = 8;
                float cx = CorridorLayout.CenterX(n), nz = CorridorLayout.DoorZ(n) + slot * 0.5f - w - 0.005f;
                Qd("PhotoFrame", c, new Vector3(cx, 1.6f, nz), new Vector2(0.5f, 0.62f), M("Trim", G(0.12f)));
                Qd("Photo", c, new Vector3(cx, 1.63f, nz - 0.002f), new Vector2(0.42f, 0.42f), M("Dark", G(0.05f), 0.05f));
                T("PhotoCaption", c, new Vector3(cx, 1.36f, nz - 0.003f), Vector3.zero, "PARTICIPANT OF THE MONTH\n<b>0416</b>", 0.025f, new Vector2(0.46f, 0.08f), G(0.9f));
            }

            // more readable, grabbable traces along the route (epistemic immersion: the reframes were always on file)
            {
                float h4 = CorridorLayout.CorridorHalf, w4 = CorridorLayout.Wall, d4 = CorridorLayout.RoomDepth;
                Folder(c, new Vector3(CorridorLayout.CenterX(4), 0.006f, CorridorLayout.DoorZ(4) + 0.9f), 30f, @"<b>VIEWING LOG</b>
0412: 8 s
0413: 8 s
0414: 8 s
0415: 8 s
0416: 8 s
Nobody looks away.");
                float back7 = CorridorLayout.Side(7) * (h4 + w4 + d4);
                Folder(c, new Vector3(back7 - CorridorLayout.Side(7) * 0.3f, 0.79f, CorridorLayout.DoorZ(7) - 0.25f), -75f, @"<b>0391</b>
I agree.
I agree.
I agree.
<size=70%>(asked to sign once)</size>");
                Folder(c, new Vector3(CorridorLayout.CenterX(8), 0.485f, CorridorLayout.DoorZ(8)), 90f, @"<b>FOR 0418</b>
Thank you for your cooperation.
Please remain seated.");
                var watch = Grabbable(P(PrimitiveType.Cylinder, "Watch", c, new Vector3(-0.5f, 0.31f, -13.3f), new Vector3(0.05f, 0.006f, 0.05f), M("Metal", G(0.55f), 0.7f), true), 0.05f);
                T("WatchFace", watch.transform, new Vector3(0, 1.05f, 0), new Vector3(90, 0, 0), "0391", 0.35f, new Vector2(1f, 1f), Ink);
            }
        }

        // ---------- easter eggs behind the keycard doors (lore for the player who looks; epistemic immersion, brief 17.4) ----------

        // Lore props the player can pick up, turn and read (desk E or XR grab). Kinematic until first released, then
        // GrabTracker lets them fall. GrabKind.Other is only read by Tests 1-3; these rooms are reached in Test 4.
        static GameObject Grabbable(GameObject g, float mass)
        {
            var rb = g.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.mass = mass; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            g.AddComponent<XRGrabInteractable>().movementType = XRBaseInteractable.MovementType.VelocityTracking;
            g.AddComponent<GrabTracker>().kind = GrabKind.Other;
            return g;
        }

        // A loose folder with a typed note on its cover.
        static void Folder(Transform r, Vector3 pos, float yaw, string text)
        {
            var f = Grabbable(Cb("LoreFolder", r, pos, new Vector3(0.23f, 0.012f, 0.31f), M("Folder", new Color(0.85f, 0.75f, 0.5f), 0.1f), true), 0.2f);
            f.transform.rotation = Quaternion.Euler(0, yaw, 0);
            Qd("FolderSheet", f.transform, new Vector3(0, 0.55f, 0), new Vector2(0.9f, 0.9f), M("Paper", G(0.88f), 0.05f), new Vector3(90, 0, 0));
            T("FolderText", f.transform, new Vector3(0, 0.6f, 0), new Vector3(90, 0, 0), text, 0.055f, new Vector2(0.85f, 0.85f), Ink, TextAlignmentOptions.TopLeft);
        }

        static readonly string[] ObserverNotes =
        {
            "0417\n- polite\n- responds to praise\n- watch the hands",
            "Do not mention the door.\n\nIf it asks about the clipboard, write it down.",
            "If it does nothing:\nwait.\nDo not go in.\nDo not speak to it.",
        };

        // Room 3, the archive: the facility already knows the outcome.
        static void ArchiveLore(Transform r, float cx, float cz, int side, float yaw, Material paper, Material wood)
        {
            var face = new Vector3(0, yaw, 0); // faces the door (local +Z = corridor)
            // the player's own file, pulled out and waiting on the floor
            var mine = Grabbable(Cb("File_0417", r, new Vector3(cx, 0.125f, cz + 0.6f), new Vector3(0.3f, 0.25f, 0.18f), paper, true), 0.4f);
            mine.transform.rotation = Quaternion.Euler(face);
            T("File_0417_Label", mine.transform, new Vector3(0, 0.12f, 0.51f), new Vector3(0, 180, 0),
                "<b>0417</b>\n<size=45%>PREDICTED RESULT:\nAS EXPECTED\n(filed before arrival)</size>", 0.16f, new Vector2(0.95f, 0.9f), Ink);

            // the score so far, chalked up on the north wall (faces -Z, into the room)
            float nz = cz + CorridorLayout.SlotLen * 0.5f - CorridorLayout.Wall - 0.005f;
            Qd("Whiteboard", r, new Vector3(cx, 1.45f, nz), new Vector2(1.6f, 0.9f), M("Whiteboard", G(0.95f), 0.6f));
            T("WhiteboardText", r, new Vector3(cx, 1.45f, nz - 0.003f), Vector3.zero,
                "RESULTS TO DATE\n<size=70%>AS EXPECTED  .....  411\nNONE  ..............  5</size>\n<size=55%>Condition C: under review</size>", 0.07f, new Vector2(1.5f, 0.85f), new Color(0.1f, 0.2f, 0.55f), TextAlignmentOptions.TopLeft);

            // someone refused before: participant 0203's file lies open on a desk
            var desk = new Vector3(cx + side * 0.6f, 0, cz - 0.9f);
            Cb("ArchiveDesk", r, desk + new Vector3(0, 0.375f, 0), new Vector3(0.8f, 0.75f, 0.5f), wood, true);
            Qd("File_0203", r, desk + new Vector3(0, 0.752f, 0), new Vector2(0.3f, 0.21f), paper, new Vector3(90, yaw + 180f, 0));
            T("File_0203_Text", r, desk + new Vector3(0, 0.754f, 0), new Vector3(90, yaw + 180f, 0),
                "<b>PARTICIPANT 0203</b>\nTest 3: no response (90 s)\nRESULT: NONE\nFollow-up: none possible.", 0.016f, new Vector2(0.28f, 0.2f), Ink, TextAlignmentOptions.TopLeft);
            // 0203 did not go quietly: dried blood on the file, a trail toward the door, and a scrawl low on the south wall
            // (the counter-voice to "Participation is voluntary"; named in the content note, brief 17.7)
            var blood = M("DriedBlood", new Color(0.28f, 0.02f, 0.02f), 0.35f);
            float[] dx = { -0.06f, 0.03f, 0.08f, -0.02f }, dz = { 0.04f, -0.05f, 0.02f, 0.07f };
            for (int k = 0; k < dx.Length; k++)
                P(PrimitiveType.Sphere, "BloodDrop", r, desk + new Vector3(dx[k], 0.754f, dz[k]), new Vector3(0.012f + 0.006f * (k % 2), 0.002f, 0.012f + 0.004f * k), blood);
            for (int k = 0; k < 6; k++) // floor trail from the desk toward the door (-side is the corridor)
                P(PrimitiveType.Sphere, "BloodDrop", r, desk + new Vector3(-side * (0.35f + 0.28f * k), 0.002f, 0.12f * Mathf.Sin(k * 1.7f)), new Vector3(0.02f + 0.006f * (k % 3), 0.002f, 0.018f), blood);
            float sz = cz - CorridorLayout.SlotLen * 0.5f + CorridorLayout.Wall + 0.005f; // south inner wall face, faces +Z
            T("Scrawl", r, new Vector3(cx + side * 0.6f, 0.35f, sz), new Vector3(0, 180, 6), "IT IS NOT\nVOLUNTARY", 0.07f, new Vector2(0.8f, 0.25f), new Color(0.45f, 0.04f, 0.03f));

            // loose folders: every deviation already had its reframe (the narrator's method, on paper)
            Folder(r, desk + new Vector3(-0.25f, 0.762f, 0.08f), yaw + 170f, "<b>0112</b>\nTried the door 41 times.\nRecorded as: persistence.");
            Folder(r, desk + new Vector3(0.22f, 0.762f, -0.05f), yaw + 195f, "<b>0288</b>\nThrew the cube through the window.\nWindow replaced.\nRecorded as: enthusiasm.");
            Folder(r, new Vector3(cx + side * 0.5f, 0.006f, cz + 0.2f), yaw + 120f, "<b>0391</b>\nLeft a watch.\nAsked for it back.\nRequest filed.");
            Folder(r, new Vector3(cx - side * 0.3f, 0.006f, cz - 0.3f), yaw + 80f, "<b>MEMO</b>\nNarrator warmth must not drop below 60%.\nIf it does, re-record.\n<size=70%>- Facility</size>");

            // the narrator's script on a lectern: the voice is a person reading lines
            var lect = new Vector3(cx - side * 0.9f, 0, cz - 0.9f);
            Cb("Lectern", r, lect + new Vector3(0, 0.55f, 0), new Vector3(0.35f, 1.1f, 0.35f), wood, true);
            Qd("Script", r, lect + new Vector3(0, 1.105f, 0), new Vector2(0.21f, 0.297f), paper, new Vector3(90, yaw + 180f, 0));
            T("ScriptText", r, lect + new Vector3(0, 1.107f, 0), new Vector3(90, yaw + 180f, 0),
                "<b>NARRATOR SCRIPT v12</b>\n\nR-04 <i>(do not record)</i>\n\"I did not think anyone would take it literally.\"\n\nNote: stay warm. Always.", 0.012f, new Vector2(0.19f, 0.27f), Ink, TextAlignmentOptions.TopLeft);
        }

        // Room 6, where the observers sit: the lab, live, from above.
        static void ObserverLore(Transform r, float cx, float cz, int side, float yaw, Material wood)
        {
            var desk = new Vector3(cx + side * 0.2f, 0, cz + 1.0f);
            Cb("MonitorDesk", r, desk + new Vector3(0, 0.375f, 0), new Vector3(0.9f, 0.75f, 0.5f), wood, true);
            var mon = Qd("CctvMonitor", r, desk + new Vector3(0, 1.0f, 0.05f), new Vector2(0.48f, 0.27f), M("CctvScreen", G(1f), 0.3f)); // faces -Z, into the room (desk stands at the north wall)
            mon.AddComponent<CctvFeed>();
            Cb("MonitorBody", r, desk + new Vector3(0, 1.0f, 0.075f), new Vector3(0.52f, 0.31f, 0.04f), M("Cam", G(0.18f), 0.3f));
            T("CctvLabel", r, desk + new Vector3(0, 1.19f, 0.048f), Vector3.zero, "ROOM 1  -  LIVE", 0.03f, new Vector2(0.5f, 0.05f), new Color(0.8f, 0.1f, 0.08f));
            var cup = M("Cup", G(0.9f), 0.5f);
            for (int k = 0; k < 2; k++)
                Grabbable(P(PrimitiveType.Cylinder, "Cup", r, desk + new Vector3(-0.3f + 0.55f * k, 0.8f, -0.1f), new Vector3(0.08f, 0.05f, 0.08f), cup, true), 0.15f);
            // the observers were participants once
            Folder(r, desk + new Vector3(0.05f, 0.762f, -0.12f), 10f, "<b>0416</b>\nCompleted.\nTransferred.\n<size=70%>Start date: tomorrow.</size>");
        }

        // Pedestal with a labelled cap at the room centre; a grab attempt on it = RoomButton n (same desk/XR path as the doors).
        static void RoomButton(Transform r, float x, float z, int n, string label)
        {
            Cb("Pedestal", r, new Vector3(x, 0.5f, z), new Vector3(0.4f, 1f, 0.4f), M("Console", G(0.3f), 0.25f), true);
            var b = E("RoomButton_" + n, r, new Vector3(x, 1.0f, z)).transform;
            P(PrimitiveType.Cylinder, "Cap", b, new Vector3(0, 0.015f, 0), new Vector3(0.16f, 0.015f, 0.16f), M("ConsentOrange", new Color(1f, 0.42f, 0.05f), 0.55f, Color.black), true);
            T("Label", b, new Vector3(0, 0.032f, 0), new Vector3(90, FaceCorridor(n), 0), label, 0.016f, new Vector2(0.15f, 0.05f), Ink); // reads upright from the door (playtest: +180 was upside down)
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
            Grille(sp.transform);

            Cam("CeilingCamera", new Vector3(1.6f, 2.5f, -1.6f));
            Cam("CeilingCameraB", new Vector3(-1.6f, 2.5f, 1.6f));
        }

        static void Cam(string n, Vector3 pos)
        {
            var c = E(n, null, pos).transform;
            Cb("Body", c, Vector3.zero, new Vector3(0.1f, 0.09f, 0.18f), M("Cam", G(0.18f), 0.3f));
            P(PrimitiveType.Cylinder, "Lens", c, new Vector3(0, 0, 0.1f), new Vector3(0.05f, 0.02f, 0.05f), M("Dark", G(0.05f), 0.05f), false, new Vector3(90, 0, 0));
            Led(n == "CeilingCamera" ? "LED_Camera" : n == "CeilingCameraB" ? "LED_CameraB" : "LED_CameraC", c,new Vector3(0.03f, 0.03f, 0.091f), new Vector3(0.018f, 0.018f, 0.006f)); // blinked by RoomStateController
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
