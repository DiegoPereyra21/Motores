using System;
using System.Collections.Generic;
using UnityEngine;

//literalmente un slotdel inventario, con su cantidad y item
[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int amount;

    public InventorySlot(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}

//x el momento sin interafz grafica
public class Inventory : MonoBehaviour
{
    //diccionario interno item -> slot (para acceso rápido por ítem)
    private Dictionary<ItemData, InventorySlot> slots = new Dictionary<ItemData, InventorySlot>();

    //solo lectura para q la ui pueda iterarlos luego
    public IReadOnlyCollection<InventorySlot> Slots => slots.Values;

    //cada q cambia el inventario llama al evento para q la ui cambie, cuando tengamos
    public event Action OnInventoryChanged;

    //agrega un item al inventario(la mejor forma posible)
    public void AddItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return;

        if (slots.TryGetValue(item, out InventorySlot slot))
        {
            slot.amount += amount;
        }
        else
        {
            slots[item] = new InventorySlot(item, amount);
        }

        Debug.Log($"[Inventory] Se agregó {amount}x {item.itemName}. Total: {slots[item].amount}");
        OnInventoryChanged?.Invoke();
    }

    //intentara quitar una cantida dd eitem, true o false si es q pudo
    public bool RemoveItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        if (!slots.TryGetValue(item, out InventorySlot slot) || slot.amount < amount)
        {
            Debug.Log($"[Inventory] No se pudo quitar {amount}x {item?.itemName}. Cantidad insuficiente.");
            return false;
        }

        slot.amount -= amount;

        if (slot.amount <= 0)
        {
            slots.Remove(item);
        }

        Debug.Log($"[Inventory] Se quitó {amount}x {item.itemName}. Restante: {(slots.ContainsKey(item) ? slots[item].amount : 0)}");
        OnInventoryChanged?.Invoke();
        return true;
    }

    //devuelve la cantidad actual de 1 item
    public int GetItemCount(ItemData item)
    {
        if (item == null)
            return 0;

        return slots.TryGetValue(item, out InventorySlot slot) ? slot.amount : 0;
    }

    //verifica si el inventario tiene cierta cantidad de algo
    public bool HasItem(ItemData item, int amount)
    {
        return GetItemCount(item) >= amount;
    }
}