using UnityEngine;

namespace TiltMirror
{
    // Same colors as the design canvas.
    public static class Palette
    {
        public static readonly Color Background = new Color32(0x0B, 0x0C, 0x24, 255);

        public static readonly Color SunGround = new Color32(0xFF, 0xD9, 0xBF, 255);
        public static readonly Color SunWall = new Color32(0x7A, 0x2E, 0x1F, 255);
        public static readonly Color Sun = new Color32(0xFF, 0xB0, 0x20, 255);

        public static readonly Color MoonGround = new Color32(0x1A, 0x1C, 0x4D, 255);
        public static readonly Color MoonWall = new Color32(0x6C, 0x73, 0xEA, 255);
        public static readonly Color Moon = new Color32(0xBF, 0xF3, 0xEE, 255);

        public static readonly Color Seam = Color.white;
    }
}
