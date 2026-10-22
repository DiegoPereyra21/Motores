using UnityEngine;

public enum TrapDeploymentType
{
    Place,
    Throw
}

[System.Serializable]
public class TrapData : ItemSpecializedData
{
    [Header("Tipo de uso")]
    public TrapDeploymentType deploymentType;

    // el daño en el tiempo es opcional
    [Header("Daño")]
    [Min(0)]
    public float damage = 10f;

    [Min(0)]
    public float damageOverTime = 0f;

    [Min(0)]
    public float damageDuration = 0f;

    [Header("Prefab")]
    public GameObject prefab;
}