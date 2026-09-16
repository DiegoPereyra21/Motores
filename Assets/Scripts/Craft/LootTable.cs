using System;
using System.Collections.Generic;
using UnityEngine;

//resultado de un drop, q da un item y su cantidad
[Serializable]
public struct ItemStack
{
    public ItemData item;
    public int amount;

    public ItemStack(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}

//q item dropea, chance y cantidad de la misma
[Serializable]
public class LootEntry
{
    public ItemData item;

    [Range(0f, 100f)]
    public float dropChance;

    public int minAmount;
    public int maxAmount;
}

//tabla de que items pueden dropear y q cantidad/probabilidad
[CreateAssetMenu(fileName = "NewLootTable", menuName = "Zombie Cabin/Loot Table")]
public class LootTable : ScriptableObject
{
    [SerializeField] private List<LootEntry> lootEntries;

    //devuelve los items q salieron sorteados
    // drop calculado para los zombies muertos
    public List<ItemStack> RollRandomLoot()
    {
        List<ItemStack> result = new List<ItemStack>();

        foreach (LootEntry entry in lootEntries)
        {
            if (entry.item == null)
                continue;

            float roll = UnityEngine.Random.Range(0f, 100f);

            if (roll <= entry.dropChance)
            {
                int amount = UnityEngine.Random.Range(entry.minAmount, entry.maxAmount + 1);

                if (amount > 0)
                {
                    result.Add(new ItemStack(entry.item, amount));
                }
            }
        }

        return result;
    }

    // roll entre items con una chance de 100% de que dropee algo
    public List<ItemStack> RollGuaranteedLoot()
    {
        List<ItemStack> result = new List<ItemStack>();

        foreach (LootEntry entry in lootEntries)
        {
            if (entry.item == null)
                continue;

            int amount = UnityEngine.Random.Range(entry.minAmount, entry.maxAmount + 1);

            if (amount > 0)
            {
                result.Add(new ItemStack(entry.item, amount));
            }
        }

        return result;
    }
}