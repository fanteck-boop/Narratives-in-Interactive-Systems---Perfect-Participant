using PerfectParticipant.Core;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PerfectParticipant
{
    // One asset per Lines.Text id: Data/Dialogue/<id>.asset. Text stays in Core. Empty clips = placeholder audio.
    [CreateAssetMenu(menuName = "PerfectParticipant/DialogueLine")]
    public class DialogueLine : ScriptableObject
    {
        public string id;
        public AudioClip clean, slipped;
        public string Text => Lines.Text.TryGetValue(id, out var t) ? t : id;
    }

#if UNITY_EDITOR
    public static class DialogueLineTools
    {
        const string Dir = "Assets/_Project/Data/Dialogue", Vo = "Assets/_Project/Audio/VO";

        // Idempotent: creates missing assets, (re)links VO clips if <id>_clean.wav / _slipped.wav exist, refills DialogueManager.
        [MenuItem("Perfect Participant/Generate Dialogue Lines")]
        public static void Generate()
        {
            foreach (var id in Lines.Text.Keys)
            {
                var path = $"{Dir}/{id}.asset";
                var l = AssetDatabase.LoadAssetAtPath<DialogueLine>(path);
                if (!l) { l = ScriptableObject.CreateInstance<DialogueLine>(); AssetDatabase.CreateAsset(l, path); }
                l.id = id;
                l.clean = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Vo}/{id}_clean.wav");
                l.slipped = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Vo}/{id}_slipped.wav");
                EditorUtility.SetDirty(l);
            }
            AssetDatabase.SaveAssets();
            foreach (var dm in Object.FindObjectsByType<DialogueManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            { dm.FillLines(); EditorUtility.SetDirty(dm); }
            Debug.Log("Dialogue lines generated: " + Lines.Text.Count);
        }
    }
#endif
}
