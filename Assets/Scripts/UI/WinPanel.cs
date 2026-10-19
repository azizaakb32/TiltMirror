using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TiltMirror
{
    public sealed class WinPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Image[] stars;
        [SerializeField] TMP_Text movesText;
        [SerializeField] TMP_Text parText;
        [SerializeField] Color starOn = new Color32(0xFF, 0xB0, 0x20, 255);
        [SerializeField] Color starOff = new Color(0.1f, 0.11f, 0.3f, 0.18f);

        public void Show(int starCount, int moves, int par)
        {
            for (int i = 0; i < stars.Length; i++)
                stars[i].color = i < starCount ? starOn : starOff;
            if (movesText != null) movesText.text = moves.ToString();
            if (parText != null) parText.text = par.ToString();
            HudView.RenameLabel(root.transform, "ParLabel", "BEST");
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }
}
