using UnityEngine;

[System.Serializable]
public class PlaceableData : ItemSpecializedData
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Barricada (solo si se pone en una puerta o ventana)")]
    [Min(1f)] public float maxDurability = 50f; //cuanto daño aguanta, igual que dice la receta
    [Min(0f)] public float buildTime = 2f; //segundos que tarda el jugador en colocarla
}
