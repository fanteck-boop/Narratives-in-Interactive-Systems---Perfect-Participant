using PerfectParticipant.Core;
using UnityEngine;

namespace PerfectParticipant
{
    // A counter per WallTally chart row, one count per classified response (Intake skipped). Numbers, not marks:
    // with the corridor rooms the marks ran off the panel (playtest 2026-10-24).
    // Rows: Cooperation = Comply, Initiative = Deviate, Observation = Inaction.
    public class TallyWall : MonoBehaviour
    {
        [SerializeField] ExperimentRunner runner;
        [SerializeField] Transform wall;            // auto: "WallTally". Chart centre; marks drawn 3 cm in front (-Z, north wall)
        [SerializeField] bool makeLabels = true;    // off if the chart texture already has row names
        static readonly string[] Names = { "Cooperation", "Initiative", "Observation" };
        readonly int[] n = new int[3];
        readonly TMPro.TextMeshPro[] count = new TMPro.TextMeshPro[3];
        Transform root;
        Vector3 origin;

        void Awake()
        {
            if (!wall) wall = RoomUtil.Named("WallTally");
            origin = wall ? wall.position : new Vector3(-1.2f, 1.5f, 1.98f);
            root = new GameObject("TallyMarks").transform;
            root.SetParent(transform, false);
            if (makeLabels)
                for (int r = 0; r < 3; r++)
                {
                    var t = RoomUtil.Text("Label_" + Names[r], root, RowPos(r, -3) + new Vector3(0f, 0f, 0.02f), Quaternion.identity, 0.04f, 0.4f);
                    t.text = Names[r];
                }
            for (int r = 0; r < 3; r++)
            {
                count[r] = RoomUtil.Text("Count_" + Names[r], root, origin + new Vector3(0.22f, 0.2f - 0.2f * r, -0.01f), Quaternion.identity, 0.07f, 0.2f);
                count[r].color = new Color(0.08f, 0.08f, 0.08f);
                count[r].text = "0";
            }
        }

        void OnEnable()
        {
            if (!runner) runner = FindFirstObjectByType<ExperimentRunner>();
            if (runner) runner.CommandIssued += On;
        }

        void OnDisable() { if (runner) runner.CommandIssued -= On; }

        // Label column (makeLabels only), left of the counters.
        Vector3 RowPos(int row, int slot) => origin + new Vector3(0.05f + 0.06f * slot, 0.2f - 0.2f * row, -0.03f);

        void On(Command c)
        {
            if (c.Kind == CommandKind.Transition && c.State == GameState.Intake)
            {
                System.Array.Clear(n, 0, 3);
                foreach (var t in count) t.text = "0";
            }
            if (c.Kind != CommandKind.Classified || c.Test == TestId.Intake) return;
            int row = c.Response == ResponseKind.Comply ? 0 : c.Response == ResponseKind.Deviate ? 1 : c.Response == ResponseKind.Inaction ? 2 : -1;
            if (row < 0) return;
            count[row].text = (++n[row]).ToString();
        }
    }
}
