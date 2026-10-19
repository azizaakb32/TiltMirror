using UnityEngine;

namespace TiltMirror
{
    // Minimal local save using PlayerPrefs. Swap for a proper save system later.
    public static class ProgressService
    {
        const string CurrentKey = "tm_current_level";

        public static int CurrentLevelIndex
        {
            get { return PlayerPrefs.GetInt(CurrentKey, 0); }
            set { PlayerPrefs.SetInt(CurrentKey, value); PlayerPrefs.Save(); }
        }

        public static int GetStars(int levelIndex)
        {
            return PlayerPrefs.GetInt("tm_stars_" + levelIndex, 0);
        }

        // Keeps the best result only.
        public static void SaveResult(int levelIndex, int stars)
        {
            if (stars > GetStars(levelIndex))
            {
                PlayerPrefs.SetInt("tm_stars_" + levelIndex, stars);
                PlayerPrefs.Save();
            }
        }

        public static int TotalStars(int levelCount)
        {
            int sum = 0;
            for (int i = 0; i < levelCount; i++) sum += GetStars(i);
            return sum;
        }

        // Index of the first level that has not been completed yet. Every level up to
        // and including this one is playable; later ones are locked.
        // If all levels are done, the last level is returned.
        public static int HighestUnlocked(int levelCount)
        {
            for (int i = 0; i < levelCount; i++)
            {
                if (GetStars(i) == 0) return i;
            }
            return Mathf.Max(0, levelCount - 1);
        }

        // 3 stars at par or better, 2 stars within +3 moves, otherwise 1.
        public static int StarsFor(int moves, int par)
        {
            if (moves <= par) return 3;
            if (moves <= par + 3) return 2;
            return 1;
        }
    }
}
