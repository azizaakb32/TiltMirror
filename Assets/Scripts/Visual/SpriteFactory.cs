using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiltMirror
{
    // Gives BoardView its sprites.
    // 1) If the PNG exists in Assets/Resources/TiltMirror/Sprites it is used (the real art).
    // 2) Otherwise a simple white sprite is generated in code, so the game always runs.
    // "Pre-colored" art must NOT be tinted; generated fallbacks are white and get tinted.
    public static class SpriteFactory
    {
        const int Ppu = 64;
        const string Root = "TiltMirror/Sprites/";

        static readonly HashSet<Sprite> preColored = new HashSet<Sprite>();

        static Sprite square;
        static Sprite rounded;
        static Sprite sunHalf;
        static Sprite moonHalf;
        static Sprite sunRing;
        static Sprite moonRing;
        static Sprite sunWall;
        static Sprite moonWall;
        static Sprite dot;
        static Sprite key;
        static Sprite sunLock;
        static Sprite moonLock;
        static Sprite glow;
        static bool glowLoaded;

        public static bool IsPreColored(Sprite s)
        {
            return s != null && preColored.Contains(s);
        }

        static Sprite FromResources(string file, bool isPreColored)
        {
            Sprite s = Resources.Load<Sprite>(Root + file);
            if (s != null && isPreColored) preColored.Add(s);
            return s;
        }

        // 1 x 1 unit white square (used for boards, grid lines and the seam).
        public static Sprite Square()
        {
            if (square == null) square = Make(2, 2, 2, delegate (float x, float y) { return 1f; });
            return square;
        }

        // 1 x 1 unit white rounded square (fallback for walls).
        public static Sprite RoundedSquare()
        {
            if (rounded == null)
                rounded = Make(64, 64, Ppu, delegate (float x, float y) { return RoundedAlpha(x, y, 64f, 16f); });
            return rounded;
        }

        public static Sprite SunWall()
        {
            if (sunWall == null)
            {
                sunWall = FromResources("wall_sun", true);
                if (sunWall == null) sunWall = RoundedSquare();
            }
            return sunWall;
        }

        public static Sprite MoonWall()
        {
            if (moonWall == null)
            {
                moonWall = FromResources("wall_moon", true);
                if (moonWall == null) moonWall = RoundedSquare();
            }
            return moonWall;
        }

        // Chapter 2: the key (gold, pre-colored) and the lock tiles.
        public static Sprite Key()
        {
            if (key == null)
            {
                key = FromResources("key", true);
                if (key == null)
                    key = Make(64, 64, Ppu, delegate (float x, float y)
                    {
                        float bow = Mathf.Clamp01(18f - Distance(x, y, 22f, 42f) + 0.5f);
                        float hole = Mathf.Clamp01(Distance(x, y, 22f, 42f) - 8f + 0.5f);
                        float shaft = (x > 30f && x < 58f && y > 36f && y < 47f) ? 1f : 0f;
                        return Mathf.Max(bow * hole, shaft);
                    });
            }
            return key;
        }

        public static Sprite SunLock()
        {
            if (sunLock == null)
            {
                sunLock = FromResources("lock_sun", true);
                if (sunLock == null) sunLock = RoundedSquare();
            }
            return sunLock;
        }

        public static Sprite MoonLock()
        {
            if (moonLock == null)
            {
                moonLock = FromResources("lock_moon", true);
                if (moonLock == null) moonLock = RoundedSquare();
            }
            return moonLock;
        }

        // 0.5 x 1 unit half disc. Round on the left, flat on the right (the Sun).
        public static Sprite SunHalf()
        {
            if (sunHalf == null)
            {
                sunHalf = FromResources("sun_half", true);
                if (sunHalf == null)
                    sunHalf = Make(32, 64, Ppu, delegate (float x, float y)
                    {
                        return Mathf.Clamp01(32f - Distance(x, y, 32f, 32f) + 0.5f);
                    });
            }
            return sunHalf;
        }

        // 0.5 x 1 unit half disc. Round on the right, flat on the left (the Moon).
        public static Sprite MoonHalf()
        {
            if (moonHalf == null)
            {
                moonHalf = FromResources("moon_half", true);
                if (moonHalf == null)
                    moonHalf = Make(32, 64, Ppu, delegate (float x, float y)
                    {
                        return Mathf.Clamp01(32f - Distance(x, y, 0f, 32f) + 0.5f);
                    });
            }
            return moonHalf;
        }

        // Goal markers.
        public static Sprite SunRing()
        {
            if (sunRing == null)
            {
                sunRing = FromResources("sun_ring", true);
                if (sunRing == null)
                    sunRing = Make(32, 64, Ppu, delegate (float x, float y)
                    {
                        return Mathf.Clamp01(2.5f - Mathf.Abs(Distance(x, y, 32f, 32f) - 30f));
                    });
            }
            return sunRing;
        }

        public static Sprite MoonRing()
        {
            if (moonRing == null)
            {
                moonRing = FromResources("moon_ring", true);
                if (moonRing == null)
                    moonRing = Make(32, 64, Ppu, delegate (float x, float y)
                    {
                        return Mathf.Clamp01(2.5f - Mathf.Abs(Distance(x, y, 0f, 32f) - 30f));
                    });
            }
            return moonRing;
        }

        // 0.25 x 0.25 unit circle (the eyes). White, tinted by BoardView.
        public static Sprite Dot()
        {
            if (dot == null)
            {
                dot = FromResources("eye_dot", false);
                if (dot == null)
                    dot = Make(16, 16, Ppu, delegate (float x, float y)
                    {
                        return Mathf.Clamp01(8f - Distance(x, y, 8f, 8f) + 0.5f);
                    });
            }
            return dot;
        }

        // Soft vertical glow behind the seam. Optional: returns null if the art is missing.
        public static Sprite SeamGlow()
        {
            if (!glowLoaded)
            {
                glow = FromResources("seam_glow", false);
                glowLoaded = true;
            }
            return glow;
        }

        static float Distance(float x, float y, float cx, float cy)
        {
            float dx = x - cx;
            float dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        static float RoundedAlpha(float x, float y, float size, float r)
        {
            float half = size * 0.5f;
            float qx = Mathf.Abs(x - half) - (half - r);
            float qy = Mathf.Abs(y - half) - (half - r);
            float ox = Mathf.Max(qx, 0f);
            float oy = Mathf.Max(qy, 0f);
            float d = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
            return Mathf.Clamp01(0.5f - d);
        }

        static Sprite Make(int w, int h, int ppu, Func<float, float, float> alphaAt)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float a = alphaAt(x + 0.5f, y + 0.5f);
                    pixels[y * w + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
        }
    }
}
