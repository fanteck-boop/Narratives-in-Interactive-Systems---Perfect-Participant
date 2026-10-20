using System.Collections.Generic;
using PerfectParticipant.Core;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PerfectParticipant
{
    // Consumes PlayLine / StopDialogue. Busy = true from handling until queue empty.
    // No clip = procedural murmur (PlaceholderAudio), duration 0.06 s per character.
    public class DialogueManager : MonoBehaviour
    {
        public bool Busy { get; protected set; }

        [SerializeField] ExperimentRunner runner;
        [SerializeField] DialogueLine[] lines;      // fill via menu Perfect Participant/Generate Dialogue Lines
        [SerializeField] AudioSource voice;         // auto: child NarratorVoice under SpeakerGrille
        [SerializeField] TMP_Text subtitle;         // auto: head-locked strip under Camera.main
        public bool subtitlesOn = true;

        struct Item { public string id; public bool slipped; }
        readonly Queue<Item> queue = new Queue<Item>();
        readonly Dictionary<string, DialogueLine> byId = new Dictionary<string, DialogueLine>();
        Item cur;
        bool playing;
        float left;

        // Moved by RoomStateController on NarratorToWindow.
        public Transform Voice => voice.transform;

        void Awake()
        {
            foreach (var l in lines) if (l) byId[l.id] = l;
            if (!voice)
            {
                var g = RoomUtil.Named("SpeakerGrille");
                var go = new GameObject("NarratorVoice");
                go.transform.SetParent(g ? g : transform, false);
                voice = go.AddComponent<AudioSource>();
                voice.spatialBlend = 1f;
                voice.playOnAwake = false;
                voice.minDistance = 1.5f;
            }
        }

        void OnEnable()
        {
            if (!runner) runner = FindFirstObjectByType<ExperimentRunner>();
            if (runner) runner.CommandIssued += On;
        }

        void OnDisable() { if (runner) runner.CommandIssued -= On; }

        public void Skip() { queue.Clear(); End(); } // debug panel

        static bool IsNudge(string id) => id.Contains("-N");

        void On(Command c)
        {
            if (c.Kind == CommandKind.StopDialogue)
            {
                queue.Clear();
                End();
                return;
            }
            if (c.Kind != CommandKind.PlayLine) return;
            if (IsNudge(c.Id) && Busy) return;                          // drop nudge if anything queued or playing
            queue.Enqueue(new Item { id = c.Id, slipped = c.Slipped });
            Busy = true;
            if (playing && IsNudge(cur.id) && !IsNudge(c.Id)) End(true); // reaction cuts a nudge, never the other way
        }

        void Update()
        {
            if (playing)
            {
                left -= Time.deltaTime;
                if (left > 0f) return;
                End();
            }
            if (queue.Count > 0) Play(queue.Dequeue());
            Busy = playing || queue.Count > 0;
        }

        void Play(Item it)
        {
            cur = it;
            Lines.Text.TryGetValue(it.id, out var text);
            text = text ?? it.id;
            AudioClip real = null;
            if (byId.TryGetValue(it.id, out var l)) real = (it.slipped && l.slipped) ? l.slipped : l.clean;
            var clip = real ? real : PlaceholderAudio.Murmur(it.id, text, IsNudge(it.id));
            left = real ? real.length : text.Length * 0.06f;
            voice.clip = clip;
            voice.Play();
            playing = true;
            Busy = true;
            if (subtitlesOn) Show(text);
        }

        // keepBusy: caller will start the next item this frame.
        void End(bool keepBusy = false)
        {
            playing = false;
            voice.Stop();
            if (subtitle) subtitle.text = "";
            if (!keepBusy) Busy = queue.Count > 0;
        }

        void Show(string s)
        {
            if (!subtitle)
            {
                var cam = Camera.main;
                if (!cam) return;
                // bottom strip, screen UI; above the fade so lines stay readable on black
                var r = RoomUtil.ScreenLayer("SubtitleCanvas", cam, 1.2f, 102);
                RoomUtil.Fill(r, 0.1f, 0.12f, 0.9f, 0.3f); // not lower: the headset's lower field is blurry
                var t = RoomUtil.ScreenText(r, 34f);
                t.outlineWidth = 0.25f;
                t.outlineColor = Color.black;
                subtitle = t;
            }
            subtitle.text = s;
        }

#if UNITY_EDITOR
        [ContextMenu("Fill Lines")]
        public void FillLines()
        {
            var g = AssetDatabase.FindAssets("t:DialogueLine", new[] { "Assets/_Project/Data/Dialogue" });
            lines = new DialogueLine[g.Length];
            for (int i = 0; i < g.Length; i++) lines[i] = AssetDatabase.LoadAssetAtPath<DialogueLine>(AssetDatabase.GUIDToAssetPath(g[i]));
        }

        void Reset() { runner = FindFirstObjectByType<ExperimentRunner>(); FillLines(); }
#endif
    }
}
