using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TiltMirror.EditorTools
{
    // Menu: Tilt Mirror > Build Demo UI
    // Builds the whole in-game UI in the open scene and connects every slot and button:
    // Canvas, swipe layer, HUD (level / moves / par), undo + restart buttons,
    // D-pad, and the JOINED panel. Safe to run again: it replaces its own old UI.
    public static class BuildDemoUI
    {
        const string UiPath = "Assets/Resources/TiltMirror/UI/";
        const string DatabasePath = "Assets/Levels/LevelDatabase.asset";
        const string CanvasName = "TiltMirror Canvas";

        static readonly Color Navy = new Color32(0x1A, 0x1C, 0x4D, 255);
        static readonly Color Gold = new Color32(0xFF, 0xB0, 0x20, 255);
        static readonly Color Grey = new Color32(0x6E, 0x70, 0x94, 255);
        static readonly Color SoftWhite = new Color(1f, 1f, 1f, 0.65f);
        static readonly Color Glass = new Color(1f, 1f, 1f, 0.10f);

        static GameController FindController()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<GameController>();
#else
            return Object.FindObjectOfType<GameController>();
#endif
        }

        static bool HasEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<EventSystem>() != null;
#else
            return Object.FindObjectOfType<EventSystem>() != null;
#endif
        }

        [MenuItem("Tilt Mirror/Build Demo UI")]
        static void Build()
        {
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
            if (db == null)
            {
                EditorUtility.DisplayDialog("Tilt Mirror",
                    "Level fayllari topilmadi.\n\nAvval menyudan\nTilt Mirror > Create Sample Levels (12, 13)\nni bosing, keyin bu buyruqni qayta bosing.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources/TiltMirror/UI"))
            {
                EditorUtility.DisplayDialog("Tilt Mirror",
                    "Assets/Resources/TiltMirror/UI papkasi topilmadi.\n\nProject oynasida Assets > Resources > TiltMirror > UI borligini tekshiring. Papka nomlari aynan shunday bo'lishi kerak.", "OK");
                return;
            }

            // Re-import all art so the Sprite settings are applied, even if the
            // images were imported before these scripts finished compiling.
            AssetDatabase.ImportAsset("Assets/Resources/TiltMirror",
                ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            Sprite round = LoadSprite("ui_round_rect");
            Sprite circle = LoadSprite("ui_circle");
            Sprite star = LoadSprite("ui_star");
            Sprite undoIcon = LoadSprite("ui_undo");
            Sprite restartIcon = LoadSprite("ui_restart");
            Sprite logo = LoadSprite("logo_disc");
            if (round == null || circle == null || star == null)
            {
                EditorUtility.DisplayDialog("Tilt Mirror",
                    "UI rasmlari hali ham Sprite sifatida topilmadi.\n\nProject'da bitta rasmni (masalan ui_circle) bosing va Inspector'da Texture Type 'Sprite (2D and UI)' ekanini tekshiring. Bo'lmasa, uni shunday qilib Apply bosing. Keyin Build Demo UI ni qayta bosing.", "OK");
                return;
            }

            // Old copy of this UI is removed so the tool can be run again.
            GameObject old = GameObject.Find(CanvasName);
            if (old != null) Undo.DestroyObjectImmediate(old);

            // Game object with the controller.
            GameController controller = FindController();
            if (controller == null)
            {
                GameObject g = new GameObject("Game");
                Undo.RegisterCreatedObjectUndo(g, "Build Demo UI");
                controller = g.AddComponent<GameController>();
            }
            Assign(controller, "database", db);

            // Camera must be orthographic for a 2D board.
            Camera cam = Camera.main;
            if (cam != null)
            {
                Undo.RecordObject(cam, "Build Demo UI");
                cam.orthographic = true;
            }

            // Event system (needed for buttons and swipe).
            if (!HasEventSystem())
            {
                GameObject es = new GameObject("EventSystem", typeof(EventSystem));
                Undo.RegisterCreatedObjectUndo(es, "Build Demo UI");
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            // Canvas.
            GameObject canvasGo = new GameObject(CanvasName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Build Demo UI");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform root = canvasGo.transform;

            Vector2 top = new Vector2(0.5f, 1f);
            Vector2 bottom = new Vector2(0.5f, 0f);
            Vector2 mid = new Vector2(0.5f, 0.5f);

            // 1. Swipe layer (first = behind everything else).
            GameObject swipe = new GameObject("SwipeLayer", typeof(RectTransform));
            swipe.transform.SetParent(root, false);
            Stretch(swipe);
            Image swipeImg = swipe.AddComponent<Image>();
            swipeImg.color = new Color(0f, 0f, 0f, 0f);
            swipeImg.raycastTarget = true;
            SwipeInput swipeInput = swipe.AddComponent<SwipeInput>();
            Assign(swipeInput, "controller", controller);

            // 2. HUD.
            GameObject hudGo = new GameObject("Hud", typeof(RectTransform));
            hudGo.transform.SetParent(root, false);
            Stretch(hudGo);
            HudView hud = hudGo.AddComponent<HudView>();

            TextMeshProUGUI levelText = MakeText(hudGo.transform, "LevelText", "LEVEL 1", 72f, Color.white, top, new Vector2(0f, -130f), new Vector2(520f, 100f));
            MakeText(hudGo.transform, "MovesLabel", "MOVES", 30f, SoftWhite, top, new Vector2(-190f, -235f), new Vector2(300f, 50f));
            TextMeshProUGUI movesText = MakeText(hudGo.transform, "MovesText", "0", 64f, Color.white, top, new Vector2(-190f, -300f), new Vector2(300f, 90f));
            MakeText(hudGo.transform, "ParLabel", "PAR", 30f, SoftWhite, top, new Vector2(190f, -235f), new Vector2(300f, 50f));
            TextMeshProUGUI parText = MakeText(hudGo.transform, "ParText", "0", 64f, Color.white, top, new Vector2(190f, -300f), new Vector2(300f, 90f));

            Assign(hud, "levelText", levelText);
            Assign(hud, "movesText", movesText);
            Assign(hud, "parText", parText);
            Assign(controller, "hud", hud);

            // 3. Undo (top left) and restart (top right).
            Button undoBtn = MakeButton(hudGo.transform, "UndoButton", circle, Glass, top, new Vector2(-440f, -130f), new Vector2(120f, 120f));
            MakeIcon(undoBtn.transform, "Icon", undoIcon, Color.white, mid, Vector2.zero, new Vector2(64f, 64f));
            OnClick(undoBtn, new UnityAction(controller.Undo));

            Button restartBtn = MakeButton(hudGo.transform, "RestartButton", circle, Glass, top, new Vector2(440f, -130f), new Vector2(120f, 120f));
            MakeIcon(restartBtn.transform, "Icon", restartIcon, Color.white, mid, Vector2.zero, new Vector2(64f, 64f));
            OnClick(restartBtn, new UnityAction(controller.Restart));

            // 4. D-pad (cross shape under the board).
            GameObject dpad = new GameObject("DPad", typeof(RectTransform));
            dpad.transform.SetParent(root, false);
            Stretch(dpad);
            Vector2 btnSize = new Vector2(200f, 124f);
            Button up = MakeDpadButton(dpad.transform, "Up", round, LoadSprite("dpad_up"), bottom, new Vector2(0f, 384f), btnSize);
            Button left = MakeDpadButton(dpad.transform, "Left", round, LoadSprite("dpad_left"), bottom, new Vector2(-215f, 250f), btnSize);
            Button right = MakeDpadButton(dpad.transform, "Right", round, LoadSprite("dpad_right"), bottom, new Vector2(215f, 250f), btnSize);
            Button down = MakeDpadButton(dpad.transform, "Down", round, LoadSprite("dpad_down"), bottom, new Vector2(0f, 116f), btnSize);
            MakeIcon(dpad.transform, "Center", logo, Color.white, bottom, new Vector2(0f, 250f), new Vector2(96f, 96f));
            OnClick(up, new UnityAction(controller.MoveUp));
            OnClick(left, new UnityAction(controller.MoveLeft));
            OnClick(right, new UnityAction(controller.MoveRight));
            OnClick(down, new UnityAction(controller.MoveDown));

            // 5. WinPanel (last = on top of everything).
            GameObject winGo = new GameObject("WinPanel", typeof(RectTransform));
            winGo.transform.SetParent(root, false);
            Stretch(winGo);
            WinPanel win = winGo.AddComponent<WinPanel>();

            GameObject overlay = new GameObject("Overlay", typeof(RectTransform));
            overlay.transform.SetParent(winGo.transform, false);
            Stretch(overlay);
            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.02f, 0.1f, 0.7f);
            dim.raycastTarget = true;

            Image card = MakeBox(overlay.transform, "Card", round, Color.white, mid, Vector2.zero, new Vector2(900f, 820f));
            Transform c = card.transform;

            MakeIcon(c, "Logo", logo, Color.white, mid, new Vector2(0f, 300f), new Vector2(190f, 190f));
            MakeText(c, "JoinedText", "JOINED", 84f, Navy, mid, new Vector2(0f, 150f), new Vector2(700f, 110f));

            Image[] starImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                starImages[i] = MakeIcon(c, "Star" + (i + 1), star, Gold, mid, new Vector2((i - 1) * 160f, 0f), new Vector2(130f, 130f));
            }

            MakeText(c, "MovesLabel", "MOVES", 34f, Grey, mid, new Vector2(-170f, -130f), new Vector2(300f, 50f));
            TextMeshProUGUI winMoves = MakeText(c, "MovesText", "0", 64f, Navy, mid, new Vector2(-170f, -190f), new Vector2(300f, 90f));
            MakeText(c, "ParLabel", "PAR", 34f, Grey, mid, new Vector2(170f, -130f), new Vector2(300f, 50f));
            TextMeshProUGUI winPar = MakeText(c, "ParText", "0", 64f, Navy, mid, new Vector2(170f, -190f), new Vector2(300f, 90f));

            Button replay = MakeButton(c, "ReplayButton", round, Navy, mid, new Vector2(-215f, -320f), new Vector2(380f, 130f));
            MakeText(replay.transform, "Label", "Replay", 48f, Color.white, mid, Vector2.zero, new Vector2(360f, 90f));
            OnClick(replay, new UnityAction(controller.Restart));

            Button next = MakeButton(c, "NextButton", round, Gold, mid, new Vector2(215f, -320f), new Vector2(380f, 130f));
            MakeText(next.transform, "Label", "Next", 48f, Navy, mid, Vector2.zero, new Vector2(360f, 90f));
            OnClick(next, new UnityAction(controller.Next));

            Assign(win, "root", overlay);
            Assign(win, "movesText", winMoves);
            Assign(win, "parText", winPar);
            SerializedObject winSo = new SerializedObject(win);
            SerializedProperty starsProp = winSo.FindProperty("stars");
            starsProp.arraySize = 3;
            for (int i = 0; i < 3; i++) starsProp.GetArrayElementAtIndex(i).objectReferenceValue = starImages[i];
            winSo.ApplyModifiedProperties();
            Assign(controller, "winPanel", win);

            overlay.SetActive(false);

            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Selection.activeGameObject = controller.gameObject;

            EditorUtility.DisplayDialog("Tilt Mirror",
                "UI tayyor!\n\nEndi Play tugmasini bosing.\nGame oynasida yuqoridagi o'lchamni 9:16 Portrait qilib qo'ying.", "OK");
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

        static void SetRect(GameObject go, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // Background image that stretches with 9-slice (buttons, cards).
        static Image MakeBox(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetRect(go, anchor, pos, size);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        // Icon that keeps its proportions and does not catch clicks.
        static Image MakeIcon(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetRect(go, anchor, pos, size);
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

        static Button MakeDpadButton(Transform parent, string name, Sprite bg, Sprite arrows, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            Button button = MakeButton(parent, name, bg, Glass, anchor, pos, size);
            MakeIcon(button.transform, "Arrows", arrows, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x * 0.8f, size.y * 0.8f));
            return button;
        }

        static TextMeshProUGUI MakeText(Transform parent, string name, string text, float fontSize, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetRect(go, anchor, pos, size);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }
    }
}
