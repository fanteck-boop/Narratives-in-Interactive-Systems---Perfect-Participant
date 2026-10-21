using PerfectParticipant.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace PerfectParticipant
{
    // Content note before start, result card, end-of-session fade.
    // Content note keeps ExperimentRunner disabled until trigger / Space / Enter / StartExperiment().
    // ShowCard: RESULT: NONE -> fade to black, text on black (E3). Else text on the Door, black after endHold.
    public class CardDisplay : MonoBehaviour
    {
        [SerializeField] ExperimentRunner runner;
        [SerializeField] TMP_Text title, sub;       // auto: "CardTitle" / "CardSubtitle", else created on the Door's inside face
        [SerializeField] bool showContentNote = true;
        [SerializeField, TextArea(3, 8)] string note =
            "Content note\n\nThis experience contains spoken narration and sudden sounds. It takes about ten minutes. " +
            "You may remove the headset at any time.\n\nPress the trigger to begin.\nSeated? Press A or X (S on keyboard).";
        [SerializeField] float endHold = 12f, fadeSeconds = 3f;

        TMP_Text blackText;
        Image fade;
        float alpha, target, endAt;
        bool waiting, ended, blackCard;
        string pendingBlack;
        static readonly string[] Seat = { "<XRController>{RightHand}/primaryButton", "<XRController>{LeftHand}/primaryButton" };
        static readonly string[] Triggers = { "<XRController>{RightHand}/triggerPressed", "<XRController>{LeftHand}/triggerPressed" };

        void Awake()
        {
            if (!runner) runner = FindFirstObjectByType<ExperimentRunner>();
            if (!title) title = RoomUtil.Named("CardTitle")?.GetComponent<TMP_Text>();
            if (!sub) sub = RoomUtil.Named("CardSubtitle")?.GetComponent<TMP_Text>();
            if (!title || !sub)
            {
                var d = RoomUtil.Named("Door");
                float z = (d ? d.position.z : -2f) + 0.06f, x = d ? d.position.x : 0f;
                var back = Quaternion.Euler(0f, 180f, 0f); // door is south, text must face north
                if (!title) title = RoomUtil.Text("CardTitle", null, new Vector3(x, 1.7f, z), back, 0.07f, 0.8f);
                if (!sub) sub = RoomUtil.Text("CardSubtitle", null, new Vector3(x, 1.55f, z), back, 0.04f, 0.8f);
            }
            title.text = "";
            sub.text = "";
            if (showContentNote && runner)
            {
                waiting = true;
                runner.enabled = false;
                alpha = target = 1f;
                pendingBlack = Note();
            }
        }

        void OnEnable()
        {
            if (!runner) runner = FindFirstObjectByType<ExperimentRunner>();
            if (runner) runner.CommandIssued += On;
        }

        void OnDisable() { if (runner) runner.CommandIssued -= On; }

        string Note() => note + "\nSeated mode: " + (SeatedMode.Seated ? "on" : "off");

        void SeatPressed()
        {
            var k = Keyboard.current;
            bool hit = k != null && k.sKey.wasPressedThisFrame;
            foreach (var p in Seat) if (InputSystem.FindControl(p) is ButtonControl b && b.wasPressedThisFrame) hit = true;
            var s = hit ? FindFirstObjectByType<SeatedMode>() : null;
            if (!s) return;
            s.Toggle();
            if (blackText) blackText.text = Note();
        }

        public void StartExperiment()
        {
            if (!waiting) return;
            waiting = false;
            target = 0f;
            pendingBlack = null;
            if (blackText) blackText.text = "";
            runner.enabled = true;
        }

        void On(Command c)
        {
            if (c.Kind == CommandKind.Transition && c.State == GameState.Intake)
            {
                ended = false;
                blackCard = false;
                title.text = "";
                sub.text = "";
                if (!waiting) target = 0f;
            }
            else if (c.Kind == CommandKind.ShowCard)
            {
                // every ending now happens in the corridor, back to the room door: all cards go on the black screen
                blackCard = true; target = 1f; pendingBlack = c.Id + (string.IsNullOrEmpty(c.Text) ? "" : "\n<size=60%>" + c.Text + "</size>");
            }
        }

        // Full-screen UI (RoomUtil.ScreenLayer). Headset: fade just past the near plane covers everything, text at 1 m on top.
        void Rig()
        {
            var cam = Camera.main;
            if (!cam || fade) return;
            var f = RoomUtil.ScreenLayer("FadeCanvas", cam, 0.1f, 100);
            RoomUtil.Fill(f, 0f, 0f, 1f, 1f);
            fade = f.gameObject.AddComponent<Image>();
            fade.raycastTarget = false;
            var t = RoomUtil.ScreenLayer("BlackTextCanvas", cam, 1f, 101);
            RoomUtil.Fill(t, 0.15f, 0.15f, 0.85f, 0.85f);
            blackText = RoomUtil.ScreenText(t, 40f);
            blackText.text = "";
        }

        static bool StartPressed()
        {
            var k = Keyboard.current;
            if (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) return true;
            foreach (var p in Triggers)
                if (InputSystem.FindControl(p) is ButtonControl b && b.wasPressedThisFrame) return true;
            return false;
        }

        void Update()
        {
            Rig();
            if (!fade) return;
            if (waiting) SeatPressed();
            if (waiting && StartPressed()) StartExperiment();

            alpha = Mathf.MoveTowards(alpha, target, Time.deltaTime / Mathf.Max(0.1f, fadeSeconds));
            fade.color = new Color(0f, 0f, 0f, alpha);
            fade.enabled = alpha > 0.001f;
            if (pendingBlack != null && alpha >= 0.99f) { blackText.text = pendingBlack; pendingBlack = null; }

            if (runner && !ended && !waiting && runner.Flow.Finished) { ended = true; endAt = Time.time + endHold; }
            if (ended && !blackCard && Time.time >= endAt) target = 1f;
        }
    }
}
