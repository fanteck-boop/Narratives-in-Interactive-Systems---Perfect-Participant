using System.Collections.Generic;
using PerfectParticipant.Core;
using UnityEngine;

namespace PerfectParticipant
{
    // Turns SetTier / SetComposure / Transition / RoomEvent into primitives-friendly effects.
    // Everything found by name (see ARCHITECTURE.md) or by serialized field. Missing objects are skipped, never an error.
    // Light changes are exponential approaches (tau 2.5 s, 5 s for brown-out): monotone, no flicker.
    public class RoomStateController : MonoBehaviour
    {
        [SerializeField] ExperimentRunner runner;
        [SerializeField] DialogueManager dialogue;
        [SerializeField] Light[] lights;            // auto: lights named RoomLight*, else every enabled light
        [SerializeField] AudioSource hum;           // auto: "RoomHum", else created with procedural hum
        [SerializeField] Transform doorHinge;       // auto: "DoorHinge". Else Door rotates around its local -X edge (unit cube mesh)
        [SerializeField] float doorOpenAngle = 75f; // degrees about world up; +75 swings out through the south doorway, flip if it swings into the room
        [SerializeField] float blindsHalfOpen = 0.6f; // 0 closed, 1 fully up; Test 2 shows observers at this value

        struct Preset { public Color c; public float k; public Preset(Color c, float k) { this.c = c; this.k = k; } }
        // index = Tier: Neutral, Valued (warm), Flagged (cold, bright), Unclassified (dim, grey-green)
        static readonly Preset[] Tiers =
        {
            new Preset(new Color(0.95f, 0.97f, 1f), 1f),
            new Preset(new Color(1f, 0.9f, 0.75f), 1.1f),
            new Preset(new Color(0.7f, 0.82f, 1f), 1.2f),
            new Preset(new Color(0.78f, 0.85f, 0.8f), 0.6f),
        };
        static readonly Preset Emergency = new Preset(new Color(1f, 0.2f, 0.12f), 0.15f);
        static readonly Preset Warm = new Preset(new Color(1f, 0.82f, 0.55f), 1.2f);

        Tier tier;
        bool emergency, warm, humOn, pulse, recording;
        int composure = 100;
        Color lightCol = Tiers[0].c;
        float lightK = 1f;
        float[] baseI;
        readonly List<(Transform t, Tier tier)> props = new List<(Transform, Tier)>();

        // door
        Transform door, handle;
        Vector3 doorPos, handlePos;
        Quaternion doorRot, handleRot;
        float doorAngle, doorTarget, doorSpeed;

        // blinds
        Transform blinds;
        Vector3 blindsPos, blindsScale;
        float blindsT, blindsTarget;

        // observers
        enum Obs { Hidden, Idle, Nod, Write, Lean, PensDown }
        struct Sil { public Transform t; public Vector3 pos; public Quaternion rot; public Renderer r; }
        readonly List<Sil> sils = new List<Sil>();
        Obs obs;
        float obsT, silFade;

        // LEDs
        class Led { public Material m; public Color cur, tgt; }
        readonly Dictionary<string, Led> leds = new Dictionary<string, Led>();

        // corridor room doors: Leaf_n pivots at the hinge, swing into the room
        class CDoor { public Transform leaf; public float angle, target; }
        readonly Dictionary<int, CDoor> cdoors = new Dictionary<int, CDoor>();
        const float CDoorOpen = 90f, CDoorOpenSpeed = 120f, CDoorCloseSpeed = 45f; // deg, deg/s; slow shut (playtest: felt like a trap)

        // misc props
        Transform clipboard, cube, form, window, grille, camT;
        Vector3 clipPos, cubePos, voiceLocal;
        Quaternion clipRot, cubeRot;
        bool voiceHomeSet;
        ConfigurableJoint tether;
        LineRenderer tetherLine;

        static Color Green => new Color(0.1f, 0.9f, 0.25f);
        static Color Amber => new Color(1f, 0.55f, 0.1f);
        static Color Red => new Color(0.8f, 0.05f, 0.03f);

        void Awake()
        {
            if (!runner) runner = FindFirstObjectByType<ExperimentRunner>();
            if (!dialogue) dialogue = FindFirstObjectByType<DialogueManager>();

            if (lights == null || lights.Length == 0)
            {
                var all = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                var named = new List<Light>();
                foreach (var l in all) if (l.name.StartsWith("RoomLight")) named.Add(l);
                lights = named.Count > 0 ? named.ToArray() : all;
            }
            baseI = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++) baseI[i] = lights[i].intensity;

            door = RoomUtil.Named("Door"); handle = RoomUtil.Named("DoorHandle");
            if (!doorHinge) doorHinge = RoomUtil.Named("DoorHinge");
            if (door) { doorPos = door.position; doorRot = door.rotation; }
            if (handle) { handlePos = handle.localPosition; handleRot = handle.localRotation; }

            blinds = RoomUtil.Named("WindowBlinds");
            if (blinds) { blindsPos = blinds.localPosition; blindsScale = blinds.localScale; }

            for (int i = 1; i <= 3; i++)
            {
                var t = RoomUtil.Named("Silhouette_0" + i);
                if (t) sils.Add(new Sil { t = t, pos = t.position, rot = t.rotation, r = t.GetComponentInChildren<Renderer>(true) });
            }

            clipboard = RoomUtil.Named("Clipboard");
            if (clipboard)
            {
                clipPos = clipboard.position; clipRot = clipboard.rotation;
                if (!clipboard.TryGetComponent<Rigidbody>(out var rb)) rb = clipboard.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }
            cube = RoomUtil.Named("Cube");
            if (cube) { cubePos = cube.position; cubeRot = cube.rotation; }
            form = RoomUtil.Named("ConsentForm");
            window = RoomUtil.Named("ObservationWindow");
            grille = RoomUtil.Named("SpeakerGrille");
            camT = RoomUtil.Named("CeilingCamera");
            for (int n = CorridorLayout.First; n <= CorridorLayout.Last; n++)
            {
                var l = RoomUtil.Named("Leaf_" + n);
                if (l) cdoors[n] = new CDoor { leaf = l };
            }

            foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r.name.StartsWith("LED_")) AddLed(r.name, r);
            var btn = RoomUtil.Named("ConsentButton");
            if (btn) AddLed("ConsentButton", btn.GetComponentInChildren<Renderer>(true));

            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name.StartsWith("OnlyValued_")) props.Add((t, Tier.Valued));
                else if (t.name.StartsWith("OnlyFlagged_")) props.Add((t, Tier.Flagged));
                else if (t.name.StartsWith("OnlyUnclassified_")) props.Add((t, Tier.Unclassified));
            }

            if (!hum)
            {
                var h = RoomUtil.Named("RoomHum");
                hum = h ? h.GetComponent<AudioSource>() : null;
            }
            if (!hum)
            {
                var go = new GameObject("RoomHum");
                go.transform.SetParent(grille ? grille : transform, false);
                hum = go.AddComponent<AudioSource>();
                hum.spatialBlend = 0.5f;
            }
            if (!hum.clip) hum.clip = PlaceholderAudio.Hum();
            hum.loop = true;
            hum.playOnAwake = false;
            hum.volume = 0f;
            ResetRoom();
        }

        void OnEnable()
        {
            if (!runner) runner = FindFirstObjectByType<ExperimentRunner>();
            if (runner) runner.CommandIssued += On;
        }

        void OnDisable() { if (runner) runner.CommandIssued -= On; }

        void AddLed(string key, Renderer r)
        {
            if (!r) return;
            var m = r.material;
            m.EnableKeyword("_EMISSION");
            leds[key] = new Led { m = m };
        }

        void SetLed(string key, Color c) { if (leds.TryGetValue(key, out var l)) l.tgt = c; }

        // ---------- commands ----------

        void On(Command c)
        {
            switch (c.Kind)
            {
                case CommandKind.SetTier: tier = c.Tier; ApplyProps(); break;
                case CommandKind.SetComposure: composure = c.Value; break;
                case CommandKind.Transition: Trans(c.State); break;
                case CommandKind.RoomEvent:
                    if ((c.Id == RoomEvents.DoorOpen || c.Id == RoomEvents.DoorClose) && cdoors.TryGetValue(c.Value, out var cd))
                    {
                        cd.target = c.Id == RoomEvents.DoorOpen ? CDoorOpen : 0f;
                        if (c.Id == RoomEvents.DoorOpen) SetLed("LED_Reader" + c.Value, Green); // keycard reader (3, 6) only; others have none
                        Beep(PlaceholderAudio.Thump(c.Id == RoomEvents.DoorOpen ? 80f : 55f, 0.5f), cd.leaf.position, 1f); // shut = deeper
                    }
                    else Evt(c.Id);
                    break;
                case CommandKind.Classified:
                    if (c.Test == TestId.Test1 && c.Response == ResponseKind.Comply) SetLed("LED_Tray", Green);
                    break;
            }
        }

        void Trans(GameState s)
        {
            switch (s)
            {
                case GameState.Intake:
                    ResetRoom();
                    humOn = true;
                    if (dialogue && !voiceHomeSet) { voiceLocal = dialogue.Voice.localPosition; voiceHomeSet = true; }
                    break;
                case GameState.Test1:
                    if (cube)
                    {
                        var rb = cube.GetComponent<Rigidbody>();
                        if (rb && !rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                        cube.SetPositionAndRotation(cubePos, cubeRot);
                    }
                    SetLed("LED_Tray", Amber);
                    break;
                case GameState.Test3:
                    if (form) form.gameObject.SetActive(true);
                    SetLed("ConsentButton", Amber);
                    SetLed("LED_Console", Amber);
                    break;
                case GameState.EndE1:
                    warm = true;
                    foreach (var k in new List<string>(leds.Keys)) if (!k.StartsWith("LED_Camera")) SetLed(k, Green);
                    Beep(PlaceholderAudio.Applause(), grille ? grille.position : Vector3.up * 2.5f, 0.7f);
                    Beep(PlaceholderAudio.Chime(), door ? door.position : Vector3.back * 2f, 1f); // door itself opened on DoorUnlock after Test3
                    break;
                case GameState.EndE2:
                    SetLed("LED_Clipboard", new Color(1f, 0.9f, 0.7f));
                    break;
            }
        }

        void Evt(string id)
        {
            switch (id)
            {
                case RoomEvents.ObserversIn:
                    foreach (var s in sils) s.t.gameObject.SetActive(true);
                    silFade = 0f; SetObs(Obs.Idle);
                    blindsTarget = blindsHalfOpen;
                    break;
                case RoomEvents.ObserversNod: SetObs(Obs.Nod); break;
                case RoomEvents.ObserversWriteFast: SetObs(Obs.Write); break;
                case RoomEvents.ObserversLeanIn: SetObs(Obs.Lean); break;
                case RoomEvents.ObserversPensDown: SetObs(Obs.PensDown); break;
                case RoomEvents.BrownOut: emergency = true; break;
                case RoomEvents.HumStop: humOn = false; break;
                case RoomEvents.RecorderClick:
                    Beep(PlaceholderAudio.Click(), camT ? camT.position : Vector3.up * 2.5f, 1f);
                    recording = false; // the recorder stops: both camera lights go dark
                    SetLed("LED_Camera", Color.black); SetLed("LED_CameraB", Color.black);
                    break;
                case RoomEvents.BlindsUp: blindsTarget = 1f; break;
                case RoomEvents.ClipboardFall: Fall(); break;
                case RoomEvents.NarratorToWindow:
                    if (dialogue && window) dialogue.Voice.position = window.position; // glass centre
                    break;
                case RoomEvents.ButtonPulse: pulse = true; break;
                case RoomEvents.DoorUnlockHeavy: pulse = false; SetLed("ConsentButton", Amber); Unlock(true); break;
                case RoomEvents.DoorUnlock: Unlock(false); break; // consent given: the room door opens onto the corridor
            }
        }

        // ---------- effects ----------

        public void Rehome() // SeatedMode moved the cube
        {
            if (!cube) return;
            cubePos = cube.position; cubeRot = cube.rotation;
            if (tether) tether.connectedAnchor = TetherAnchor;
        }

        void ResetRoom()
        {
            tier = Tier.Neutral; emergency = warm = humOn = pulse = false;
            composure = 100;
            ApplyProps();
            if (door) { door.SetPositionAndRotation(doorPos, doorRot); }
            if (handle) { handle.localPosition = handlePos; handle.localRotation = handleRot; }
            doorAngle = doorTarget = 0f;
            blindsTarget = blindsT = 0f;
            foreach (var s in sils)
            {
                s.t.SetPositionAndRotation(s.pos, s.rot);
                s.t.gameObject.SetActive(false);
            }
            obs = Obs.Hidden;
            if (clipboard)
            {
                var rb = clipboard.GetComponent<Rigidbody>();
                rb.isKinematic = false; // velocity setters warn on kinematic bodies
                rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                clipboard.SetPositionAndRotation(clipPos, clipRot);
            }
            if (form) form.gameObject.SetActive(false);
            if (voiceHomeSet && dialogue) dialogue.Voice.localPosition = voiceLocal;
            foreach (var l in leds.Values) l.tgt = Color.black;
            SetLed("LED_Door", Red);
            SetLed("LED_Reader3", Red); SetLed("LED_Reader6", Red);
            recording = true;
        }

        void ApplyProps()
        {
            foreach (var p in props) p.t.gameObject.SetActive(p.tier == tier);
            Tether(tier == Tier.Flagged);
        }

        // Flagged prop (brief): steel tether from the table to the cube. Joint keeps a thrown cube within reach.
        const float TetherLength = 0.7f;
        Vector3 TetherAnchor => cubePos + Vector3.down * 0.04f; // table top under the cube's home spot

        void Tether(bool on)
        {
            if (!cube) return;
            if (on && !tether && cube.TryGetComponent<Rigidbody>(out _))
            {
                tether = cube.gameObject.AddComponent<ConfigurableJoint>();
                tether.autoConfigureConnectedAnchor = false;
                tether.anchor = Vector3.zero;
                tether.connectedAnchor = TetherAnchor; // world space: no connected body
                tether.xMotion = tether.yMotion = tether.zMotion = ConfigurableJointMotion.Limited;
                tether.linearLimit = new SoftJointLimit { limit = TetherLength };
            }
            else if (!on && tether) { Destroy(tether); tether = null; }

            if (on && !tetherLine)
            {
                tetherLine = new GameObject("CubeTether").AddComponent<LineRenderer>();
                tetherLine.positionCount = 2;
                tetherLine.widthMultiplier = 0.005f;
                tetherLine.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.55f, 0.57f, 0.6f) }; // ponytail: unlit grey, steel material later
                tetherLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (tetherLine) tetherLine.enabled = on;
        }

        void SetObs(Obs o) { obs = o; obsT = 0f; }

        static void Beep(AudioClip c, Vector3 p, float v) => AudioSource.PlayClipAtPoint(c, p, v);

        void Unlock(bool heavy)
        {
            if (door) Beep(PlaceholderAudio.Thump(heavy ? 45f : 80f, heavy ? 0.9f : 0.4f), door.position, 1f);
            SetLed("LED_Door", Green);
            if (handle) handle.localRotation = handleRot * Quaternion.Euler(0f, 0f, -25f);
            doorTarget = doorOpenAngle;
            doorSpeed = heavy ? 12f : 25f; // deg/s
        }

        void Fall()
        {
            if (!clipboard) return;
            var rb = clipboard.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.AddForce(Vector3.right * 0.6f, ForceMode.Impulse); // nudge off the shelf, west wall = player's left
            if (!clipboard.GetComponent<ClipboardThud>()) clipboard.gameObject.AddComponent<ClipboardThud>();
        }

        // ---------- per frame ----------

        void Update()
        {
            float dt = Time.deltaTime;

            var tp = emergency ? Emergency : warm ? Warm : Tiers[(int)tier];
            float a = 1f - Mathf.Exp(-dt / (emergency ? 5f : 2.5f));
            lightCol = Color.Lerp(lightCol, tp.c, a);
            lightK = Mathf.Lerp(lightK, tp.k, a);
            for (int i = 0; i < lights.Length; i++)
            {
                if (!lights[i]) continue;
                lights[i].color = lightCol;
                lights[i].intensity = baseI[i] * lightK;
            }

            // hum: pitch sags with composure, winds down on HumStop
            if (humOn && !hum.isPlaying) hum.Play();
            float ha = 1f - Mathf.Exp(-dt / 2f);
            hum.volume = Mathf.Lerp(hum.volume, humOn ? 0.15f : 0f, ha);
            hum.pitch = Mathf.Lerp(hum.pitch, humOn ? Mathf.Lerp(0.75f, 1f, composure / 100f) : 0.25f, ha);
            if (!humOn && hum.isPlaying && hum.volume < 0.005f) hum.Stop();

            if (blinds)
            {
                blindsT = Mathf.MoveTowards(blindsT, blindsTarget, dt / 5f);
                float sy = blindsScale.y * Mathf.Lerp(1f, 0.08f, blindsT); // top edge fixed, bottom edge rises
                blinds.localScale = new Vector3(blindsScale.x, sy, blindsScale.z);
                blinds.localPosition = blindsPos + Vector3.up * ((blindsScale.y - sy) * 0.5f);
            }

            if (door && doorAngle != doorTarget)
            {
                float next = Mathf.MoveTowards(doorAngle, doorTarget, doorSpeed * dt);
                var pivot = doorHinge ? doorHinge.position : door.TransformPoint(-0.5f, 0f, 0f);
                door.RotateAround(pivot, Vector3.up, next - doorAngle);
                doorAngle = next;
            }

            if (recording) // slow red recording blink, 0.5 Hz (brief: nothing faster than 1 Hz); B is offset so they never sync
                for (int i = 0; i < 2; i++)
                    if (leds.TryGetValue(i == 0 ? "LED_Camera" : "LED_CameraB", out var cl))
                        cl.tgt = Mathf.Repeat(Time.time + i * 0.7f, 2f) < 0.6f ? Red * 1.6f : Red * 0.05f;
            if (pulse && leds.TryGetValue("ConsentButton", out var pb)) pb.tgt = Amber * (0.6f + 0.4f * Mathf.Sin(Time.time * Mathf.PI)); // 0.5 Hz
            float la = 1f - Mathf.Exp(-dt / 0.5f);
            foreach (var l in leds.Values)
            {
                l.cur = Color.Lerp(l.cur, l.tgt, la);
                l.m.SetColor("_EmissionColor", l.cur * 2.5f);
            }

            if (tetherLine && tetherLine.enabled) { tetherLine.SetPosition(0, TetherAnchor); tetherLine.SetPosition(1, cube.position); }

            foreach (var cd in cdoors.Values)
                if (cd.angle != cd.target)
                {
                    cd.angle = Mathf.MoveTowards(cd.angle, cd.target, (cd.target > cd.angle ? CDoorOpenSpeed : CDoorCloseSpeed) * dt);
                    cd.leaf.localRotation = Quaternion.Euler(0f, cd.angle, 0f);
                }

            UpdateObservers(dt);
        }

        void UpdateObservers(float dt)
        {
            if (obs == Obs.Hidden) return;
            obsT += dt;
            silFade = Mathf.MoveTowards(silFade, 1f, dt / 3f);
            if ((obs == Obs.Nod || obs == Obs.Write || obs == Obs.Lean) && obsT > 5f) SetObs(Obs.Idle);
            float k = 1f - Mathf.Exp(-dt * 6f);
            for (int i = 0; i < sils.Count; i++)
            {
                var s = sils[i];
                float ph = i * 1.7f, tilt = 0f, lean = 0f, rock = Mathf.Sin(Time.time * 0.6f + ph) * 1.5f;
                switch (obs)
                {
                    case Obs.Nod: tilt = 18f * Mathf.Max(0f, Mathf.Sin(obsT * 2.5f + ph * 0.2f)); break;
                    case Obs.Write: tilt = 8f + 3f * Mathf.Sin(Time.time * 12f + ph); break;
                    case Obs.Lean: tilt = 14f; lean = -0.25f; break;      // toward window = -Z
                    case Obs.PensDown: rock = 0f; break;
                }
                var tr = s.rot * Quaternion.Euler(-tilt, 0f, rock);       // -X pitch tips the head toward -Z
                var tpos = s.pos + Vector3.forward * lean;
                s.t.SetPositionAndRotation(Vector3.Lerp(s.t.position, tpos, k), Quaternion.Slerp(s.t.rotation, tr, k));
                if (s.r) s.r.material.color = Color.Lerp(Color.black, new Color(0.14f, 0.14f, 0.15f), silFade);
            }
        }
    }

    // Plays one thud on first impact, then removes itself.
    class ClipboardThud : MonoBehaviour
    {
        void OnCollisionEnter(Collision c)
        {
            AudioSource.PlayClipAtPoint(PlaceholderAudio.Thump(70f, 0.5f), transform.position, 1f);
            Destroy(this);
        }
    }
}
