
using System;
using System.Collections.Generic;
using UnityEngine;

// Un slot puede contener un stack de objetos comunes
// o una instancia individual de un arma.
[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int amount;

    // estado individual del arma
    public int weaponDurability;

    public bool IsEmpty => item == null || amount <= 0;

    public InventorySlot(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
        weaponDurability = GetMaxWeaponDurability(item);
    }

    public void Clear()
    {
        item = null;
        amount = 0;
        weaponDurability = 0;
    }

    public void SetItem(ItemData newItem, int newAmount)
    {
        item = newItem;
        amount = newAmount;
        weaponDurability = GetMaxWeaponDurability(newItem);
    }

    private static int GetMaxWeaponDurability(ItemData item)
    {
        if (item != null && item.TryGetWeaponData(out WeaponData data))
            return Mathf.Max(1, data.maxDurability);

        return 0;
    }
}

public class Inventory : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField, Min(1)]
    private int capacity = 30;

    private readonly List<InventorySlot> slots = new();

    public IReadOnlyList<InventorySlot> Slots => slots;

    public int Capacity => slots.Count;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        while (slots.Count < capacity)
            slots.Add(new InventorySlot(null, 0));

        if (slots.Count > capacity)
            slots.RemoveRange(capacity, slots.Count - capacity);
    }

    public void AddItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return;

        InitializeSlots();

        int remaining = amount;

        if (item.IsWeapon())
        {
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (!slots[i].IsEmpty)
                    continue;

                slots[i].SetItem(item, 1);
                remaining--;
            }
        }
        else
        {
            int maxStack = Mathf.Max(1, item.maxStackSize);

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];

                if (slot.IsEmpty || slot.item != item)
                    continue;

                int available = maxStack - slot.amount;

                if (available <= 0)
                    continue;

                int added = Mathf.Min(available, remaining);
                slot.amount += added;
                remaining -= added;
            }

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (!slots[i].IsEmpty)
                    continue;

                int added = Mathf.Min(maxStack, remaining);
                slots[i].SetItem(item, added);
                remaining -= added;
            }
        }

        int addedTotal = amount - remaining;

        if (addedTotal > 0)
        {
            Debug.Log(
                $"[Inventory] Se agrego {addedTotal}x {item.itemName}. " + (remaining > 0 ? $"Inventario lleno: faltaron {remaining}." : ""));

            OnInventoryChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[Inventory] No hay espacio para {item.itemName}.");
        }
    }

    public bool RemoveItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0 || !HasItem(item, amount))
            return false;

        int remaining = amount;

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            InventorySlot slot = slots[i];

            if (slot.IsEmpty || slot.item != item)
                continue;

            int removed = Mathf.Min(slot.amount, remaining);
            slot.amount -= removed;
            remaining -= removed;

            if (slot.amount <= 0)
                slot.Clear();
        }

        OnInventoryChanged?.Invoke();

        return true;
    }

    public int GetItemCount(ItemData item)
    {
        if (item == null)
            return 0;

        int total = 0;

        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty && slot.item == item)
                total += slot.amount;
        }

        return total;
    }

    public bool HasItem(ItemData item, int amount)
    {
        return amount >= 0 && GetItemCount(item) >= amount;
    }

    // para mover o intercambiar items en el inventario
    public bool MoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= slots.Count ||
            toIndex < 0 || toIndex >= slots.Count ||
            fromIndex == toIndex)
        {
            return false;
        }

        InventorySlot from = slots[fromIndex];
        InventorySlot to = slots[toIndex];

        if (from.IsEmpty)
            return false;

        // apilar si ambos slots tienen el mismo objeto y no es un arma
        if (!to.IsEmpty && from.item == to.item && !from.item.IsWeapon())
        {
            int maxStack = Mathf.Max(1, from.item.maxStackSize);
            int space = maxStack - to.amount;

            if (space > 0)
            {
                int moved = Mathf.Min(space, from.amount);

                to.amount += moved;
                from.amount -= moved;

                if (from.amount <= 0)
                    from.Clear();

                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        // intercambia las posiciones conservando la durabilidad
        slots[fromIndex] = to;
        slots[toIndex] = from;

        OnInventoryChanged?.Invoke();
        return true;
    }

    // ordena objetos alfabeticamente
    public void SortByName()
    {
        slots.Sort((a, b) =>
        {
            // los slots vacios siempre quedan al final
            if (a.IsEmpty && b.IsEmpty)
                return 0;

            if (a.IsEmpty)
                return 1;

            if (b.IsEmpty)
                return -1;

            int nameComparison = string.Compare(a.item.itemName, b.item.itemName, StringComparison.OrdinalIgnoreCase);

            if (nameComparison != 0)
                return nameComparison;

            return a.amount.CompareTo(b.amount);
        });

        OnInventoryChanged?.Invoke();
    }


    // ordena los objetos por cantidad descendente
    public void SortByAmount()
    {
        slots.Sort((a, b) =>
        {
            if (a.IsEmpty && b.IsEmpty)
                return 0;

            if (a.IsEmpty)
                return 1;

            if (b.IsEmpty)
                return -1;

            int amountComparison = b.amount.CompareTo(a.amount);

            if (amountComparison != 0)
                return amountComparison;

            return string.Compare(a.item.itemName, b.item.itemName, StringComparison.OrdinalIgnoreCase);
        });

        OnInventoryChanged?.Invoke();
    }


    // reduce la durabilidad de un arma ubicada en un slot
    public bool ReduceWeaponDurability(int slotIndex, int damage)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count || damage <= 0)
            return false;

        InventorySlot slot = slots[slotIndex];

        if (slot.IsEmpty || !slot.item.IsWeapon())
            return false;

        slot.weaponDurability = Mathf.Max(0, slot.weaponDurability - damage);

        // romper un arma la elimina
        if (slot.weaponDurability <= 0)
            slot.Clear();

        OnInventoryChanged?.Invoke();
        return true;
    }

    // retira una instancia del bolso y deja libre su posicion
    public InventorySlot TakeSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return null;

        InventorySlot slot = slots[slotIndex];

        if (slot.IsEmpty)
            return null;

        slots[slotIndex] = new InventorySlot(null, 0);

        OnInventoryChanged?.Invoke();

        return slot;
    }

    // devuelve una instancia al primer espacio disponible
    public bool ReturnSlot(InventorySlot slot, int preferredIndex = -1)
    {
        if (slot == null || slot.IsEmpty)
            return false;

        // intentar devolverlo a una posicion especifica
        if (preferredIndex >= 0)
        {
            if (preferredIndex >= slots.Count || !slots[preferredIndex].IsEmpty)
                return false;

            slots[preferredIndex] = slot;
            OnInventoryChanged?.Invoke();
            return true;
        }

        // devolverlo al primer espacio disponible
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty)
                continue;

            slots[i] = slot;
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }
}
