using UnityEngine;

[System.Serializable]
public class WeaponData : ItemSpecializedData
{
    [Header("Daño")]
    [Min(0)]
    public float damage = 10f;

    [Header("Durabilidad")]
    [Min(1)]
    public int maxDurability = 100;

    [Header("Prefab")]
    public GameObject prefab;
}