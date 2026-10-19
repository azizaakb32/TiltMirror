using UnityEngine;
using TiltMirror.Core;

namespace TiltMirror.EditorTools
{
    // Two hand-checked levels (found with a breadth-first search while designing the prototype).
    // Walls are flat lists of x,y pairs. y = 0 is the top row.
    public static class SampleLevels
    {
        public struct Sample
        {
            public int Number;
            public Vector2Int SunGoal;
            public Vector2Int MoonGoal;
            public int[] SunWalls;
            public int[] MoonWalls;
            public int ExpectedPar;
        }

        public static readonly Sample[] All = new Sample[]
        {
            new Sample
            {
                Number = 12,
                SunGoal = new Vector2Int(3, 0),
                MoonGoal = new Vector2Int(0, 0),
                SunWalls = new int[] { 0,0, 0,4, 0,5, 1,3, 1,4, 1,7, 2,2, 2,4, 2,6 },
                MoonWalls = new int[] { 0,2, 0,3, 0,7, 1,3, 2,1, 2,3, 2,6, 3,5, 3,6 },
                ExpectedPar = 7
            },
            new Sample
            {
                Number = 13,
                SunGoal = new Vector2Int(3, 1),
                MoonGoal = new Vector2Int(0, 3),
                SunWalls = new int[] { 0,0, 0,1, 1,0, 1,6, 1,7, 2,5, 2,6, 3,2, 3,7 },
                MoonWalls = new int[] { 0,7, 1,1, 1,2, 1,7, 2,0, 2,1, 2,6, 3,1, 3,3 },
                ExpectedPar = 8
            }
        };

        public static BoardSpec ToSpec(Sample s)
        {
            BoardSpec spec = new BoardSpec();
            spec.Width = 4;
            spec.Height = 8;
            spec.SunStart = new Int2(0, 7);
            spec.MoonStart = new Int2(3, 7);
            spec.SunGoal = new Int2(s.SunGoal.x, s.SunGoal.y);
            spec.MoonGoal = new Int2(s.MoonGoal.x, s.MoonGoal.y);
            for (int i = 0; i + 1 < s.SunWalls.Length; i += 2) spec.SunWalls.Add(new Int2(s.SunWalls[i], s.SunWalls[i + 1]));
            for (int i = 0; i + 1 < s.MoonWalls.Length; i += 2) spec.MoonWalls.Add(new Int2(s.MoonWalls[i], s.MoonWalls[i + 1]));
            return spec;
        }
    }
}
