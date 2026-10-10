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

    [Header("Prefab")]
    public GameObject prefab;
}