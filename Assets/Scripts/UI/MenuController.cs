using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TiltMirror
{
    // Switches between the Home screen, the Levels screen and the game itself.
    // Everything here is wired up automatically by Tilt Mirror > Build Menu UI.
    public sealed class MenuController : MonoBehaviour
    {
        [SerializeField] GameController controller;

        [SerializeField] GameObject homeScreen;
        [SerializeField] GameObject levelsScreen;
        [SerializeField] GameObject gameScreen;

        [SerializeField] RectTransform levelsContent;   // grid parent inside the scroll view
        [SerializeField] TMP_Text playLabel;            // "LEVEL 5" on the PLAY button
        [SerializeField] TMP_Text totalStarsText;       // "28 / 90" on the Levels screen
        [SerializeField] Image soundIcon;

        [SerializeField] Sprite soundOnSprite;
        [SerializeField] Sprite soundOffSprite;
        [SerializeField] Sprite roundRect;
        [SerializeField] Sprite starSprite;
        [SerializeField] Sprite lockSprite;

        static readonly Color Gold = new Color32(0xFF, 0xB0, 0x20, 255);
        static readonly Color StarOff = new Color(1f, 1f, 1f, 0.25f);

        void Awake()
        {
            // If the slot was lost (script replaced, scene rebuilt), find the GameController by itself.
            if (controller == null) controller = FindFirstObjectByType<GameController>();
            if (controller == null)
                Debug.LogError("MenuController: sahnada GameController topilmadi. GameController obyektida skript 'Missing' bo'lib qolmaganini tekshiring.");
        }

        void Start()
        {
            if (controller == null) return;
            if (controller != null) controller.LevelsRequested += OnLevelsRequested;
            ShowHome();
        }

        void OnDestroy()
        {
            if (controller != null) controller.LevelsRequested -= OnLevelsRequested;
        }

        // ---- screens ----

        public void ShowHome()
        {
            controller.CloseLevel();
            SetScreens(true, false, false);
            RefreshHome();
        }

        public void ShowLevels()
        {
            controller.CloseLevel();
            SetScreens(false, true, false);
            RebuildLevelGrid();
        }

        // Continue: opens the first level that has not been completed yet.
        public void PlayCurrent()
        {
            Click();
            int count = controller.LevelCount;
            int index = Mathf.Clamp(ProgressService.HighestUnlocked(count), 0, Mathf.Max(0, count - 1));
            StartLevel(index);
        }

        public void ShowLevelsFromButton()
        {
            Click();
            ShowLevels();
        }

        public void ShowHomeFromButton()
        {
            Click();
            ShowHome();
        }

        public void ToggleSound()
        {
            AudioManager sound = AudioManager.Ensure();
            sound.Muted = !sound.Muted;
            UpdateSoundIcon();
            if (!sound.Muted) sound.Play("sfx_click");
        }

        void StartLevel(int index)
        {
            SetScreens(false, false, true);
            controller.OpenLevel(index);
        }

        void OnLevelsRequested()
        {
            ShowLevels();
        }

        void SetScreens(bool home, bool levels, bool game)
        {
            if (homeScreen != null) homeScreen.SetActive(home);
            if (levelsScreen != null) levelsScreen.SetActive(levels);
            if (gameScreen != null) gameScreen.SetActive(game);
        }

        // ---- home ----

        void RefreshHome()
        {
            int count = controller.LevelCount;
            int index = Mathf.Clamp(ProgressService.HighestUnlocked(count), 0, Mathf.Max(0, count - 1));
            if (playLabel != null) playLabel.text = "LEVEL " + (index + 1);
            UpdateSoundIcon();
        }

        void UpdateSoundIcon()
        {
            if (soundIcon == null) return;
            bool muted = AudioManager.Ensure().Muted;
            Sprite s = muted ? soundOffSprite : soundOnSprite;
            if (s != null) soundIcon.sprite = s;
        }

        // ---- level list ----

        void RebuildLevelGrid()
        {
            if (levelsContent == null) return;

            for (int i = levelsContent.childCount - 1; i >= 0; i--)
                Destroy(levelsContent.GetChild(i).gameObject);

            int count = controller.LevelCount;
            int unlocked = ProgressService.HighestUnlocked(count);
            for (int i = 0; i < count; i++) MakeLevelButton(i, i > unlocked);

            if (totalStarsText != null)
                totalStarsText.text = ProgressService.TotalStars(count) + " / " + (count * 3);

            levelsContent.anchoredPosition = Vector2.zero;
        }

        void MakeLevelButton(int index, bool locked)
        {
            int stars = ProgressService.GetStars(index);
            bool done = stars > 0;

            GameObject go = new GameObject("Level_" + (index + 1), typeof(RectTransform));
            go.transform.SetParent(levelsContent, false);

            Image bg = go.AddComponent<Image>();
            bg.sprite = roundRect;
            bg.type = Image.Type.Sliced;
            if (locked) bg.color = new Color(1f, 1f, 1f, 0.06f);
            else if (done) bg.color = new Color(1f, 0.69f, 0.125f, 0.22f);
            else bg.color = new Color(1f, 1f, 1f, 0.16f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            button.interactable = !locked;

            if (locked)
            {
                AddImage(go.transform, "Lock", lockSprite, new Color(1f, 1f, 1f, 0.45f), Vector2.zero, new Vector2(64f, 64f));
                return;
            }

            AddText(go.transform, "Number", (index + 1).ToString(), 64f, Color.white, new Vector2(0f, 22f), new Vector2(180f, 90f));
            for (int s = 0; s < 3; s++)
            {
                AddImage(go.transform, "Star" + (s + 1), starSprite, s < stars ? Gold : StarOff,
                    new Vector2((s - 1) * 44f, -58f), new Vector2(38f, 38f));
            }

            int captured = index;
            button.onClick.AddListener(delegate
            {
                Click();
                StartLevel(captured);
            });
        }

        static void AddImage(Transform parent, string name, Sprite sprite, Color color, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = color;
            img.raycastTarget = false;
        }

        static void AddText(Transform parent, string name, string text, float size, Color color, Vector2 pos, Vector2 box)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
        }

        static void Click()
        {
            AudioManager.Ensure().Play("sfx_click");
        }
    }
}
