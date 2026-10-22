
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
    [SerializeField, Min(1)] private int slotCapacity = 30;
    
    // para manejar el movimiento de la camara con el menu abierto
    public bool IsInventoryOpen { get; private set; }

    private VisualElement root;
    private VisualElement inventoryPanel;
    private VisualElement inventoryGrid;
    private Label capacityCount;
    private Animator playerAnimator;

    private readonly List<VisualElement> slots = new();

    private void Awake()
    {
        if (playerController != null)
            playerAnimator = playerController.GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (inventoryPanel != null &&
                inventoryPanel.resolvedStyle.display != DisplayStyle.None)
            {
                ToggleInventory();
            }
        }
    }

    private void OnEnable()
    {
        if (uiDocument == null || inventory == null)
        {
            Debug.LogError("[InventoryUI] Falta asignar UIDocument o Inventory.");
            return;
        }

        root = uiDocument.rootVisualElement;
        inventoryPanel = root.Q<VisualElement>("InventoryPanel");
        inventoryGrid = root.Q<VisualElement>("InventoryGrid");
        capacityCount = root.Q<Label>("CapacityCount");

        if (inventoryGrid == null)
        {
            Debug.LogError("[InventoryUI] No se encontro InventoryGrid en el UXML.");
            return;
        }

        if (inventoryPanel != null)
            inventoryPanel.style.display = DisplayStyle.None;

        GenerateSlots();

        inventory.OnInventoryChanged += RefreshUI;
        RefreshUI();

        if (inventoryAction != null)
        {
            inventoryAction.action.performed += OnInventory;
            inventoryAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshUI;

        if (inventoryAction != null)
        {
            inventoryAction.action.performed -= OnInventory;
            inventoryAction.action.Disable();
        }
    }

    private void GenerateSlots()
    {
        slots.Clear();
        inventoryGrid.Clear();

        for (int i = 0; i < slotCapacity; i++)
        {
            VisualElement slot = new VisualElement();
            slot.name = $"Slot_{i + 1:00}";
            slot.AddToClassList("inventory-slot");

            slot.style.width = 130;
            slot.style.height = 130;
            slot.style.flexShrink = 0;

            Image image = new Image();
            image.name = "SlotImage";
            image.AddToClassList("slot-image");

            Label title = new Label();
            title.name = "SlotTitle";
            title.AddToClassList("slot-title");

            Label count = new Label();
            count.name = "SlotCount";
            count.AddToClassList("slot-count");

            slot.Add(image);
            slot.Add(title);
            slot.Add(count);

            inventoryGrid.Add(slot);
            slots.Add(slot);
        }
    }

    private void RefreshUI()
    {
        foreach (VisualElement slot in slots)
        {
            slot.Q<Label>("SlotTitle").text = "";
            slot.Q<Image>("SlotImage").image = null;
            slot.Q<Label>("SlotCount").text = "";
        }

        int index = 0;

        foreach (InventorySlot inventorySlot in inventory.Slots)
        {
            if (index >= slots.Count)
                break;

            if (inventorySlot.item != null)
            {
                VisualElement slot = slots[index];

                Label title = slot.Q<Label>("SlotTitle");
                Image image = slot.Q<Image>("SlotImage");
                Label count = slot.Q<Label>("SlotCount");

                title.text = inventorySlot.item.itemName;

                if (inventorySlot.item.icon != null)
                    image.image = inventorySlot.item.icon.texture;

                count.text = inventorySlot.amount.ToString();
            }

            index++;
        }

        if (capacityCount != null)
            capacityCount.text = $"{inventory.Slots.Count} / {slots.Count}";
    }

    private void ToggleInventory()
    {
        if (inventoryPanel == null)
            return;

        IsInventoryOpen = !IsInventoryOpen;

        inventoryPanel.style.display = IsInventoryOpen ? DisplayStyle.Flex : DisplayStyle.None;

        UpdatePlayerState();

        UnityEngine.Cursor.visible = IsInventoryOpen;
        UnityEngine.Cursor.lockState = IsInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
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
}
