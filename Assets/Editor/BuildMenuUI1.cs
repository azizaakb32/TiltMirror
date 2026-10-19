using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TiltMirror.EditorTools
{
    // Menu: Tilt Mirror > Build Menu UI
    // Run it AFTER Build Demo UI. It adds the Home screen and the Levels screen, moves the
    // in-game UI into a "GameScreen" group, adds a Back button to the HUD and connects everything.
    // Safe to run again: it replaces its own old screens.
    public static class BuildMenuUI
    {
        const string UiPath = "Assets/Resources/TiltMirror/UI/";
        const string CanvasName = "TiltMirror Canvas";

        static readonly Color Navy = new Color32(0x1A, 0x1C, 0x4D, 255);
        static readonly Color DeepNavy = new Color32(0x0B, 0x0C, 0x24, 255);
        static readonly Color Peach = new Color32(0xFF, 0xD9, 0xBF, 255);
        static readonly Color Brown = new Color32(0x7A, 0x2E, 0x1F, 255);
        static readonly Color Gold = new Color32(0xFF, 0xB0, 0x20, 255);
        static readonly Color Mint = new Color32(0xBF, 0xF3, 0xEE, 255);
        static readonly Color Glass = new Color(1f, 1f, 1f, 0.10f);

        static GameController FindController()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<GameController>();
#else
            return Object.FindObjectOfType<GameController>();
#endif
        }

        [MenuItem("Tilt Mirror/Build Menu UI")]
        static void Build()
        {
            GameObject canvasGo = GameObject.Find(CanvasName);
            GameController controller = FindController();
            if (canvasGo == null || controller == null)
            {
                EditorUtility.DisplayDialog("Tilt Mirror",
                    "Avval o'yin UI'sini yarating:\nTilt Mirror > Build Demo UI\n\nKeyin bu buyruqni qayta bosing.", "OK");
                return;
            }

            AssetDatabase.ImportAsset("Assets/Resources/TiltMirror",
                ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            Sprite round = LoadSprite("ui_round_rect");
            Sprite circle = LoadSprite("ui_circle");
            Sprite star = LoadSprite("ui_star");
            Sprite lockIcon = LoadSprite("ui_lock");
            Sprite back = LoadSprite("ui_back");
            Sprite soundOn = LoadSprite("ui_sound_on");
            Sprite soundOff = LoadSprite("ui_sound_off");
            Sprite levelsIcon = LoadSprite("ui_levels");
            Sprite logo = LoadSprite("logo_disc");
            if (round == null || circle == null || star == null || back == null || lockIcon == null
                || soundOn == null || soundOff == null || levelsIcon == null || logo == null)
            {
                EditorUtility.DisplayDialog("Tilt Mirror",
                    "UI rasmlaridan ba'zilari Sprite sifatida topilmadi.\n\nAssets/Resources/TiltMirror papkasi ustiga o'ng tugma > Reimport bosing va qayta urinib ko'ring.", "OK");
                return;
            }

            Transform root = canvasGo.transform;
            Vector2 top = new Vector2(0.5f, 1f);
            Vector2 mid = new Vector2(0.5f, 0.5f);

            // ---------- GameScreen: groups the in-game UI so menus can hide it ----------
            GameObject gameScreen;
            Transform existing = root.Find("GameScreen");
            if (existing == null)
            {
                gameScreen = new GameObject("GameScreen", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(gameScreen, "Build Menu UI");
                gameScreen.transform.SetParent(root, false);
                Stretch(gameScreen);
                gameScreen.transform.SetSiblingIndex(0);
            }
            else
            {
                gameScreen = existing.gameObject;
            }

            string[] parts = new string[] { "SwipeLayer", "Hud", "DPad", "WinPanel" };
            for (int i = 0; i < parts.Length; i++)
            {
                Transform t = root.Find(parts[i]);
                if (t != null) t.SetParent(gameScreen.transform, false);
            }

            // ---------- Back button in the HUD ----------
            Transform hud = gameScreen.transform.Find("Hud");
            if (hud != null)
            {
                Transform oldBack = hud.Find("BackButton");
                if (oldBack != null) Undo.DestroyObjectImmediate(oldBack.gameObject);

                Button backBtn = MakeButton(hud, "BackButton", circle, Glass, top, new Vector2(-440f, -130f), new Vector2(120f, 120f));
                MakeIcon(backBtn.transform, "Icon", back, Color.white, mid, Vector2.zero, new Vector2(64f, 64f));
                OnClick(backBtn, new UnityAction(controller.BackToLevels));

                // Undo moves next to restart to make room for Back.
                Transform undo = hud.Find("UndoButton");
                if (undo != null) ((RectTransform)undo).anchoredPosition = new Vector2(300f, -130f);
            }

            // ---------- MenuController ----------
            MenuController menu = canvasGo.GetComponent<MenuController>();
            if (menu == null) menu = canvasGo.AddComponent<MenuController>();

            Transform oldHome = root.Find("HomeScreen");
            if (oldHome != null) Undo.DestroyObjectImmediate(oldHome.gameObject);
            Transform oldLevels = root.Find("LevelsScreen");
            if (oldLevels != null) Undo.DestroyObjectImmediate(oldLevels.gameObject);

            // ---------- Home screen ----------
            GameObject home = new GameObject("HomeScreen", typeof(RectTransform));
            home.transform.SetParent(root, false);
            Stretch(home);
            Undo.RegisterCreatedObjectUndo(home, "Build Menu UI");

            MakeHalf(home.transform, "SunSide", Peach, 0f, 0.5f);
            MakeHalf(home.transform, "MoonSide", Navy, 0.5f, 1f);

            GameObject seam = new GameObject("Seam", typeof(RectTransform));
            seam.transform.SetParent(home.transform, false);
            RectTransform seamRt = (RectTransform)seam.transform;
            seamRt.anchorMin = new Vector2(0.5f, 0f);
            seamRt.anchorMax = new Vector2(0.5f, 1f);
            seamRt.pivot = new Vector2(0.5f, 0.5f);
            seamRt.anchoredPosition = Vector2.zero;
            seamRt.sizeDelta = new Vector2(8f, 0f);
            Image seamImg = seam.AddComponent<Image>();
            seamImg.color = Color.white;
            seamImg.raycastTarget = false;

            MakeIcon(home.transform, "Logo", logo, Color.white, mid, new Vector2(0f, 400f), new Vector2(440f, 440f));

            // Title is split at the seam: "TILT" on the left half, "MIRROR" on the right half.
            // Size 92 keeps "MIRROR" on one line inside the 540 px half-width.
            MakeText(home.transform, "TitleLeft", "TILT", 92f, Brown, mid, new Vector2(-16f, 40f), new Vector2(520f, 140f), TextAlignmentOptions.Right, new Vector2(1f, 0.5f));
            MakeText(home.transform, "TitleRight", "MIRROR", 92f, Mint, mid, new Vector2(16f, 40f), new Vector2(520f, 140f), TextAlignmentOptions.Left, new Vector2(0f, 0.5f));
            MakeText(home.transform, "TaglineLeft", "Move one.", 44f, Brown, mid, new Vector2(-16f, -70f), new Vector2(500f, 70f), TextAlignmentOptions.Right, new Vector2(1f, 0.5f));
            MakeText(home.transform, "TaglineRight", "Move both.", 44f, Mint, mid, new Vector2(16f, -70f), new Vector2(500f, 70f), TextAlignmentOptions.Left, new Vector2(0f, 0.5f));

            Button play = MakeButton(home.transform, "PlayButton", round, Gold, mid, new Vector2(0f, -330f), new Vector2(640f, 180f));
            MakeText(play.transform, "PlayText", "PLAY", 76f, Navy, mid, new Vector2(0f, 24f), new Vector2(560f, 100f), TextAlignmentOptions.Center, mid);
            TextMeshProUGUI playLabel = MakeText(play.transform, "PlayLabel", "LEVEL 1", 36f, Navy, mid, new Vector2(0f, -50f), new Vector2(560f, 50f), TextAlignmentOptions.Center, mid);
            OnClick(play, new UnityAction(menu.PlayCurrent));

            Button levelsBtn = MakeButton(home.transform, "LevelsButton", circle, Brown, mid, new Vector2(-110f, -570f), new Vector2(140f, 140f));
            MakeIcon(levelsBtn.transform, "Icon", levelsIcon, Peach, mid, Vector2.zero, new Vector2(70f, 70f));
            OnClick(levelsBtn, new UnityAction(menu.ShowLevelsFromButton));

            Button soundBtn = MakeButton(home.transform, "SoundButton", circle, Mint, mid, new Vector2(110f, -570f), new Vector2(140f, 140f));
            Image soundImg = MakeIcon(soundBtn.transform, "Icon", soundOn, Navy, mid, Vector2.zero, new Vector2(70f, 70f));
            OnClick(soundBtn, new UnityAction(menu.ToggleSound));

            // ---------- Levels screen ----------
            GameObject levels = new GameObject("LevelsScreen", typeof(RectTransform));
            levels.transform.SetParent(root, false);
            Stretch(levels);
            Undo.RegisterCreatedObjectUndo(levels, "Build Menu UI");

            Image levelsBg = levels.AddComponent<Image>();
            levelsBg.color = DeepNavy;
            levelsBg.raycastTarget = true;

            Button levelsBack = MakeButton(levels.transform, "BackButton", circle, Glass, top, new Vector2(-440f, -130f), new Vector2(120f, 120f));
            MakeIcon(levelsBack.transform, "Icon", back, Color.white, mid, Vector2.zero, new Vector2(64f, 64f));
            OnClick(levelsBack, new UnityAction(menu.ShowHomeFromButton));

            MakeText(levels.transform, "Title", "LEVELS", 76f, Color.white, top, new Vector2(0f, -130f), new Vector2(520f, 100f), TextAlignmentOptions.Center, mid);
            MakeIcon(levels.transform, "StarIcon", star, Gold, top, new Vector2(300f, -130f), new Vector2(52f, 52f));
            TextMeshProUGUI totalStars = MakeText(levels.transform, "TotalStars", "0 / 0", 44f, Color.white, top, new Vector2(420f, -130f), new Vector2(200f, 70f), TextAlignmentOptions.Center, mid);

            GameObject scroll = new GameObject("Scroll", typeof(RectTransform));
            scroll.transform.SetParent(levels.transform, false);
            RectTransform scrollRt = (RectTransform)scroll.transform;
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(40f, 60f);
            scrollRt.offsetMax = new Vector2(-40f, -260f);
            Image scrollImg = scroll.AddComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0f);
            scrollImg.raycastTarget = true;
            ScrollRect scrollRect = scroll.AddComponent<ScrollRect>();

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scroll.transform, false);
            Stretch(viewport);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;

            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(200f, 200f);
            grid.spacing = new Vector2(24f, 24f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(0, 0, 20, 60);

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;
            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 40f;

            // ---------- connect everything ----------
            Assign(menu, "controller", controller);
            Assign(menu, "homeScreen", home);
            Assign(menu, "levelsScreen", levels);
            Assign(menu, "gameScreen", gameScreen);
            Assign(menu, "levelsContent", contentRt);
            Assign(menu, "playLabel", playLabel);
            Assign(menu, "totalStarsText", totalStars);
            Assign(menu, "soundIcon", soundImg);
            Assign(menu, "soundOnSprite", soundOn);
            Assign(menu, "soundOffSprite", soundOff);
            Assign(menu, "roundRect", round);
            Assign(menu, "starSprite", star);
            Assign(menu, "lockSprite", lockIcon);

            // The menu decides when a level starts.
            SerializedObject so = new SerializedObject(controller);
            SerializedProperty autoStart = so.FindProperty("autoStart");
            if (autoStart != null)
            {
                autoStart.boolValue = false;
                so.ApplyModifiedProperties();
            }

            gameScreen.SetActive(false);
            levels.SetActive(false);
            home.SetActive(true);

            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Selection.activeGameObject = canvasGo;

            EditorUtility.DisplayDialog("Tilt Mirror",
                "Menyular tayyor!\n\nPlay bosing: bosh ekran chiqadi. PLAY tugmasi o'yinni boshlaydi.", "OK");
        }

        // ---------- helpers ----------

        static Sprite LoadSprite(string file)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(UiPath + file + ".png");
        }

        static void Assign(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError("Tilt Mirror: '" + field + "' maydoni " + target.GetType().Name + " ichida topilmadi.");
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        static void OnClick(Button button, UnityAction action)
        {
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        static void Stretch(GameObject go)
        {
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void SetRect(GameObject go, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // Left or right half of the screen, filled with one color.
        static void MakeHalf(Transform parent, string name, Color color, float xMin, float xMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
        }

        static Image MakeBox(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetRect(go, anchor, pos, size, new Vector2(0.5f, 0.5f));
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        static Image MakeIcon(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetRect(go, anchor, pos, size, new Vector2(0.5f, 0.5f));
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Button MakeButton(Transform parent, string name, Sprite bg, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            Image img = MakeBox(parent, name, bg, color, anchor, pos, size);
            Button button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            return button;
        }

        static TextMeshProUGUI MakeText(Transform parent, string name, string text, float fontSize, Color color,
            Vector2 anchor, Vector2 pos, Vector2 size, TextAlignmentOptions align, Vector2 pivot)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetRect(go, anchor, pos, size, pivot);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.fontStyle = FontStyles.Bold;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }
    }
}
