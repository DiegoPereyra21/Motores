using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Zombie Cabin/Item Data")]
public class ItemData : ScriptableObject
{
    [Tooltip("Identificador único del ítem")]
    public string itemId;

    [Tooltip("Nombre visible del ítem")]
    public string itemName;

    [Tooltip("Ícono del ítem (opcional por ahora)")]
    public Sprite icon;
}