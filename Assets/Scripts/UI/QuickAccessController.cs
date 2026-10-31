
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class QuickAccessController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private InventoryUI inventoryUI;

    [Header("Configuracion")]
    [SerializeField, Min(1)] private int slotCount = 4;

    private InventorySlot[] equippedSlots;

    public int SelectedIndex { get; private set; } = 0;

    public event Action OnQuickAccessChanged;
    public event Action<int> OnSelectionChanged;

    public InventorySlot SelectedSlot => equippedSlots != null && SelectedIndex >= 0 && SelectedIndex < equippedSlots.Length ? equippedSlots[SelectedIndex] : null;
    public IReadOnlyList<InventorySlot> EquippedSlots => equippedSlots;

    private void Awake()
    {
        InitializeSlots();
    }

    private void OnEnable()
    {
        InitializeSlots();

        if (inventory != null)
            inventory.OnInventoryChanged += ValidateEquippedSlots;
    }

    private void Start()
    {
        NotifyQuickAccessChanged();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= ValidateEquippedSlots;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // evitar cambiar de objeto mientras el inventario está abierto
        if (inventoryUI != null && inventoryUI.IsInventoryOpen)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SelectSlot(0);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SelectSlot(1);

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SelectSlot(2);

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            SelectSlot(3);
    }

    private void InitializeSlots()
    {
        if (equippedSlots != null)
            return;

        slotCount = 4;
        equippedSlots = new InventorySlot[slotCount];
    }

    public InventorySlot GetEquippedSlot(int index)
    {
        InitializeSlots();

        if (index < 0 || index >= equippedSlots.Length)
            return null;

        return equippedSlots[index];
    }

    public bool EquipFromInventory(int inventoryIndex, int quickIndex)
    {
        InitializeSlots();

        if (inventory == null || inventoryIndex < 0 || inventoryIndex >= inventory.Slots.Count || quickIndex < 0 || quickIndex >= equippedSlots.Length)
        {
            return false;
        }

        InventorySlot incoming = inventory.Slots[inventoryIndex];

        if (incoming.IsEmpty || !CanEquip(incoming.item))
            return false;

        // evitar que una instancia este en dos slots
        if (Array.IndexOf(equippedSlots, incoming) >= 0)
            return false;

        InventorySlot previous = equippedSlots[quickIndex];

        // retira el objeto del inventario
        incoming = inventory.TakeSlot(inventoryIndex);

        if (incoming == null)
            return false;

        // si habia un objeto equipado lo devuelve al inventario
        if (previous != null)
        {
            if (!inventory.ReturnSlot(previous))
            {
                // si no se pudo devolver, restaura el objeto entrante
                inventory.ReturnSlot(incoming);
                return false;
            }
        }

        equippedSlots[quickIndex] = incoming;

        ValidateSelectedSlot();

        NotifyQuickAccessChanged();

        return true;
    }

    private bool CanEquip(ItemData item)
    {
        return item != null && (item.IsWeapon() || item.IsConsumable() || item.category == ItemCategory.Trap);
    }

    public bool Unequip(int quickIndex)
    {
        InitializeSlots();

        if (inventory == null || quickIndex < 0 || quickIndex >= equippedSlots.Length)
        {
            return false;
        }

        InventorySlot slot = equippedSlots[quickIndex];

        if (slot == null || slot.IsEmpty)
            return false;

        if (!inventory.ReturnSlot(slot))
        {
            Debug.LogWarning("[QuickAccess] No hay espacio para desequipar el objeto");
            return false;
        }

        equippedSlots[quickIndex] = null;

        if (SelectedIndex == quickIndex)
            ValidateSelectedSlot();

        NotifyQuickAccessChanged();

        return true;
    }

    // intercambia dos slots rapidos sin duplicar objetos
    public bool MoveQuickSlot(int fromIndex, int toIndex)
    {
        InitializeSlots();

        if (fromIndex < 0 || fromIndex >= equippedSlots.Length || toIndex < 0 || toIndex >= equippedSlots.Length || fromIndex == toIndex)
        {
            return false;
        }

        InventorySlot temp = equippedSlots[fromIndex];

        equippedSlots[fromIndex] = equippedSlots[toIndex];
        equippedSlots[toIndex] = temp;

        if (SelectedIndex == fromIndex)
            SelectSlot(toIndex);
        else if (SelectedIndex == toIndex)
            SelectSlot(fromIndex);

        NotifyQuickAccessChanged();
        return true;
    }

    public void SelectSlot(int index)
    {
        if (index < -1 || index >= equippedSlots.Length)
            return;

        if (SelectedIndex == index)
            return;

        SelectedIndex = index;
        OnSelectionChanged?.Invoke(SelectedIndex);
    }

    // limpia las referencias de objetos que fueron eliminados o consumidos
    private void ValidateEquippedSlots()
    {
        bool changed = false;

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (equippedSlots[i] != null && equippedSlots[i].IsEmpty)
            {
                equippedSlots[i] = null;
                changed = true;
            }
        }

        if (changed)
        {
            ValidateSelectedSlot();
            NotifyQuickAccessChanged();
        }
    }

    private void NotifyQuickAccessChanged()
    {
        OnQuickAccessChanged?.Invoke();
    }

    // desequipar y devolver el objeto a una posicion especifica
    public bool UnequipToInventoryIndex(int quickIndex, int inventoryIndex)
    {
        InitializeSlots();

        if (inventory == null || quickIndex < 0 || quickIndex >= equippedSlots.Length || inventoryIndex < 0 || inventoryIndex >= inventory.Slots.Count)
        {
            return false;
        }

        InventorySlot slot = equippedSlots[quickIndex];

        if (slot == null || slot.IsEmpty)
            return false;

        if (!inventory.ReturnSlot(slot, inventoryIndex))
            return false;

        equippedSlots[quickIndex] = null;

        if (SelectedIndex == quickIndex)
            ValidateSelectedSlot();

        NotifyQuickAccessChanged();

        return true;
    }

    public bool EquipToFirstEmptySlot(int inventoryIndex)
    {
        InitializeSlots();

        if (inventory == null || inventoryIndex < 0 || inventoryIndex >= inventory.Slots.Count)
            return false;

        InventorySlot incoming = inventory.Slots[inventoryIndex];

        if (incoming.IsEmpty || !CanEquip(incoming.item))
            return false;

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (equippedSlots[i] != null && !equippedSlots[i].IsEmpty)
                continue;

            return EquipFromInventory(inventoryIndex, i);
        }

        Debug.Log("[QuickAccess] No hay espacios libres.");
        return false;
    }

    private void ValidateSelectedSlot()
    {
        InventorySlot selected = GetEquippedSlot(SelectedIndex);

        if (selected != null && !selected.IsEmpty)
            return;

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (equippedSlots[i] == null || equippedSlots[i].IsEmpty)
                continue;

            SelectSlot(i);
            return;
        }

        SelectSlot(-1);
    }
}
