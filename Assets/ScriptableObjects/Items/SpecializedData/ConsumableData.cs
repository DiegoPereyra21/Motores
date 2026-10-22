using UnityEngine;

[System.Serializable]

public enum ConsumableEffectType
{
    Heal,
    RestoreStamina,
    ReduceInfection
}

public class ConsumableData : ItemSpecializedData
{
    [Header("Cantidad de usos")]
    [Min(1)]
    public int maxUses = 1;

    // el efecto en el tiempo es opcional
    [Header("Efecto")]
    public ConsumableEffectType effectType;

    [Min(0)]
    public float effectAmount = 25f;

    [Min(0)]
    public float effectOverTime = 0f;

    [Min(0)]
    public float effectDuration = 0f;

    [Header("Prefab")]
    public GameObject prefab;
}