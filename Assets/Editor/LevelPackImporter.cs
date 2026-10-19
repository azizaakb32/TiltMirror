using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TiltMirror.Core;

namespace TiltMirror.EditorTools
{
    // Menu: Tilt Mirror > Import Level Pack (30 levels)
    // Creates 30 ready levels (par 3 to 14, difficulty rises step by step), puts them
    // in Assets/Levels and fills the LevelDatabase. Every level was checked with the
    // solver before it was added, and is checked again here during the import.
    public static class LevelPackImporter
    {
        const string Folder = "Assets/Levels";

        struct Entry
        {
            public int Number;
            public int Par;
            public int SunGoalX, SunGoalY, MoonGoalX, MoonGoalY;
            public int[] SunWalls;
            public int[] MoonWalls;

            public Entry(int number, int par, int sgx, int sgy, int mgx, int mgy, int[] sunWalls, int[] moonWalls)
            {
                Number = number;
                Par = par;
                SunGoalX = sgx;
                SunGoalY = sgy;
                MoonGoalX = mgx;
                MoonGoalY = mgy;
                SunWalls = sunWalls;
                MoonWalls = moonWalls;
            }
        }

        // x,y pairs. y = 0 is the top row. Sun starts at (0,7), Moon at (3,7).
        static readonly Entry[] Pack = new Entry[]
        {
            new Entry(1, 3, 1, 0, 2, 0,
                new int[] { 0,0, 2,3, 0,6 },
                new int[] { 3,0, 1,6, 2,6 }),
            new Entry(2, 3, 1, 0, 0, 1,
                new int[] { 3,1, 0,3, 2,4 },
                new int[] { 0,0, 1,2, 3,5 }),
            new Entry(3, 4, 1, 0, 2, 0,
                new int[] { 3,0, 0,1, 1,4, 2,5 },
                new int[] { 0,2, 3,3, 3,5, 2,6 }),
            new Entry(4, 4, 2, 0, 1, 2,
                new int[] { 1,0, 1,2, 0,3, 2,5 },
                new int[] { 0,1, 2,2, 3,3, 1,7 }),
            new Entry(5, 5, 0, 4, 1, 0,
                new int[] { 2,2, 0,3, 2,3, 1,7, 3,7 },
                new int[] { 2,0, 3,2, 2,4, 3,5, 2,7 }),
            new Entry(6, 5, 3, 0, 1, 0,
                new int[] { 1,1, 2,1, 3,5, 0,6, 3,7 },
                new int[] { 0,0, 1,2, 1,5, 0,6, 2,6 }),
            new Entry(7, 5, 3, 1, 0, 0,
                new int[] { 3,0, 2,2, 1,3, 0,5, 1,5 },
                new int[] { 1,0, 0,4, 2,4, 1,7, 2,7 }),
            new Entry(8, 6, 3, 3, 0, 0,
                new int[] { 2,0, 0,1, 2,1, 3,1, 0,2, 1,5 },
                new int[] { 2,2, 3,2, 2,3, 3,4, 2,6, 2,7 }),
            new Entry(9, 6, 3, 2, 3, 0,
                new int[] { 1,0, 3,1, 2,2, 0,3, 3,6, 1,7 },
                new int[] { 0,0, 3,2, 2,3, 1,5, 3,5, 2,6 }),
            new Entry(10, 6, 0, 0, 2, 2,
                new int[] { 1,3, 1,4, 2,4, 3,4, 1,5, 1,6 },
                new int[] { 1,1, 3,2, 2,3, 3,3, 2,5, 0,6 }),
            new Entry(11, 7, 3, 1, 2, 2,
                new int[] { 0,0, 2,0, 1,1, 3,2, 1,3, 2,6, 2,7 },
                new int[] { 2,0, 2,1, 0,2, 1,2, 1,3, 0,5, 3,6 }),
            new Entry(12, 7, 2, 2, 3, 1,
                new int[] { 1,0, 3,1, 1,2, 2,3, 0,6, 3,6, 2,7 },
                new int[] { 2,0, 3,0, 0,1, 2,1, 0,2, 2,3, 2,7 }),
            new Entry(13, 7, 2, 4, 2, 0,
                new int[] { 3,0, 0,1, 0,2, 3,3, 1,4, 2,5, 0,6 },
                new int[] { 3,0, 0,1, 3,1, 2,2, 2,4, 3,4, 1,6 }),
            new Entry(14, 8, 3, 0, 1, 2,
                new int[] { 1,0, 2,1, 0,3, 1,3, 3,4, 1,5, 1,6, 3,6 },
                new int[] { 3,0, 3,1, 0,2, 2,3, 0,4, 3,5, 0,6, 1,6 }),
            new Entry(15, 8, 2, 1, 0, 0,
                new int[] { 2,0, 3,0, 1,1, 3,2, 3,3, 0,4, 0,5, 3,6 },
                new int[] { 0,1, 3,1, 1,3, 3,3, 0,4, 3,4, 0,5, 2,7 }),
            new Entry(16, 8, 0, 2, 1, 1,
                new int[] { 0,0, 0,1, 1,2, 3,3, 0,4, 2,4, 3,5, 2,7 },
                new int[] { 1,0, 3,0, 0,1, 2,1, 3,2, 0,3, 0,4, 1,5 }),
            new Entry(17, 9, 0, 1, 3, 0,
                new int[] { 0,0, 3,0, 2,1, 1,2, 2,3, 2,5, 3,6, 1,7 },
                new int[] { 0,1, 2,2, 3,2, 1,4, 3,4, 2,5, 3,5, 0,7 }),
            new Entry(18, 9, 0, 3, 3, 2,
                new int[] { 0,0, 0,1, 2,1, 1,3, 2,3, 0,4, 1,4, 3,5, 2,7 },
                new int[] { 3,0, 1,2, 1,3, 2,3, 3,3, 3,5, 0,7, 1,7, 2,7 }),
            new Entry(19, 9, 3, 4, 3, 2,
                new int[] { 0,0, 3,0, 2,1, 0,2, 0,3, 2,3, 3,3, 1,4, 2,6 },
                new int[] { 0,1, 3,1, 0,2, 2,2, 1,4, 0,5, 3,5, 2,6, 3,6 }),
            new Entry(20, 10, 2, 3, 3, 4,
                new int[] { 3,0, 3,1, 1,2, 2,2, 1,4, 1,5, 3,5, 3,6, 3,7 },
                new int[] { 0,1, 2,1, 0,2, 3,2, 1,3, 3,3, 0,4, 2,5, 3,5 }),
            new Entry(21, 10, 3, 2, 3, 2,
                new int[] { 1,0, 3,0, 2,1, 3,1, 1,2, 0,4, 1,4, 2,6, 2,7 },
                new int[] { 3,0, 0,1, 1,1, 3,1, 1,3, 2,4, 3,4, 0,5, 0,7 }),
            new Entry(22, 10, 0, 0, 2, 2,
                new int[] { 0,1, 2,1, 3,1, 0,2, 3,2, 2,3, 2,5, 3,5, 1,6 },
                new int[] { 0,0, 3,2, 0,3, 1,3, 3,3, 0,5, 3,5, 0,7, 1,7 }),
            new Entry(23, 11, 0, 4, 1, 1,
                new int[] { 0,0, 2,0, 3,1, 1,2, 3,2, 0,3, 3,4, 3,6, 1,7, 2,7 },
                new int[] { 0,0, 1,0, 0,1, 2,1, 2,2, 3,3, 1,4, 3,5, 1,6, 2,7 }),
            new Entry(24, 11, 3, 4, 1, 4,
                new int[] { 0,0, 1,2, 1,3, 3,3, 0,4, 1,4, 0,5, 1,5, 2,7, 3,7 },
                new int[] { 1,1, 0,2, 1,2, 1,3, 3,3, 2,5, 0,6, 0,7, 1,7, 2,7 }),
            new Entry(25, 11, 1, 0, 1, 3,
                new int[] { 0,0, 2,0, 0,1, 1,3, 1,4, 0,5, 3,5, 3,6, 1,7, 2,7 },
                new int[] { 0,0, 1,0, 3,0, 2,1, 1,2, 3,3, 2,5, 0,6, 2,6, 2,7 }),
            new Entry(26, 12, 3, 2, 1, 4,
                new int[] { 3,0, 0,1, 1,1, 2,1, 3,1, 2,2, 1,3, 3,4, 1,5, 0,6 },
                new int[] { 1,0, 2,0, 1,1, 2,1, 0,3, 1,3, 3,4, 0,5, 0,6, 1,7 }),
            new Entry(27, 12, 2, 4, 0, 0,
                new int[] { 0,0, 2,0, 3,0, 0,2, 2,2, 3,2, 3,3, 1,4, 0,5, 1,6 },
                new int[] { 1,0, 2,0, 3,0, 2,1, 0,2, 3,3, 0,4, 3,5, 1,7, 2,7 }),
            new Entry(28, 13, 3, 4, 0, 4,
                new int[] { 2,0, 3,0, 0,2, 3,2, 0,3, 1,3, 0,4, 2,5, 3,5, 2,6 },
                new int[] { 0,0, 1,0, 3,0, 2,1, 0,2, 3,2, 1,3, 2,5, 2,6, 0,7 }),
            new Entry(29, 13, 1, 2, 3, 3,
                new int[] { 2,0, 3,0, 1,1, 2,1, 3,1, 0,2, 0,3, 3,3, 0,4, 2,6 },
                new int[] { 0,1, 1,1, 2,1, 3,2, 0,3, 2,4, 2,5, 3,5, 0,7, 1,7 }),
            new Entry(30, 14, 2, 0, 3, 2,
                new int[] { 0,0, 1,0, 3,1, 0,2, 1,2, 1,4, 3,4, 1,5, 3,5, 2,7 },
                new int[] { 3,0, 1,1, 3,1, 1,2, 0,3, 2,3, 0,4, 2,4, 2,5, 1,6 }),
        };

        [MenuItem("Tilt Mirror/Import Level Pack (30 levels)")]
        static void Import()
        {
            bool ok = EditorUtility.DisplayDialog("Tilt Mirror",
                "Assets/Levels ichidagi eski levellar o'chiriladi va o'rniga 30 ta yangi level yaratiladi.\n\nDavom etamizmi?",
                "Ha", "Bekor qilish");
            if (!ok) return;

            EnsureFolder(Folder);

            // Remove old levels (for example the two samples) so nothing is duplicated.
            string[] oldGuids = AssetDatabase.FindAssets("t:LevelData", new string[] { Folder });
            for (int i = 0; i < oldGuids.Length; i++)
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(oldGuids[i]));

            List<LevelData> created = new List<LevelData>();
            int problems = 0;

            for (int i = 0; i < Pack.Length; i++)
            {
                Entry e = Pack[i];

                BoardSpec spec = new BoardSpec();
                spec.Width = 4;
                spec.Height = 8;
                spec.SunStart = new Int2(0, 7);
                spec.MoonStart = new Int2(3, 7);
                spec.SunGoal = new Int2(e.SunGoalX, e.SunGoalY);
                spec.MoonGoal = new Int2(e.MoonGoalX, e.MoonGoalY);
                AddWalls(spec.SunWalls, e.SunWalls);
                AddWalls(spec.MoonWalls, e.MoonWalls);

                int solved = MirrorSim.Solve(spec);
                if (solved != e.Par)
                {
                    problems++;
                    Debug.LogError("Level " + e.Number + ": kutilgan par " + e.Par + ", yechuvchi " + solved + " topdi.");
                }

                LevelData level = ScriptableObject.CreateInstance<LevelData>();
                level.levelNumber = e.Number;
                level.width = spec.Width;
                level.height = spec.Height;
                level.par = e.Par;
                level.sunStart = new Vector2Int(0, 7);
                level.moonStart = new Vector2Int(3, 7);
                level.sunGoal = new Vector2Int(e.SunGoalX, e.SunGoalY);
                level.moonGoal = new Vector2Int(e.MoonGoalX, e.MoonGoalY);
                level.sunWalls = ToList(e.SunWalls);
                level.moonWalls = ToList(e.MoonWalls);

                AssetDatabase.CreateAsset(level, Folder + "/Level_" + e.Number.ToString("000") + ".asset");
                created.Add(level);
            }

            AssetDatabase.SaveAssets();

            string dbPath = Folder + "/LevelDatabase.asset";
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<LevelDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }
            db.levels = created;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            // Old saved progress belonged to the old levels, so clear it.
            PlayerPrefs.DeleteKey("tm_current_level");
            for (int i = 0; i < 200; i++) PlayerPrefs.DeleteKey("tm_stars_" + i);
            PlayerPrefs.Save();

            Selection.activeObject = db;
            string message = problems == 0
                ? "Tayyor! 30 ta level yaratildi va tekshirildi."
                : "Level yaratildi, lekin " + problems + " tasida par mos kelmadi. Console'ni ko'ring.";
            Debug.Log("Tilt Mirror: " + message);
            EditorUtility.DisplayDialog("Tilt Mirror", message, "OK");
        }

        static void AddWalls(HashSet<Int2> into, int[] flat)
        {
            for (int i = 0; i + 1 < flat.Length; i += 2) into.Add(new Int2(flat[i], flat[i + 1]));
        }

        static List<Vector2Int> ToList(int[] flat)
        {
            List<Vector2Int> list = new List<Vector2Int>();
            for (int i = 0; i + 1 < flat.Length; i += 2) list.Add(new Vector2Int(flat[i], flat[i + 1]));
            return list;
        }

        static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
