using System;
using System.Collections.Generic;
using UnityEngine;

public class StartingInventory : MonoBehaviour
{
    [Serializable]
    private class StartingItem
    {
        public ItemData item;
        [Min(1)] public int amount = 1;
    }

    [Header("Inventario inicial")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private List<StartingItem> startingItems = new();

    private bool hasReceivedStartingItems;

    private void Start()
    {
        if (inventory == null)
            inventory = GetComponent<Inventory>();

        if (inventory == null)
        {
            Debug.LogError("[StartingInventory] No se encontro Inventory.", this);
            return;
        }

        if (hasReceivedStartingItems)
            return;

        foreach (StartingItem entry in startingItems)
        {
            if (entry.item == null || entry.amount <= 0)
                continue;

            inventory.AddItem(entry.item, entry.amount);
        }

        hasReceivedStartingItems = true;
    }
}