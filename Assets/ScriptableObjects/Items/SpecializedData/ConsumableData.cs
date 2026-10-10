using UnityEngine;

[System.Serializable]
public class ConsumableData : ItemSpecializedData
{
    [Header("Cantidad de usos")]
    [Min(1)]
    public int maxUses = 1;

    [Header("Prefab")]
    public GameObject prefab;
}