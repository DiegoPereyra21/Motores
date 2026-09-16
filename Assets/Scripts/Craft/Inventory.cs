using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa un slot del inventario: un ítem y su cantidad.
/// </summary>
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

/// <summary>
/// Inventario del Player. Maneja ítems y cantidades, sin interfaz gráfica.
/// </summary>
public class Inventory : MonoBehaviour
{
    // Diccionario interno: item -> slot (para acceso rápido por ítem)
    private Dictionary<ItemData, InventorySlot> slots = new Dictionary<ItemData, InventorySlot>();

    /// <summary>
    /// Lista de solo lectura de los slots actuales, para que la UI pueda iterarlos.
    /// </summary>
    public IReadOnlyCollection<InventorySlot> Slots => slots.Values;

    // Evento que se dispara cada vez que el inventario cambia (para futura UI)
    public event Action OnInventoryChanged;

    /// <summary>
    /// Agrega una cantidad de un ítem al inventario.
    /// </summary>
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

    /// <summary>
    /// Intenta quitar una cantidad de un ítem. Devuelve true si pudo quitarlo.
    /// </summary>
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

    /// <summary>
    /// Devuelve la cantidad actual de un ítem en el inventario.
    /// </summary>
    public int GetItemCount(ItemData item)
    {
        if (item == null)
            return 0;

        return slots.TryGetValue(item, out InventorySlot slot) ? slot.amount : 0;
    }

    /// <summary>
    /// Verifica si el inventario tiene al menos cierta cantidad de un ítem.
    /// </summary>
    public bool HasItem(ItemData item, int amount)
    {
        return GetItemCount(item) >= amount;
    }
}