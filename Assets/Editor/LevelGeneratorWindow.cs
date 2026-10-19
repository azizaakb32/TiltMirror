using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TiltMirror.Core;

namespace TiltMirror.EditorTools
{
    // Tilt Mirror > Level Generator
    // Random walls + breadth-first search: keeps only solvable levels whose shortest
    // solution (par) is inside the range you choose. Always review generated levels by
    // hand: random does not mean fun. Use the output as a starting point.
    public sealed class LevelGeneratorWindow : EditorWindow
    {
        const string Folder = "Assets/Levels";

        int width = 4;
        int height = 8;
        Vector2Int sunGoal = new Vector2Int(3, 0);
        Vector2Int moonGoal = new Vector2Int(0, 0);
        int sunWallCount = 9;
        int moonWallCount = 9;
        int minPar = 7;
        int maxPar = 9;
        int count = 10;
        int firstLevelNumber = 1;
        int seed = 1;
        int maxAttempts = 200000;

        [MenuItem("Tilt Mirror/Level Generator")]
        static void Open()
        {
            GetWindow<LevelGeneratorWindow>("Level Generator");
        }

        [MenuItem("Tilt Mirror/Create Sample Levels (12, 13)")]
        static void CreateSamples()
        {
            EnsureFolder(Folder);
            for (int i = 0; i < SampleLevels.All.Length; i++)
            {
                SampleLevels.Sample s = SampleLevels.All[i];
                BoardSpec spec = SampleLevels.ToSpec(s);
                int par = MirrorSim.Solve(spec);
                SaveLevel(spec, s.Number, par);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RebuildDatabase();
        }

        [MenuItem("Tilt Mirror/Rebuild Level Database")]
        static void RebuildDatabase()
        {
            EnsureFolder(Folder);
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            List<LevelData> list = new List<LevelData>();
            for (int i = 0; i < guids.Length; i++)
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (level != null) list.Add(level);
            }
            list.Sort(delegate (LevelData a, LevelData b) { return a.levelNumber.CompareTo(b.levelNumber); });

            string dbPath = Folder + "/LevelDatabase.asset";
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(dbPath);
            if (db == null)
            {
                db = CreateInstance<LevelDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }
            db.levels = list;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Selection.activeObject = db;
            Debug.Log("Tilt Mirror: LevelDatabase now holds " + list.Count + " levels.");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Board", EditorStyles.boldLabel);
            width = EditorGUILayout.IntField("Width", width);
            height = EditorGUILayout.IntField("Height", height);
            sunGoal = EditorGUILayout.Vector2IntField("Sun goal (x, y; y=0 is top)", sunGoal);
            moonGoal = EditorGUILayout.Vector2IntField("Moon goal", moonGoal);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Difficulty", EditorStyles.boldLabel);
            sunWallCount = EditorGUILayout.IntField("Sun walls", sunWallCount);
            moonWallCount = EditorGUILayout.IntField("Moon walls", moonWallCount);
            minPar = EditorGUILayout.IntField("Min par (shortest solution)", minPar);
            maxPar = EditorGUILayout.IntField("Max par", maxPar);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output (" + Folder + ")", EditorStyles.boldLabel);
            count = EditorGUILayout.IntField("How many levels", count);
            firstLevelNumber = EditorGUILayout.IntField("First level number", firstLevelNumber);
            seed = EditorGUILayout.IntField("Random seed", seed);
            maxAttempts = EditorGUILayout.IntField("Max attempts", maxAttempts);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate")) Generate();
            if (GUILayout.Button("Rebuild Level Database")) RebuildDatabase();
        }

        void Generate()
        {
            EnsureFolder(Folder);
            System.Random rnd = new System.Random(seed);
            int made = 0;
            int attempts = 0;
            try
            {
                while (made < count && attempts < maxAttempts)
                {
                    attempts++;
                    if (attempts % 500 == 0)
                    {
                        float progress = (float)attempts / maxAttempts;
                        if (EditorUtility.DisplayCancelableProgressBar("Generating levels", made + "/" + count + " found, " + attempts + " tries", progress)) break;
                    }

                    BoardSpec spec = RandomSpec(rnd);
                    int par = MirrorSim.Solve(spec);
                    if (par < minPar || par > maxPar) continue;

                    SaveLevel(spec, firstLevelNumber + made, par);
                    made++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tilt Mirror: generated " + made + " levels in " + attempts + " attempts.");
            if (made > 0) RebuildDatabase();
        }

        BoardSpec RandomSpec(System.Random rnd)
        {
            BoardSpec spec = new BoardSpec();
            spec.Width = width;
            spec.Height = height;
            spec.SunStart = new Int2(0, height - 1);
            spec.MoonStart = new Int2(width - 1, height - 1);
            spec.SunGoal = new Int2(sunGoal.x, sunGoal.y);
            spec.MoonGoal = new Int2(moonGoal.x, moonGoal.y);

            List<Int2> sunPool = new List<Int2>();
            List<Int2> moonPool = new List<Int2>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Int2 c = new Int2(x, y);
                    if (!c.Equals(spec.SunStart) && !c.Equals(spec.SunGoal)) sunPool.Add(c);
                    if (!c.Equals(spec.MoonStart) && !c.Equals(spec.MoonGoal)) moonPool.Add(c);
                }
            }
            Pick(sunPool, sunWallCount, rnd, spec.SunWalls);
            Pick(moonPool, moonWallCount, rnd, spec.MoonWalls);
            return spec;
        }

        static void Pick(List<Int2> pool, int n, System.Random rnd, HashSet<Int2> into)
        {
            n = Mathf.Min(n, pool.Count);
            for (int i = 0; i < n; i++)
            {
                int j = i + rnd.Next(pool.Count - i);
                Int2 tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
                into.Add(pool[i]);
            }
        }

        static void SaveLevel(BoardSpec spec, int number, int par)
        {
            LevelData level = CreateInstance<LevelData>();
            level.levelNumber = number;
            level.width = spec.Width;
            level.height = spec.Height;
            level.par = par;
            level.sunStart = new Vector2Int(spec.SunStart.X, spec.SunStart.Y);
            level.moonStart = new Vector2Int(spec.MoonStart.X, spec.MoonStart.Y);
            level.sunGoal = new Vector2Int(spec.SunGoal.X, spec.SunGoal.Y);
            level.moonGoal = new Vector2Int(spec.MoonGoal.X, spec.MoonGoal.Y);
            level.sunWalls = ToSortedList(spec.SunWalls);
            level.moonWalls = ToSortedList(spec.MoonWalls);

            string path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/Level_" + number.ToString("000") + ".asset");
            AssetDatabase.CreateAsset(level, path);
        }

        static List<Vector2Int> ToSortedList(HashSet<Int2> set)
        {
            List<Vector2Int> list = new List<Vector2Int>();
            foreach (Int2 c in set) list.Add(new Vector2Int(c.X, c.Y));
            list.Sort(delegate (Vector2Int a, Vector2Int b)
            {
                return a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x);
            });
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
