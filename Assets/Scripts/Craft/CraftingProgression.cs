using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CraftingProgression", menuName = "Zombie Cabin/Crafting Progression")]
public class CraftingProgression : ScriptableObject
{
    [Header("Recetas por dia")]
    [SerializeField] private List<CraftingRecipe> day1Recipes = new List<CraftingRecipe>();
    [SerializeField] private List<CraftingRecipe> day2Recipes = new List<CraftingRecipe>();
    [SerializeField] private List<CraftingRecipe> day3Recipes = new List<CraftingRecipe>();

    public CraftingRecipe[] GetAvailableRecipes(int currentDay)
    {
        List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();

        if (currentDay >= 1)
            AddRecipes(day1Recipes, availableRecipes);

        if (currentDay >= 2)
            AddRecipes(day2Recipes, availableRecipes);

        if (currentDay >= 3)
            AddRecipes(day3Recipes, availableRecipes);

        return availableRecipes.ToArray();
    }

    private void AddRecipes(List<CraftingRecipe> source, List<CraftingRecipe> destination)
    {
        foreach (CraftingRecipe recipe in source)
        {
            if (recipe != null && !destination.Contains(recipe))
                destination.Add(recipe);
        }
    }
}