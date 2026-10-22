
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CraftingProgression", menuName = "Zombie Cabin/Crafting Progression")]
public class CraftingProgression : ScriptableObject
{
    [Header("Recetas por dia")]
    [SerializeField] private List<CraftingRecipe> day1Recipes = new List<CraftingRecipe>();
    [SerializeField] private List<CraftingRecipe> day2Recipes = new List<CraftingRecipe>();
    [SerializeField] private List<CraftingRecipe> day3Recipes = new List<CraftingRecipe>();

    public CraftingRecipe[] GetAllRecipes()
    {
        List<CraftingRecipe> recipes = new List<CraftingRecipe>();

        AddRecipes(day1Recipes, recipes);
        AddRecipes(day2Recipes, recipes);
        AddRecipes(day3Recipes, recipes);

        return recipes.ToArray();
    }

    public CraftingRecipe[] GetAvailableRecipes(int currentDay)
    {
        List<CraftingRecipe> recipes = new List<CraftingRecipe>();

        if (currentDay >= 1)
            AddRecipes(day1Recipes, recipes);

        if (currentDay >= 2)
            AddRecipes(day2Recipes, recipes);

        if (currentDay >= 3)
            AddRecipes(day3Recipes, recipes);

        return recipes.ToArray();
    }

    public bool IsRecipeUnlocked(CraftingRecipe recipe, int currentDay)
    {
        if (recipe == null)
            return false;

        bool unlockedOnDay1 = currentDay >= 1 && day1Recipes.Contains(recipe);
        bool unlockedOnDay2 = currentDay >= 2 && day2Recipes.Contains(recipe);
        bool unlockedOnDay3 = currentDay >= 3 && day3Recipes.Contains(recipe);

        return unlockedOnDay1 || unlockedOnDay2 || unlockedOnDay3;
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
