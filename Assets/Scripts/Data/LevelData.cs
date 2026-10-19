using System.Collections.Generic;
using UnityEngine;
using TiltMirror.Core;

namespace TiltMirror
{
    [CreateAssetMenu(fileName = "Level_000", menuName = "Tilt Mirror/Level")]
    public sealed class LevelData : ScriptableObject
    {
        public int levelNumber = 1;
        public int width = 4;
        public int height = 8;

        [Tooltip("Length of the shortest solution. Filled in automatically by Recalculate Par.")]
        public int par;

        // y = 0 is the TOP row.
        public Vector2Int sunStart = new Vector2Int(0, 7);
        public Vector2Int moonStart = new Vector2Int(3, 7);
        public Vector2Int sunGoal = new Vector2Int(3, 0);
        public Vector2Int moonGoal = new Vector2Int(0, 0);

        public List<Vector2Int> sunWalls = new List<Vector2Int>();
        public List<Vector2Int> moonWalls = new List<Vector2Int>();

        [Header("Chapter 2: key and locks")]
        public bool hasKey;
        [Tooltip("On: the key lies on the Sun board and only the Sun can pick it up. Off: Moon board.")]
        public bool keyOnSun = true;
        public Vector2Int keyPos;
        [Tooltip("Locks act like walls until the key is picked up, then they open on both boards.")]
        public List<Vector2Int> sunLocks = new List<Vector2Int>();
        public List<Vector2Int> moonLocks = new List<Vector2Int>();

        [Header("Tutorial")]
        [Tooltip("Shows the moving arrow hint until the level has been completed once.")]
        public bool showHint;

        public BoardSpec ToSpec()
        {
            BoardSpec spec = new BoardSpec();
            spec.Width = width;
            spec.Height = height;
            spec.SunStart = ToInt2(sunStart);
            spec.MoonStart = ToInt2(moonStart);
            spec.SunGoal = ToInt2(sunGoal);
            spec.MoonGoal = ToInt2(moonGoal);
            for (int i = 0; i < sunWalls.Count; i++) spec.SunWalls.Add(ToInt2(sunWalls[i]));
            for (int i = 0; i < moonWalls.Count; i++) spec.MoonWalls.Add(ToInt2(moonWalls[i]));
            spec.HasKey = hasKey;
            spec.KeyOnSun = keyOnSun;
            spec.KeyPos = ToInt2(keyPos);
            for (int i = 0; i < sunLocks.Count; i++) spec.SunLocks.Add(ToInt2(sunLocks[i]));
            for (int i = 0; i < moonLocks.Count; i++) spec.MoonLocks.Add(ToInt2(moonLocks[i]));
            return spec;
        }

        public static Int2 ToInt2(Vector2Int v)
        {
            return new Int2(v.x, v.y);
        }

        [ContextMenu("Recalculate Par")]
        public void RecalculatePar()
        {
            int solved = MirrorSim.Solve(ToSpec());
            par = solved;
            if (solved < 0) Debug.LogWarning(name + ": level has no solution.", this);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
