using UnityEngine;
using UnityEngine.UI;
using TiltMirror.Core;

namespace TiltMirror
{
    // Text-free tutorial: a gold arrow slides across the screen in the direction
    // the player should swipe. Creates its own overlay canvas, no setup needed.
    public sealed class TutorialHint : MonoBehaviour
    {
        const float Cycle = 1.3f;
        const float Travel = 170f;

        RectTransform arrow;
        Image image;
        Vector2 dirVec;
        bool visible;
        float t;

        static Sprite arrowSprite;

        public static TutorialHint Create()
        {
            GameObject go = new GameObject("TutorialHint");
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            TutorialHint hint = go.AddComponent<TutorialHint>();
            hint.Build();
            return hint;
        }

        void Build()
        {
            GameObject a = new GameObject("Arrow", typeof(RectTransform));
            a.transform.SetParent(transform, false);
            arrow = (RectTransform)a.transform;
            arrow.anchorMin = arrow.anchorMax = arrow.pivot = new Vector2(0.5f, 0.5f);
            arrow.sizeDelta = new Vector2(150f, 150f);
            image = a.AddComponent<Image>();
            image.sprite = GetSprite();
            image.color = new Color(1f, 0.69f, 0.125f, 0f);
            image.raycastTarget = false;
            a.SetActive(false);
        }

        public void Show(Dir dir)
        {
            switch (dir)
            {
                case Dir.Up: dirVec = new Vector2(0f, 1f); arrow.localRotation = Quaternion.Euler(0f, 0f, 0f); break;
                case Dir.Left: dirVec = new Vector2(-1f, 0f); arrow.localRotation = Quaternion.Euler(0f, 0f, 90f); break;
                case Dir.Down: dirVec = new Vector2(0f, -1f); arrow.localRotation = Quaternion.Euler(0f, 0f, 180f); break;
                default: dirVec = new Vector2(1f, 0f); arrow.localRotation = Quaternion.Euler(0f, 0f, 270f); break;
            }
            t = 0f;
            visible = true;
            arrow.gameObject.SetActive(true);
        }

        public void Hide()
        {
            visible = false;
            if (arrow != null) arrow.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!visible) return;
            t += Time.unscaledDeltaTime;
            float p = (t % Cycle) / Cycle;
            float ease = 1f - (1f - p) * (1f - p);
            arrow.anchoredPosition = dirVec * (Mathf.Lerp(-0.5f, 0.5f, ease) * Travel);

            float alpha = Mathf.Clamp01(p / 0.2f) * Mathf.Clamp01((1f - p) / 0.3f);
            Color c = image.color;
            c.a = alpha * 0.9f;
            image.color = c;
        }

        // Arrow pointing up, drawn in code so no extra image file is needed.
        static Sprite GetSprite()
        {
            if (arrowSprite != null) return arrowSprite;

            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < 3; sy++)
                        for (int sx = 0; sx < 3; sx++)
                            if (Inside(x + (sx + 0.5f) / 3f, y + (sy + 0.5f) / 3f)) hit++;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(hit * 255 / 9));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            arrowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return arrowSprite;
        }

        // Texture y grows upward: head on top, shaft below.
        static bool Inside(float x, float y)
        {
            float dx = Mathf.Abs(x - 64f);
            if (y >= 64f && y <= 120f) return dx <= (120f - y) * (46f / 56f);
            if (y >= 10f && y < 64f) return dx <= 15f;
            return false;
        }
    }
}
