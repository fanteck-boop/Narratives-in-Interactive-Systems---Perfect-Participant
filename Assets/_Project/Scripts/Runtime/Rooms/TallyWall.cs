using PerfectParticipant.Core;
using UnityEngine;

namespace PerfectParticipant
{
    // One small dark cube per classified response (Intake skipped) on the WallTally chart rows.
    // Rows: Cooperation = Comply, Initiative = Deviate, Observation = Inaction.
    public class TallyWall : MonoBehaviour
    {
        [SerializeField] ExperimentRunner runner;
        [SerializeField] Transform wall;            // auto: "WallTally". Chart centre; marks drawn 3 cm in front (-Z, north wall)
        [SerializeField] bool makeLabels = true;    // off if the chart texture already has row names
        static readonly string[] Names = { "Cooperation", "Initiative", "Observation" };
        readonly int[] n = new int[3];
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
        }

        void OnEnable()
        {
            if (!runner) runner = FindFirstObjectByType<ExperimentRunner>();
            if (runner) runner.CommandIssued += On;
        }

        void OnDisable() { if (runner) runner.CommandIssued -= On; }

        // Marks start right of the row words (builder labels run x -0.33..about -0.09), 0.06 apart; panel edge is +0.35.
        // Slot negative = label column (makeLabels only).
        Vector3 RowPos(int row, int slot) => origin + new Vector3(0.05f + 0.06f * slot, 0.2f - 0.2f * row, -0.03f);

        void On(Command c)
        {
            if (c.Kind == CommandKind.Transition && c.State == GameState.Intake)
            {
                System.Array.Clear(n, 0, 3);
                for (int i = root.childCount - 1; i >= 0; i--)
                    if (root.GetChild(i).name == "Mark") Destroy(root.GetChild(i).gameObject);
            }
            if (c.Kind != CommandKind.Classified || c.Test == TestId.Intake) return;
            int row = c.Response == ResponseKind.Comply ? 0 : c.Response == ResponseKind.Deviate ? 1 : c.Response == ResponseKind.Inaction ? 2 : -1;
            if (row < 0) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Mark";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(root, false);
            go.transform.position = RowPos(row, n[row]++);
            go.transform.localScale = new Vector3(0.02f, 0.12f, 0.01f);
            go.GetComponent<Renderer>().material.color = new Color(0.08f, 0.08f, 0.08f);
        }
    }
}
