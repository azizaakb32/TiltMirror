using UnityEditor;
using UnityEngine;

namespace TiltMirror.EditorTools
{
    // Test helpers. They only change the saved progress (PlayerPrefs) on this computer.
    public static class DevTools
    {
        [MenuItem("Tilt Mirror/Test: Unlock All Levels")]
        static void UnlockAll()
        {
            for (int i = 0; i < 200; i++)
            {
                if (PlayerPrefs.GetInt("tm_stars_" + i, 0) == 0) PlayerPrefs.SetInt("tm_stars_" + i, 1);
            }
            PlayerPrefs.Save();
            Debug.Log("Tilt Mirror: barcha levellar ochildi (test uchun). Qaytarish: Tilt Mirror > Test: Clear Progress.");
        }

        [MenuItem("Tilt Mirror/Test: Clear Progress")]
        static void ClearProgress()
        {
            PlayerPrefs.DeleteKey("tm_current_level");
            for (int i = 0; i < 200; i++) PlayerPrefs.DeleteKey("tm_stars_" + i);
            PlayerPrefs.Save();
            Debug.Log("Tilt Mirror: progress tozalandi.");
        }
    }
}
