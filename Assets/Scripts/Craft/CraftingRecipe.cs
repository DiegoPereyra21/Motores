using UnityEngine;

[CreateAssetMenu(fileName = "CraftingRecipe", menuName = "Zombie Cabin/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string recipeName;
    public Sprite recipeImage;
    public ItemData result;
    public int resultAmount = 1;
    
    [TextArea(3, 5)]
    public string description;

    public CraftingIngredient[] ingredients;
}