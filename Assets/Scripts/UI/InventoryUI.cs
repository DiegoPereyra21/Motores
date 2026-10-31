
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private PlayerController playerController;

    [SerializeField] private Inventory inventory;
    [SerializeField] private InputActionReference inventoryAction;
    [SerializeField] private QuickAccessController quickAccessController;
    [SerializeField, Min(1)] private int slotCapacity = 30;

    // para manejar el movimiento de la camara con el menu abierto
    public bool IsInventoryOpen { get; private set; }

    [Header("Orden de renderizado")]
    [SerializeField] private int inventoryOpenSortOrder = 50;

    [Header("Iconos del hover")]
    [SerializeField] private Sprite eyeOpenedIcon;
    [SerializeField] private Sprite eyeClosedIcon;

    [Header("Tooltip")]
    [SerializeField] private Vector2 tooltipMouseOffset = new Vector2(18f, -18f);

    private Animator playerAnimator;
    private float originalSortOrder;

    private VisualElement root;
    private VisualElement inventoryPanel;
    private VisualElement inventoryGrid;
    private Label capacityCount;
    private Button sortNameButton;
    private Button sortAmountButton;
    
    private readonly List<VisualElement> slots = new();
    private readonly List<VisualElement> quickSlots = new();
    private int draggedSlotIndex = -1;
    private int draggedQuickSlotIndex = -1;

    private VisualElement draggedItemVisual;
    private Image draggedItemImage;
    private Label draggedItemCount;

    private Image hoverToggleImage;
    private VisualElement tooltipParent;
    private const float TooltipPadding = 8f;
    private VisualElement itemTooltip;
    private Label tooltipItemName;
    private Label tooltipCategory;
    private Label tooltipAmount;
    private Label tooltipDurability;
    private Button hoverToggleButton;
    private bool hoverInfoEnabled = true;


    private void Awake()
    {
        if (playerController != null)
            playerAnimator = playerController.GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (inventoryPanel != null && inventoryPanel.resolvedStyle.display != DisplayStyle.None)
            {
                ToggleInventory();
            }
        }

        // mover el icono sostenido junto al cursor
        if ((draggedSlotIndex >= 0 || draggedQuickSlotIndex >= 0) && draggedItemVisual != null && draggedItemVisual.style.display == DisplayStyle.Flex && Mouse.current != null && root != null)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(root.panel, mousePosition);

            draggedItemVisual.style.left = panelPosition.x - 42;
            draggedItemVisual.style.top = root.panel.visualTree.layout.height - panelPosition.y - 42;
        }

        UpdateTooltipPosition();
    }

    private void OnEnable()
    {
        if (uiDocument == null || inventory == null)
        {
            Debug.LogError("[InventoryUI] Falta asignar UIDocument o Inventory.");
            return;
        }

        originalSortOrder = uiDocument.sortingOrder;

        root = uiDocument.rootVisualElement;
        inventoryPanel = root.Q<VisualElement>("InventoryPanel");
        inventoryGrid = root.Q<VisualElement>("InventoryGrid");
        capacityCount = root.Q<Label>("CapacityCount");

        sortNameButton = root.Q<Button>("SortName");
        sortAmountButton = root.Q<Button>("SortAmount");

        itemTooltip = root.Q<VisualElement>("ItemTooltip");
        tooltipParent = itemTooltip.parent;
        tooltipItemName = root.Q<Label>("TooltipItemName");
        tooltipCategory = root.Q<Label>("TooltipCategory");
        tooltipAmount = root.Q<Label>("TooltipAmount");
        tooltipDurability = root.Q<Label>("TooltipDurability");
        hoverToggleButton = root.Q<Button>("HoverToggleButton");

        if (inventoryGrid == null)
        {
            Debug.LogError("[InventoryUI] No se encontro InventoryGrid en el UXML.");
            return;
        }

        if (inventoryPanel != null)
            inventoryPanel.style.display = DisplayStyle.None;

        slotCapacity = Mathf.Max(1, slotCapacity);

        GenerateSlots();
        GenerateQuickAccessSlots();
        CreateDraggedItemVisual();

        inventory.OnInventoryChanged += RefreshUI;

        if (quickAccessController != null)
        {
            quickAccessController.OnQuickAccessChanged += RefreshQuickAccessUI;
        }

        RefreshUI();
        RefreshQuickAccessUI();

        if (inventoryAction != null)
        {
            inventoryAction.action.performed += OnInventory;
            inventoryAction.action.Enable();
        }

        if (sortNameButton != null)
            sortNameButton.clicked += inventory.SortByName;

        if (sortAmountButton != null)
            sortAmountButton.clicked += inventory.SortByAmount;

        if (itemTooltip != null)
            itemTooltip.style.display = DisplayStyle.None;

        if (hoverToggleButton != null)
        {
            hoverToggleImage = hoverToggleButton.Q<Image>();

            hoverToggleButton.clicked += ToggleHoverInfo;
        }

        UpdateHoverToggleIcon();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshUI;

        if (quickAccessController != null)
            quickAccessController.OnQuickAccessChanged -= RefreshQuickAccessUI;
        

        if (inventoryAction != null)
        {
            inventoryAction.action.performed -= OnInventory;
            inventoryAction.action.Disable();
        }

        if (draggedItemVisual != null)
            draggedItemVisual.RemoveFromHierarchy();

        draggedItemVisual = null;
        draggedItemImage = null;
        draggedItemCount = null;
        draggedSlotIndex = -1;
        draggedQuickSlotIndex = -1;

        if (IsInventoryOpen)
        {
            IsInventoryOpen = false;
            UpdatePlayerState();

            if (uiDocument != null)
                uiDocument.sortingOrder = originalSortOrder;

            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }

        if (sortNameButton != null)
            sortNameButton.clicked -= inventory.SortByName;

        if (sortAmountButton != null)
            sortAmountButton.clicked -= inventory.SortByAmount;

        if (hoverToggleButton != null)
            hoverToggleButton.clicked -= ToggleHoverInfo;
    }

    private void GenerateSlots()
    {
        slots.Clear();
        inventoryGrid.Clear();

        for (int i = 0; i < slotCapacity; i++)
        {
            int slotIndex = i;

            VisualElement slot = new VisualElement();
            slot.name = $"Slot_{i + 1:00}";
            slot.AddToClassList("inventory-slot");

            slot.style.width = 130;
            slot.style.height = 130;
            slot.style.flexShrink = 0;

            Image image = new Image();
            image.name = "SlotImage";
            image.AddToClassList("slot-image");
            image.pickingMode = PickingMode.Ignore;

            Label title = new Label();
            title.name = "SlotTitle";
            title.AddToClassList("slot-title");
            title.pickingMode = PickingMode.Ignore;

            Label count = new Label();
            count.name = "SlotCount";
            count.AddToClassList("slot-count");
            count.pickingMode = PickingMode.Ignore;

            // muestra la durabilidad si el objeto es un arma
            Label durability = new Label();
            durability.name = "SlotDurability";
            durability.AddToClassList("slot-durability");
            durability.pickingMode = PickingMode.Ignore;

            slot.Add(image);
            slot.Add(title);
            slot.Add(count);
            slot.Add(durability);

            // seleccionar un objeto para moverlo
            slot.RegisterCallback<PointerDownEvent>(evt => OnSlotPointerDown(evt, slotIndex));

            slot.RegisterCallback<PointerEnterEvent>(evt => ShowItemTooltip(slotIndex));

            slot.RegisterCallback<PointerLeaveEvent>(evt => HideItemTooltip());

            inventoryGrid.Add(slot);
            slots.Add(slot);
        }

        // detectar donde se suelta el objeto
        root.RegisterCallback<PointerUpEvent>(OnRootPointerUp);
    }

    private void CreateDraggedItemVisual()
    {
        if (draggedItemVisual != null || root == null)
            return;

        draggedItemVisual = new VisualElement();
        draggedItemVisual.name = "DraggedItemVisual";

        draggedItemVisual.style.position = Position.Absolute;
        draggedItemVisual.style.width = 84;
        draggedItemVisual.style.height = 84;

        draggedItemVisual.style.backgroundColor = new Color(0.07f, 0.12f, 0.10f, 0.92f);

        draggedItemVisual.style.borderTopWidth = 2;
        draggedItemVisual.style.borderBottomWidth = 2;
        draggedItemVisual.style.borderLeftWidth = 2;
        draggedItemVisual.style.borderRightWidth = 2;

        Color borderColor = new Color(0.69f, 0.29f, 0.22f);

        draggedItemVisual.style.borderTopColor = borderColor;
        draggedItemVisual.style.borderBottomColor = borderColor;
        draggedItemVisual.style.borderLeftColor = borderColor;
        draggedItemVisual.style.borderRightColor = borderColor;

        draggedItemVisual.style.alignItems = Align.Center;
        draggedItemVisual.style.justifyContent = Justify.Center;

        // para que el icono flotante no intercepte los eventos del mouse
        draggedItemVisual.pickingMode = PickingMode.Ignore;

        draggedItemImage = new Image();
        draggedItemImage.style.width = 60;
        draggedItemImage.style.height = 60;
        draggedItemImage.scaleMode = ScaleMode.ScaleToFit;
        draggedItemImage.pickingMode = PickingMode.Ignore;

        draggedItemCount = new Label();
        draggedItemCount.style.position = Position.Absolute;
        draggedItemCount.style.left = 3;
        draggedItemCount.style.top = 2;
        draggedItemCount.style.color = new Color(0.86f, 0.81f, 0.69f);
        draggedItemCount.style.unityFontStyleAndWeight = FontStyle.Bold;
        draggedItemCount.pickingMode = PickingMode.Ignore;

        draggedItemVisual.Add(draggedItemImage);
        draggedItemVisual.Add(draggedItemCount);

        root.Add(draggedItemVisual);
        draggedItemVisual.style.display = DisplayStyle.None;
    }

    // seleccionar un objeto para moverlo
    private void OnSlotPointerDown(PointerDownEvent evt, int slotIndex)
    {
        if (evt.button != 0 || !IsInventoryOpen)
            return;

        if (slotIndex < 0 || slotIndex >= inventory.Slots.Count)
            return;

        InventorySlot selectedSlot = inventory.Slots[slotIndex];

        if (selectedSlot.IsEmpty)
            return;

        // Doble clic: equipar en el primer slot rápido disponible.
        if (evt.clickCount == 2)
        {
            if (quickAccessController != null)
                quickAccessController.EquipToFirstEmptySlot(slotIndex);

            evt.StopPropagation();
            return;
        }

        draggedSlotIndex = slotIndex;

        // ocultar temporalmente el objeto del slot original
        slots[slotIndex].AddToClassList("slot-dragging");
        slots[slotIndex].AddToClassList("slot-selected");

        CreateDraggedItemVisual();

        // limpiar los datos visuales anteriores antes de tomar otro objeto
        draggedItemImage.image = null;
        draggedItemCount.text = "";

        // asignar los datos del objeto seleccionado actualmente
        if (selectedSlot.item.icon != null)
            draggedItemImage.image = selectedSlot.item.icon.texture;

        if (selectedSlot.amount > 1)
            draggedItemCount.text = selectedSlot.amount.ToString();

        draggedItemVisual.style.display = DisplayStyle.Flex;

        evt.StopPropagation();
    }

    // detectar el destino cuando se suelta el objeto
    private void OnRootPointerUp(PointerUpEvent evt)
    {
        bool draggingFromInventory = draggedSlotIndex >= 0;
        bool draggingFromQuickAccess = draggedQuickSlotIndex >= 0;

        if (evt.button != 0 || (!draggingFromInventory && !draggingFromQuickAccess))
        {
            return;
        }

        int sourceIndex = draggedSlotIndex;
        int sourceQuickIndex = draggedQuickSlotIndex;

        int targetIndex = -1;
        int targetQuickIndex = -1;

        // Buscar el elemento debajo del cursor.
        VisualElement target = root.panel.Pick(evt.position);

        while (target != null)
        {
            // Comprobar slots normales del inventario.
            for (int i = 0; i < slots.Count; i++)
            {
                if (target == slots[i])
                {
                    targetIndex = i;
                    break;
                }
            }

            // Si no encontramos un slot normal, comprobar los rápidos.
            if (targetIndex < 0)
            {
                for (int i = 0; i < quickSlots.Count; i++)
                {
                    if (target == quickSlots[i])
                    {
                        targetQuickIndex = i;
                        break;
                    }
                }
            }

            if (targetIndex >= 0 ||
                targetQuickIndex >= 0 ||
                target == root)
            {
                break;
            }

            target = target.parent;
        }

        // INVENTARIO -> ACCESO RÁPIDO
        if (draggingFromInventory)
        {
            if (targetIndex >= 0 && targetIndex < inventory.Slots.Count && targetIndex != sourceIndex)
            {
                inventory.MoveItem(sourceIndex, targetIndex);
            }
            else if (targetQuickIndex >= 0 && quickAccessController != null)
            {
                quickAccessController.EquipFromInventory(sourceIndex, targetQuickIndex);
            }
        }

        // ACCESO RÁPIDO -> INVENTARIO
        else if (draggingFromQuickAccess)
        {
            if (targetIndex >= 0 && targetIndex < inventory.Slots.Count && quickAccessController != null)
            {
                // Solo permite devolverlo a un espacio vacío.
                if (inventory.Slots[targetIndex].IsEmpty)
                {
                    quickAccessController.UnequipToInventoryIndex(sourceQuickIndex, targetIndex);
                }
            }

            // ACCESO RÁPIDO -> OTRO SLOT RÁPIDO
            else if (targetQuickIndex >= 0 && targetQuickIndex != sourceQuickIndex && quickAccessController != null)
            {
                quickAccessController.MoveQuickSlot(sourceQuickIndex, targetQuickIndex);
            }
        }

        // Limpiar estados visuales del inventario.
        foreach (VisualElement slot in slots)
        {
            slot.RemoveFromClassList("slot-selected");
            slot.RemoveFromClassList("slot-dragging");
        }

        // Limpiar estados visuales del acceso rápido.
        foreach (VisualElement slot in quickSlots)
        {
            slot.RemoveFromClassList("slot-dragging");
        }

        // Ocultar el icono que sigue al cursor.
        if (draggedItemVisual != null)
            draggedItemVisual.style.display = DisplayStyle.None;

        // Restablecer ambos índices.
        draggedSlotIndex = -1;
        draggedQuickSlotIndex = -1;

        RefreshUI();
        RefreshQuickAccessUI();

        evt.StopPropagation();
    }

    private void RefreshUI()
    {
        if (inventory == null || slots.Count == 0)
            return;

        int occupiedSlots = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            VisualElement slot = slots[i];

            Label title = slot.Q<Label>("SlotTitle");
            Image image = slot.Q<Image>("SlotImage");
            Label count = slot.Q<Label>("SlotCount");
            Label durability = slot.Q<Label>("SlotDurability");

            title.text = "";
            image.image = null;
            count.text = "";
            durability.text = "";

            slot.RemoveFromClassList("slot-occupied");

            if (i >= inventory.Slots.Count)
                continue;

            InventorySlot inventorySlot = inventory.Slots[i];

            if (inventorySlot.IsEmpty)
                continue;

            occupiedSlots++;

            // mientras sostenemos el objeto, no mostrarlo en su slot
            if (i == draggedSlotIndex)
                continue;

            title.text = inventorySlot.item.itemName;

            if (inventorySlot.item.icon != null)
                image.image = inventorySlot.item.icon.texture;

            if (inventorySlot.amount > 1)
                count.text = inventorySlot.amount.ToString();

            // mostrar la durabilidad actual de cada arma
            if (inventorySlot.item.IsWeapon() && inventorySlot.item.TryGetWeaponData(out WeaponData weaponData))
            {
                durability.text = $"{inventorySlot.weaponDurability}/{weaponData.maxDurability}";
            }

            slot.AddToClassList("slot-occupied");
        }

        if (capacityCount != null)
            capacityCount.text = $"{occupiedSlots} / {slots.Count}";
    }

    private void ToggleInventory()
    {
        if (inventoryPanel == null)
            return;

        IsInventoryOpen = !IsInventoryOpen;

        inventoryPanel.style.display = IsInventoryOpen ? DisplayStyle.Flex : DisplayStyle.None;

        // priorizar el inventario mientras esta abierto
        uiDocument.sortingOrder = IsInventoryOpen ? inventoryOpenSortOrder : originalSortOrder;

        if (!IsInventoryOpen)
        {
            // cancelar cualquier objeto que estemos sosteniendo
            if (draggedSlotIndex >= 0 &&
                draggedSlotIndex < slots.Count)
            {
                slots[draggedSlotIndex].RemoveFromClassList("slot-dragging");
                slots[draggedSlotIndex].RemoveFromClassList("slot-selected");
            }

            draggedSlotIndex = -1;

            if (draggedItemVisual != null)
                draggedItemVisual.style.display = DisplayStyle.None;

            draggedQuickSlotIndex = -1;


            foreach (VisualElement slot in slots)
            {
                slot.RemoveFromClassList("slot-selected");
                slot.RemoveFromClassList("slot-dragging");
            }

            foreach (VisualElement slot in quickSlots)
            {
                slot.RemoveFromClassList("slot-dragging");
            }
        }

        UpdatePlayerState();

        UnityEngine.Cursor.visible = IsInventoryOpen;
        UnityEngine.Cursor.lockState = IsInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;

        RefreshUI();
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        if (context.performed)
            ToggleInventory();
    }

    private void UpdatePlayerState()
    {
        if (playerController != null)
        {
            playerController.enabled = !IsInventoryOpen;

            if (playerAnimator != null)
                playerAnimator.speed = IsInventoryOpen ? 0f : 1f;
        }
    }

    private void ToggleHoverInfo()
    {
        hoverInfoEnabled = !hoverInfoEnabled;

        if (!hoverInfoEnabled)
            HideItemTooltip();

        UpdateHoverToggleIcon();
    }

    private void UpdateHoverToggleIcon()
    {
        if (hoverToggleImage == null)
            return;

        hoverToggleImage.sprite = hoverInfoEnabled ? eyeOpenedIcon : eyeClosedIcon;
    }

    private void ShowItemTooltip(int slotIndex)
    {
        if (!hoverInfoEnabled || draggedSlotIndex >= 0 || draggedQuickSlotIndex >= 0 || itemTooltip == null || slotIndex < 0 || slotIndex >= inventory.Slots.Count)
        {
            return;
        }

        InventorySlot slot = inventory.Slots[slotIndex];

        if (slot.IsEmpty)
        {
            HideItemTooltip();
            return;
        }

        tooltipItemName.text = slot.item.itemName;
        tooltipCategory.text = GetTooltipCategory(slot.item);
        tooltipAmount.text = $"Cantidad: {slot.amount}";

        tooltipDurability.text = "";

        if (slot.item.IsWeapon() && slot.item.TryGetWeaponData(out WeaponData weaponData))
        {
            tooltipDurability.text = $"Durabilidad: {slot.weaponDurability}/{weaponData.maxDurability}";
        }

        itemTooltip.style.display = DisplayStyle.Flex;

        UpdateTooltipPosition();
    }

    private void HideItemTooltip()
    {
        if (itemTooltip != null)
            itemTooltip.style.display = DisplayStyle.None;
    }

    private string GetTooltipCategory(ItemData item)
    {
        if (item == null)
            return "";

        if (item.origin == ItemOrigin.Raw)
            return "MATERIAL";

        switch (item.category)
        {
            case ItemCategory.Weapon:
                return "ARMA";

            case ItemCategory.Consumable:
                return "CONSUMIBLE";

            case ItemCategory.Placeable:
                return "REFUERZO";

            case ItemCategory.Trap:

                if (item.specializedData is TrapData trapData)
                {
                    switch (trapData.deploymentType)
                    {
                        case TrapDeploymentType.Throw:
                            return "ARROJABLE";

                        case TrapDeploymentType.Place:
                            return "TRAMPA";
                    }
                }

                return "TRAMPA";

            default:
                return "OBJETO";
        }
    }

    // para setear la posicion del popup de info del inventario
    private void UpdateTooltipPosition()
    {
        if (itemTooltip == null || itemTooltip.style.display == DisplayStyle.None || root == null || root.panel == null || Mouse.current == null)
            return;
        
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(root.panel, mousePosition);

        panelPosition.y = root.panel.visualTree.layout.height - panelPosition.y;

        Vector2 localPosition = tooltipParent.WorldToLocal(panelPosition);

        float tooltipWidth = itemTooltip.resolvedStyle.width;
        float tooltipHeight = itemTooltip.resolvedStyle.height;

        float containerWidth = tooltipParent.resolvedStyle.width;
        float containerHeight = tooltipParent.resolvedStyle.height;

        float x = localPosition.x + tooltipMouseOffset.x;
        float y = localPosition.y + tooltipMouseOffset.y;

        if (x + tooltipWidth > containerWidth - TooltipPadding)
            x = localPosition.x - tooltipWidth - Mathf.Abs(tooltipMouseOffset.x);

        if (y + tooltipHeight > containerHeight - TooltipPadding)
            y = localPosition.y - tooltipHeight - Mathf.Abs(tooltipMouseOffset.y);

        x = Mathf.Clamp(x, TooltipPadding, Mathf.Max(TooltipPadding, containerWidth - tooltipWidth - TooltipPadding));

        y = Mathf.Clamp(y, TooltipPadding, Mathf.Max(TooltipPadding, containerHeight - tooltipHeight - TooltipPadding));

        itemTooltip.style.left = x;
        itemTooltip.style.top = y;
    }

    // genera los slots de acceso rapido
    private void GenerateQuickAccessSlots()
    {
        quickSlots.Clear();

        string[] slotNames =
        {
        "QuickSlot_01",
        "QuickSlot_02",
        "QuickSlot_03",
        "QuickSlot_04"
    };

        foreach (string slotName in slotNames)
        {
            VisualElement slot = root.Q<VisualElement>(slotName);

            if (slot == null)
            {
                Debug.LogError($"[InventoryUI] No se encontro {slotName} en el UXML.");
                continue;
            }

            slot.Clear();

            Image icon = new Image();
            icon.name = "QuickSlotIcon";
            icon.AddToClassList("quick-slot-icon");
            icon.pickingMode = PickingMode.Ignore;
            icon.scaleMode = ScaleMode.ScaleToFit;

            slot.Add(icon);

            quickSlots.Add(slot);

            int quickIndex = quickSlots.Count - 1;

            slot.RegisterCallback<PointerDownEvent>(evt => OnQuickSlotPointerDown(evt, quickIndex));
        }
    }

    // actualiza los slots de acceso rapido
    private void RefreshQuickAccessUI()
    {
        if (quickAccessController == null)
            return;

        for (int i = 0; i < quickSlots.Count; i++)
        {
            VisualElement slotVisual = quickSlots[i];

            Image icon = slotVisual.Q<Image>("QuickSlotIcon");

            InventorySlot equippedSlot = quickAccessController.GetEquippedSlot(i);

            icon.image = null;

            slotVisual.RemoveFromClassList("slot-occupied");

            if (equippedSlot == null || equippedSlot.IsEmpty)
                continue;

            if (equippedSlot.item.icon != null)
                icon.image = equippedSlot.item.icon.texture;

            slotVisual.AddToClassList("slot-occupied");
        }
    }

    private void OnQuickSlotPointerDown(PointerDownEvent evt, int quickIndex)
    {
        if (!IsInventoryOpen || quickAccessController == null)
            return;

        if (evt.button == 1)
        {
            quickAccessController.Unequip(quickIndex);
        }
        else if (evt.button == 0)
        {
            InventorySlot selectedSlot = quickAccessController.GetEquippedSlot(quickIndex);

            if (selectedSlot == null || selectedSlot.IsEmpty)
                return;

            draggedQuickSlotIndex = quickIndex;

            quickSlots[quickIndex].AddToClassList("slot-dragging");

            CreateDraggedItemVisual();

            draggedItemImage.image = selectedSlot.item.icon != null ? selectedSlot.item.icon.texture : null;

            draggedItemCount.text = "";

            draggedItemVisual.style.display = DisplayStyle.Flex;
        }

        evt.StopPropagation();
    }
}
