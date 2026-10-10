using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Zombie Cabin/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Informacion general")]
    public string itemId;
    public string itemName;
    public Sprite icon;

    [Header("Stack para el inventario")]
    [Min(1)]
    public int maxStackSize = 1;

    [Header("Clasificacion")]
    public ItemOrigin origin;
    public ItemCategory category;

    [Header("Datos especializados")]
    [SerializeReference]
    public ItemSpecializedData specializedData;

    // checks de tipo de objeto
    public bool IsNone()
    {
        return category == ItemCategory.None;
    }

    public bool IsWeapon()
    {
        return category == ItemCategory.Weapon;
    }

    public bool IsConsumable()
    {
        return category == ItemCategory.Consumable;
    }

    public bool IsPlaceable()
    {
        return category == ItemCategory.Placeable;
    }

    public bool IsTrap()
    {
        return category == ItemCategory.Trap;
    }

    // obtener datos
    public bool TryGetWeaponData(out WeaponData data)
    {
        data = specializedData as WeaponData;
        return category == ItemCategory.Weapon && data != null;
    }

    public bool TryGetConsumableData(out ConsumableData data)
    {
        data = specializedData as ConsumableData;
        return category == ItemCategory.Consumable && data != null;
    }

    public bool TryGetPlaceableData(out PlaceableData data)
    {
        data = specializedData as PlaceableData;
        return category == ItemCategory.Placeable && data != null;
    }

    public bool TryGetTrapData(out TrapData data)
    {
        data = specializedData as TrapData;
        return category == ItemCategory.Trap && data != null;
    }
}

public enum ItemOrigin
{
    Raw,
    Crafted
}

public enum ItemCategory
{
    None,
    Weapon,
    Consumable,
    Placeable,
    Trap
}