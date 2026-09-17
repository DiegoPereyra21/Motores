using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private Inventory inventory;
    [SerializeField] private InputActionReference inventoryAction;

    private VisualElement root;

    private List<VisualElement> slots = new List<VisualElement>();

    private void OnEnable()
    {
        root = uiDocument.rootVisualElement;


        for (int i = 1; i <= 9; i++)
        {
            VisualElement slot = root.Q<VisualElement>($"Slot_{i:00}");

            if (slot != null)
            {
                slots.Add(slot);
            }
        }

        // escucha cambios
        inventory.OnInventoryChanged += RefreshUI;

        RefreshUI();

        inventoryAction.action.performed += OnInventory;
        inventoryAction.action.Enable();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshUI;
        }

        if (inventoryAction != null)
        {
            inventoryAction.action.performed -= OnInventory;
            inventoryAction.action.Disable();
        }
    }

    private void RefreshUI()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            VisualElement slot = slots[i];

            Label title = slot.Q<Label>("SlotTitle");
            Image image = slot.Q<Image>("SlotImage");
            Label count = slot.Q<Label>("SlotCount");

            if (title == null)
            {
                Debug.LogError($"[InventoryUI] falta SlotTitle en {slot.name}");
            }
            else
            {
                title.text = "";
            }

            if (image == null)
            {
                Debug.LogError($"[InventoryUI] falta SlotImage en {slot.name}");
            }
            else
            {
                image.image = null;
            }

            if (count == null)
            {
                Debug.LogError($"[InventoryUI] falta SlotCount en {slot.name}");
            }
            else
            {
                count.text = "";
            }
        }

        // rellenar los slots
        int index = 0;

        foreach (InventorySlot inventorySlot in inventory.Slots)
        {
            if (index >= slots.Count)
                break;

            VisualElement slot = slots[index];

            Label title = slot.Q<Label>("SlotTitle");
            Image image = slot.Q<Image>("SlotImage");
            Label count = slot.Q<Label>("SlotCount");

            if (inventorySlot.item == null)
            {
                index++;
                continue;
            }

            if (title != null)
                title.text = inventorySlot.item.itemName;

            if (image != null && inventorySlot.item.icon != null)
                image.image = inventorySlot.item.icon.texture;

            if (count != null)
                count.text = inventorySlot.amount.ToString();

            index++;
        }
    }

    // abrir y cerrar el inventario
    private void ToggleInventory()
    {
        VisualElement inventoryPanel = root.Q<VisualElement>("InventoryPanel");

        if (inventoryPanel == null)
            return;

        bool isVisible = inventoryPanel.style.display != DisplayStyle.None;

        inventoryPanel.style.display = isVisible
            ? DisplayStyle.None
            : DisplayStyle.Flex;
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        ToggleInventory();
    }
}