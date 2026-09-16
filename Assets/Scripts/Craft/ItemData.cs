using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Zombie Cabin/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    public Sprite icon;

    public ItemType type;
}
public enum ItemType
{
    Raw,
    Crafted
}