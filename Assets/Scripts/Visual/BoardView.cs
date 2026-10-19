using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TiltMirror.Core;

namespace TiltMirror
{
    // Draws both boards and animates the two half-discs.
    // Layout: Sun board on the left, Moon board on the right, a thin seam between them.
    // The whole board sits on a pivot that tilts slightly on every move.
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] float seamGap = 0.09f;
        [SerializeField] float halfOffset = 0.25f;     // half-disc sits in the seam-side half of its cell
        [SerializeField] float slideSpeed = 14f;       // cells per second
        [SerializeField] float tiltDegrees = 1.6f;
        [Tooltip("Share of the screen height kept free at the top for the HUD.")]
        [SerializeField, Range(0f, 0.4f)] float topReserve = 0.135f;
        [Tooltip("Share of the screen height kept free at the bottom for the D-pad.")]
        [SerializeField, Range(0f, 0.5f)] float bottomReserve = 0.33f;

        public bool IsAnimating { get; private set; }

        BoardSpec spec;
        Transform pivot;
        Transform sunChar;
        Transform moonChar;
        SpriteRenderer seam;
        SpriteRenderer seamGlowRenderer;
        SpriteRenderer sunGoalRenderer;
        SpriteRenderer moonGoalRenderer;
        SpriteRenderer keyRenderer;
        readonly List<SpriteRenderer> lockRenderers = new List<SpriteRenderer>();
        Vector3 keyBaseScale;
        bool keyBusy;
        Vector3 sunOrigin;   // top-left corner of the Sun board
        Vector3 moonOrigin;  // top-left corner of the Moon board

        public void Build(BoardSpec newSpec)
        {
            StopAllCoroutines();
            IsAnimating = false;
            spec = newSpec;

            if (pivot != null) Destroy(pivot.gameObject);
            pivot = new GameObject("Pivot").transform;
            pivot.SetParent(transform, false);

            float w = spec.Width;
            float h = spec.Height;
            sunOrigin = new Vector3(-seamGap * 0.5f - w, h * 0.5f, 0f);
            moonOrigin = new Vector3(seamGap * 0.5f, h * 0.5f, 0f);

            BuildGround(sunOrigin, Palette.SunGround, new Color(0.48f, 0.18f, 0.12f, 0.11f));
            BuildGround(moonOrigin, Palette.MoonGround, new Color(0.75f, 0.95f, 0.93f, 0.09f));

            foreach (Int2 c in spec.SunWalls)
                Make("SunWall", SpriteFactory.SunWall(), Palette.SunWall, CellCenter(sunOrigin, c, 0f), new Vector2(0.86f, 0.86f), 2);
            foreach (Int2 c in spec.MoonWalls)
                Make("MoonWall", SpriteFactory.MoonWall(), Palette.MoonWall, CellCenter(moonOrigin, c, 0f), new Vector2(0.86f, 0.86f), 2);

            keyRenderer = null;
            lockRenderers.Clear();
            keyBusy = false;
            if (spec.HasKey)
            {
                foreach (Int2 c in spec.SunLocks)
                    lockRenderers.Add(Make("SunLock", SpriteFactory.SunLock(), Palette.Sun, CellCenter(sunOrigin, c, 0f), new Vector2(0.86f, 0.86f), 2));
                foreach (Int2 c in spec.MoonLocks)
                    lockRenderers.Add(Make("MoonLock", SpriteFactory.MoonLock(), Palette.Sun, CellCenter(moonOrigin, c, 0f), new Vector2(0.86f, 0.86f), 2));
                Vector3 keyPos = CellCenter(spec.KeyOnSun ? sunOrigin : moonOrigin, spec.KeyPos, 0f);
                keyBaseScale = new Vector3(0.8f, 0.8f, 1f);
                keyRenderer = Make("Key", SpriteFactory.Key(), Palette.Sun, keyPos, new Vector2(0.8f, 0.8f), 4);
            }

            sunGoalRenderer = Make("SunGoal", SpriteFactory.SunRing(), Palette.SunWall, SunPos(spec.SunGoal), Vector2.one, 3);
            moonGoalRenderer = Make("MoonGoal", SpriteFactory.MoonRing(), Palette.Moon, MoonPos(spec.MoonGoal), Vector2.one, 3);

            sunChar = BuildCharacter("Sun", SpriteFactory.SunHalf(), Palette.Sun, Palette.SunWall, new Vector3(0.1f, 0.08f, 0f));
            moonChar = BuildCharacter("Moon", SpriteFactory.MoonHalf(), Palette.Moon, Palette.MoonGround, new Vector3(-0.1f, 0.08f, 0f));

            seam = Make("Seam", SpriteFactory.Square(), Palette.Seam, Vector3.zero, new Vector2(seamGap, h + 0.14f), 6);

            // Optional soft glow behind the seam, lit up when the two halves join.
            // The glow sprite is 0.5 x 0.5 units, so it is scaled up to 1.4 wide and the board height.
            seamGlowRenderer = null;
            Sprite glowSprite = SpriteFactory.SeamGlow();
            if (glowSprite != null)
                seamGlowRenderer = Make("SeamGlow", glowSprite, new Color(1f, 1f, 1f, 0f), Vector3.zero, new Vector2(2.8f, (h + 0.6f) / 0.5f), 4);

            SnapTo(spec.StartState);
            FitCamera();
        }

        public void SnapTo(SimState s)
        {
            sunChar.localPosition = SunPos(s.Sun);
            moonChar.localPosition = MoonPos(s.Moon);
            pivot.localRotation = Quaternion.identity;
            ShowKeyState(s.HasKey);
        }

        // Key and locks are visible until the key has been picked up.
        void ShowKeyState(bool hasKey)
        {
            keyBusy = false;
            if (keyRenderer != null)
            {
                keyRenderer.gameObject.SetActive(!hasKey);
                keyRenderer.transform.localScale = keyBaseScale;
                keyRenderer.transform.localRotation = Quaternion.identity;
                keyRenderer.color = ColorWithAlpha(keyRenderer.color, 1f);
            }
            for (int i = 0; i < lockRenderers.Count; i++)
            {
                lockRenderers[i].gameObject.SetActive(!hasKey);
                lockRenderers[i].transform.localScale = new Vector3(0.86f, 0.86f, 1f);
                lockRenderers[i].color = ColorWithAlpha(lockRenderers[i].color, 1f);
            }
        }

        static Color ColorWithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        public void AnimateTo(SimState to, Dir dir, Action onDone)
        {
            StartCoroutine(SlideRoutine(to, dir, onDone));
        }

        public void PlayBlocked(Dir dir)
        {
            if (!IsAnimating) StartCoroutine(BlockedRoutine(dir));
        }

        public void PlayJoin()
        {
            StartCoroutine(JoinRoutine());
        }

        // ---- layout helpers ----

        Vector3 CellCenter(Vector3 origin, Int2 c, float xOffset)
        {
            return origin + new Vector3(c.X + 0.5f + xOffset, -(c.Y + 0.5f), 0f);
        }

        Vector3 SunPos(Int2 c) { return CellCenter(sunOrigin, c, +halfOffset); }
        Vector3 MoonPos(Int2 c) { return CellCenter(moonOrigin, c, -halfOffset); }

        void BuildGround(Vector3 origin, Color ground, Color line)
        {
            float w = spec.Width;
            float h = spec.Height;
            Make("Ground", SpriteFactory.Square(), ground, origin + new Vector3(w * 0.5f, -h * 0.5f, 0f), new Vector2(w, h), 0);
            for (int x = 1; x < spec.Width; x++)
                Make("GridV", SpriteFactory.Square(), line, origin + new Vector3(x, -h * 0.5f, 0f), new Vector2(0.025f, h), 1);
            for (int y = 1; y < spec.Height; y++)
                Make("GridH", SpriteFactory.Square(), line, origin + new Vector3(w * 0.5f, -y, 0f), new Vector2(w, 0.025f), 1);
        }

        SpriteRenderer Make(string objName, Sprite sprite, Color color, Vector3 localPos, Vector2 scale, int order)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(pivot, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            // Pre-colored art keeps its own colors; generated white sprites are tinted.
            sr.color = SpriteFactory.IsPreColored(sprite) ? new Color(1f, 1f, 1f, color.a) : color;
            sr.sortingOrder = order;
            return sr;
        }

        Transform BuildCharacter(string objName, Sprite sprite, Color body, Color eyeColor, Vector3 eyeLocal)
        {
            SpriteRenderer sr = Make(objName, sprite, body, Vector3.zero, Vector2.one, 5);
            SpriteRenderer eye = Make(objName + "Eye", SpriteFactory.Dot(), eyeColor, Vector3.zero, new Vector2(0.7f, 0.7f), 6);
            eye.transform.SetParent(sr.transform, false);
            eye.transform.localPosition = eyeLocal;
            return sr.transform;
        }

        void FitCamera()
        {
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;
            // The board lives in the band between the HUD (top) and the D-pad (bottom).
            float band = Mathf.Max(0.2f, 1f - topReserve - bottomReserve);
            float byHeight = (spec.Height + 0.4f) / (2f * band);
            float totalWidth = spec.Width * 2f + seamGap + 0.8f;
            float byWidth = totalWidth * 0.5f / cam.aspect;
            float size = Mathf.Max(byHeight, byWidth);
            cam.orthographicSize = size;

            // Move the camera down so the board sits in the middle of that band.
            float bandCenter = bottomReserve + band * 0.5f;      // 0 = bottom, 1 = top
            float shift = (bandCenter - 0.5f) * 2f * size;       // world units above screen center
            Vector3 p = cam.transform.position;
            cam.transform.position = new Vector3(0f, -shift, p.z);
        }

        // ---- animation ----

        void Update()
        {
            if (keyRenderer != null && keyRenderer.gameObject.activeSelf && !keyBusy)
            {
                float k = 1f + 0.07f * Mathf.Sin(Time.time * 4.2f);
                keyRenderer.transform.localScale = keyBaseScale * k;
                keyRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(Time.time * 2.1f));
            }

            if (sunGoalRenderer == null) return;
            float a = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.time * 3.9f));
            Color c1 = SpriteFactory.IsPreColored(sunGoalRenderer.sprite) ? Color.white : Palette.SunWall;
            c1.a = a;
            sunGoalRenderer.color = c1;
            Color c2 = SpriteFactory.IsPreColored(moonGoalRenderer.sprite) ? Color.white : Palette.Moon;
            c2.a = a;
            moonGoalRenderer.color = c2;
        }

        IEnumerator SlideRoutine(SimState to, Dir dir, Action onDone)
        {
            IsAnimating = true;
            Vector3 sFrom = sunChar.localPosition;
            Vector3 mFrom = moonChar.localPosition;
            Vector3 sTo = SunPos(to.Sun);
            Vector3 mTo = MoonPos(to.Moon);

            float dist = Mathf.Max((sTo - sFrom).magnitude, (mTo - mFrom).magnitude);
            float duration = Mathf.Clamp(dist / slideSpeed, 0.1f, 0.4f);

            float tilt = 0f;
            if (dir == Dir.Left) tilt = -tiltDegrees;
            else if (dir == Dir.Right) tilt = tiltDegrees;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float k = Mathf.Clamp01(t);
                float e = 1f - Mathf.Pow(1f - k, 3f);   // ease out cubic
                sunChar.localPosition = Vector3.LerpUnclamped(sFrom, sTo, e);
                moonChar.localPosition = Vector3.LerpUnclamped(mFrom, mTo, e);
                pivot.localRotation = Quaternion.Euler(0f, 0f, tilt * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }

            sunChar.localPosition = sTo;
            moonChar.localPosition = mTo;
            pivot.localRotation = Quaternion.identity;

            if (to.HasKey && keyRenderer != null && keyRenderer.gameObject.activeSelf)
                yield return UnlockRoutine();

            IsAnimating = false;
            if (onDone != null) onDone();
        }

        // The key pops and fades, then every lock shrinks away.
        IEnumerator UnlockRoutine()
        {
            keyBusy = true;
            Color keyColor = keyRenderer.color;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.2f;
                float k = Mathf.Clamp01(t);
                keyRenderer.transform.localScale = keyBaseScale * (1f + 0.7f * k);
                keyRenderer.color = ColorWithAlpha(keyColor, 1f - k);
                yield return null;
            }
            keyRenderer.gameObject.SetActive(false);

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.28f;
                float k = Mathf.Clamp01(t);
                float s = 0.86f * (1f - 0.55f * k);
                for (int i = 0; i < lockRenderers.Count; i++)
                {
                    lockRenderers[i].transform.localScale = new Vector3(s, s, 1f);
                    lockRenderers[i].color = ColorWithAlpha(lockRenderers[i].color, 1f - k);
                }
                yield return null;
            }
            for (int i = 0; i < lockRenderers.Count; i++) lockRenderers[i].gameObject.SetActive(false);
            keyBusy = false;
        }

        IEnumerator BlockedRoutine(Dir dir)
        {
            IsAnimating = true;
            int dx, dy;
            MirrorSim.Delta(dir, out dx, out dy);
            Vector3 sBase = sunChar.localPosition;
            Vector3 mBase = moonChar.localPosition;
            Vector3 sNudge = new Vector3(dx, -dy, 0f) * 0.07f;      // world y is up, grid y is down
            Vector3 mNudge = new Vector3(-dx, -dy, 0f) * 0.07f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.14f;
                float k = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                sunChar.localPosition = sBase + sNudge * k;
                moonChar.localPosition = mBase + mNudge * k;
                yield return null;
            }
            sunChar.localPosition = sBase;
            moonChar.localPosition = mBase;
            IsAnimating = false;
        }

        IEnumerator JoinRoutine()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.35f;
                float k = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                float s = 1f + 0.15f * k;
                sunChar.localScale = new Vector3(s, s, 1f);
                moonChar.localScale = new Vector3(s, s, 1f);
                seam.transform.localScale = new Vector3(seamGap * (1f + 1.4f * k), spec.Height + 0.14f, 1f);
                if (seamGlowRenderer != null) seamGlowRenderer.color = new Color(1f, 1f, 1f, 0.9f * k);
                yield return null;
            }
            sunChar.localScale = Vector3.one;
            moonChar.localScale = Vector3.one;
            seam.transform.localScale = new Vector3(seamGap * 1.5f, spec.Height + 0.14f, 1f);
            if (seamGlowRenderer != null) seamGlowRenderer.color = new Color(1f, 1f, 1f, 0.55f);
        }
    }
}
