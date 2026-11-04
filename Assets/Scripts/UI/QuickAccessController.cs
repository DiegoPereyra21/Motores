
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class QuickAccessController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private InventoryUI inventoryUI;

    [Header("Uso de objetos")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private InputActionReference useItemAction;

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

    private void Start()
    {
        NotifyQuickAccessChanged();
    }

    private void Update()
    {
        if (GameProgressController.Instance != null && GameProgressController.Instance.IsFinaleActive)
            return;

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

    private void OnEnable()
    {
        InitializeSlots();

        if (inventory != null)
            inventory.OnInventoryChanged += ValidateEquippedSlots;

        if (useItemAction != null)
        {
            useItemAction.action.performed += OnUseItem;
            useItemAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= ValidateEquippedSlots;

        if (useItemAction != null)
        {
            useItemAction.action.performed -= OnUseItem;
            useItemAction.action.Disable();
        }
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

    private void OnUseItem(InputAction.CallbackContext context)
    {
        if (GameProgressController.Instance != null && GameProgressController.Instance.IsFinaleActive)
            return;

        // no consumir objetos mientras el inventario esta abierto
        if (inventoryUI != null && inventoryUI.IsInventoryOpen)
            return;

        UseSelectedItem();
    }

    private bool UseSelectedItem()
    {
        InventorySlot slot = SelectedSlot;

        // check de que haya un objeto seleccionado
        if (slot == null || slot.item == null || slot.amount <= 0)
            return false;

        // check consumible
        if (!slot.item.TryGetConsumableData(out ConsumableData data))
            return false;

        if (data.effectType.ToString() != "Heal")
            return false;

        if (data.effectOverTime > 0f)
        {
            Debug.LogWarning("Este consumible utiliza curacion progresiva, todavia no implementada.");
            return false;
        }

        if (playerHealth == null)
        {
            Debug.LogError("QuickAccessController: falta asignar Player Health.");
            return false;
        }

        // no consumir si el jugador esta muerto o tiene la vida completa
        if (playerHealth.IsDead || playerHealth.Current >= playerHealth.Max)
            return false;

        // comprobar que la cantidad de curación sea valida
        if (data.effectAmount <= 0f)
            return false;

        // aplicar la curacion
        playerHealth.Heal(data.effectAmount);

        // consumir una unidad del stack
        slot.amount--;

        // si se agoto vaciar el slot
        if (slot.amount <= 0)
        {
            equippedSlots[SelectedIndex] = null;
        }

        // actualizar el HUD
        NotifyQuickAccessChanged();

        Debug.Log($"Consumible utilizado: {slot.item.itemName}");

        return true;
    }

    // reemplaza un arma equipada por su version mejorada en el mismo slot (mantiene la seleccion)
    public bool ReplaceEquippedItem(ItemData from, ItemData to)
    {
        InitializeSlots();

        if (from == null || to == null)
            return false;

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            InventorySlot slot = equippedSlots[i];

            if (slot == null || slot.IsEmpty || slot.item != from)
                continue;

            slot.SetItem(to, 1);
            NotifyQuickAccessChanged();
            return true;
        }

        return false;
    }

    public bool HasEquipped(ItemData item)
    {
        InitializeSlots();

        foreach (InventorySlot slot in equippedSlots)
            if (slot != null && !slot.IsEmpty && slot.item == item)
                return true;

        return false;
    }
}
