
using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ZombieSpawnTable",
    menuName = "Zombie Cabin/Enemies Spawn Table"
)]
public class ZombieSpawnTable : ScriptableObject
{
    [Serializable]
    public class DayEntry
    {
        [Min(1)]
        public int day = 1;

        [Min(0)]
        public int maxEnemies = 5;
    }

    [Header("Configuracion por dia")]
    [SerializeField] private DayEntry[] days;

    public int GetMaxEnemies(int day)
    {
        foreach (DayEntry entry in days)
        {
            if (entry != null && entry.day == day)
                return entry.maxEnemies;
        }

        return 0;
    }
}
