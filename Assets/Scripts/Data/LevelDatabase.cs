using System.Collections.Generic;
using UnityEngine;

namespace TiltMirror
{
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "Tilt Mirror/Level Database")]
    public sealed class LevelDatabase : ScriptableObject
    {
        public List<LevelData> levels = new List<LevelData>();

        public int Count { get { return levels.Count; } }

        public LevelData Get(int index)
        {
            return levels[Mathf.Clamp(index, 0, levels.Count - 1)];
        }
    }
}
