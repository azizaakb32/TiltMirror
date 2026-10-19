using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TiltMirror.Core;

namespace TiltMirror.EditorTools
{
    // Menu: Tilt Mirror > Add Chapter 2 (15 levels)
    // Adds levels 31-45 (key and lock levels) after the 30 levels of Chapter 1.
    // Chapter 1 levels and the saved progress are NOT touched. Safe to run again:
    // old levels 31 and above are replaced. Every level is checked with the solver.
    public static class Chapter2Importer
    {
        const string Folder = "Assets/Levels";
        const int FirstNumber = 31;

        struct Entry
        {
            public int Number, Par, SunGoalX, SunGoalY, MoonGoalX, MoonGoalY, KeyX, KeyY;
            public int[] SunWalls, MoonWalls, SunLocks, MoonLocks;
            public bool KeyOnSun;

            public Entry(int number, int par, int sgx, int sgy, int mgx, int mgy,
                int[] sunWalls, int[] moonWalls, int[] sunLocks, int[] moonLocks, bool keyOnSun, int keyX, int keyY)
            {
                Number = number; Par = par;
                SunGoalX = sgx; SunGoalY = sgy; MoonGoalX = mgx; MoonGoalY = mgy;
                SunWalls = sunWalls; MoonWalls = moonWalls; SunLocks = sunLocks; MoonLocks = moonLocks;
                KeyOnSun = keyOnSun; KeyX = keyX; KeyY = keyY;
            }
        }

        // x,y pairs. y = 0 is the top row. Sun starts at (0,7), Moon at (3,7).
        static readonly Entry[] Pack = new Entry[]
        {
            new Entry(31, 4, 0, 0, 3, 0, new int[] { 2,0, 1,2 }, new int[] { 1,1, 1,2 }, new int[] { 0,4 }, new int[0], true, 1, 5),
            new Entry(32, 5, 0, 2, 3, 0, new int[] { 3,1, 1,5, 2,5 }, new int[] { 0,0, 2,5, 0,7 }, new int[] { 1,7 }, new int[0], false, 3, 2),
            new Entry(33, 5, 0, 1, 3, 3, new int[] { 0,2, 1,4, 2,4 }, new int[] { 0,0, 1,0, 3,4 }, new int[0], new int[] { 2,1 }, false, 3, 5),
            new Entry(34, 6, 0, 0, 2, 0, new int[] { 3,1, 3,3, 2,6 }, new int[] { 3,0, 2,1, 2,2 }, new int[0], new int[] { 0,6 }, true, 1, 0),
            new Entry(35, 6, 0, 0, 1, 1, new int[] { 1,3, 2,3, 1,7 }, new int[] { 1,0, 3,3, 2,7 }, new int[] { 3,0 }, new int[] { 1,2 }, false, 1, 6),
            new Entry(36, 7, 3, 0, 1, 4, new int[] { 2,0, 2,2, 1,3, 2,3 }, new int[] { 3,2, 0,3, 2,4, 2,6 }, new int[] { 3,4 }, new int[0], false, 1, 6),
            new Entry(37, 7, 3, 2, 3, 2, new int[] { 1,1, 3,1, 2,3, 3,4 }, new int[] { 1,0, 3,1, 0,5, 1,7 }, new int[] { 2,7 }, new int[] { 3,4 }, false, 3, 0),
            new Entry(38, 8, 0, 3, 1, 0, new int[] { 0,0, 3,0, 0,2, 1,3 }, new int[] { 3,2, 2,5, 0,6, 3,6 }, new int[0], new int[] { 2,6, 1,7 }, true, 1, 7),
            new Entry(39, 8, 0, 0, 3, 0, new int[] { 2,1, 1,3, 1,4, 0,5, 2,6 }, new int[] { 0,0, 1,0, 3,1, 2,4, 0,7 }, new int[0], new int[] { 2,0, 1,5 }, true, 0, 2),
            new Entry(40, 9, 3, 2, 0, 1, new int[] { 3,1, 0,3, 3,4, 1,5, 2,5 }, new int[] { 0,0, 2,1, 3,1, 2,3, 1,4 }, new int[0], new int[] { 3,3, 0,6 }, true, 1, 7),
            new Entry(41, 10, 2, 0, 0, 2, new int[] { 1,0, 0,1, 3,2, 2,5, 2,7 }, new int[] { 0,1, 1,1, 1,2, 3,4, 2,5 }, new int[0], new int[] { 0,3, 0,7 }, true, 3, 4),
            new Entry(42, 10, 0, 0, 3, 0, new int[] { 3,0, 1,2, 3,3, 2,4, 3,5 }, new int[] { 1,0, 0,3, 0,5, 1,5, 2,6 }, new int[] { 1,4, 0,6 }, new int[] { 3,1 }, true, 2, 0),
            new Entry(43, 11, 3, 0, 0, 4, new int[] { 1,0, 3,2, 0,3, 1,5, 3,5, 2,7 }, new int[] { 2,1, 0,2, 1,3, 3,3, 2,6, 0,7 }, new int[] { 2,1 }, new int[] { 0,3 }, true, 2, 5),
            new Entry(44, 12, 0, 4, 3, 2, new int[] { 0,2, 0,5, 2,5, 3,5, 0,6, 2,7 }, new int[] { 1,1, 3,3, 3,4, 2,5, 0,6, 1,7 }, new int[] { 1,0, 3,0, 2,4 }, new int[0], false, 3, 0),
            new Entry(45, 13, 0, 0, 2, 0, new int[] { 3,1, 1,2, 1,3, 0,4, 0,5, 2,5 }, new int[] { 3,3, 0,4, 1,5, 2,5, 1,6, 2,6 }, new int[] { 3,2, 2,7 }, new int[] { 3,6 }, true, 2, 3),
        };

        [MenuItem("Tilt Mirror/Add Chapter 2 (15 levels)")]
        static void Import()
        {
            string dbPath = Folder + "/LevelDatabase.asset";
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(dbPath);
            if (db == null)
            {
                EditorUtility.DisplayDialog("Tilt Mirror",
                    "Assets/Levels/LevelDatabase.asset topilmadi.\n\nAvval: Tilt Mirror > Import Level Pack (30 levels).", "OK");
                return;
            }

            // Keep Chapter 1, drop any older Chapter 2.
            List<LevelData> keep = new List<LevelData>();
            for (int i = 0; i < db.levels.Count; i++)
            {
                LevelData old = db.levels[i];
                if (old != null && old.levelNumber < FirstNumber) keep.Add(old);
            }
            string[] oldGuids = AssetDatabase.FindAssets("t:LevelData", new string[] { Folder });
            for (int i = 0; i < oldGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(oldGuids[i]);
                LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (data != null && data.levelNumber >= FirstNumber) AssetDatabase.DeleteAsset(path);
            }

            int problems = 0;
            for (int i = 0; i < Pack.Length; i++)
            {
                Entry e = Pack[i];
                LevelData level = ScriptableObject.CreateInstance<LevelData>();
                level.levelNumber = e.Number;
                level.width = 4;
                level.height = 8;
                level.par = e.Par;
                level.sunStart = new Vector2Int(0, 7);
                level.moonStart = new Vector2Int(3, 7);
                level.sunGoal = new Vector2Int(e.SunGoalX, e.SunGoalY);
                level.moonGoal = new Vector2Int(e.MoonGoalX, e.MoonGoalY);
                level.sunWalls = ToList(e.SunWalls);
                level.moonWalls = ToList(e.MoonWalls);
                level.hasKey = true;
                level.keyOnSun = e.KeyOnSun;
                level.keyPos = new Vector2Int(e.KeyX, e.KeyY);
                level.sunLocks = ToList(e.SunLocks);
                level.moonLocks = ToList(e.MoonLocks);
                level.showHint = e.Number <= FirstNumber + 1;   // arrows on the first two key levels

                int solved = MirrorSim.Solve(level.ToSpec());
                if (solved != e.Par)
                {
                    problems++;
                    Debug.LogError("Level " + e.Number + ": kutilgan par " + e.Par + ", yechuvchi " + solved + " topdi.");
                }

                AssetDatabase.CreateAsset(level, Folder + "/Level_" + e.Number.ToString("000") + ".asset");
                keep.Add(level);
            }

            db.levels = keep;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Selection.activeObject = db;

            string message = problems == 0
                ? "Tayyor! Chapter 2: 15 ta level qo'shildi va tekshirildi (jami " + keep.Count + " ta)."
                : "Level qo'shildi, lekin " + problems + " tasida par mos kelmadi. Console'ni ko'ring.";
            Debug.Log("Tilt Mirror: " + message);
            EditorUtility.DisplayDialog("Tilt Mirror", message, "OK");
        }

        static List<Vector2Int> ToList(int[] flat)
        {
            List<Vector2Int> list = new List<Vector2Int>();
            for (int i = 0; i + 1 < flat.Length; i += 2) list.Add(new Vector2Int(flat[i], flat[i + 1]));
            return list;
        }
    }
}
