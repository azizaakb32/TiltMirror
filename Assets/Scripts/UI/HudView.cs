using TMPro;
using UnityEngine;

namespace TiltMirror
{
    // Requires TextMeshPro (Window > TextMeshPro > Import TMP Essential Resources).
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text movesText;
        [SerializeField] TMP_Text parText;

        void Awake()
        {
            // "PAR" is hard to understand, so the label reads "BEST": the fewest possible moves.
            RenameLabel(transform, "ParLabel", "BEST");
        }

        public void Set(int level, int moves, int par)
        {
            if (levelText != null) levelText.text = "LEVEL " + level;
            if (movesText != null) movesText.text = moves.ToString();
            if (parText != null) parText.text = par.ToString();
        }

        public static void RenameLabel(Transform root, string labelName, string text)
        {
            if (root == null) return;
            TMP_Text[] all = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == labelName) all[i].text = text;
        }
    }
}
