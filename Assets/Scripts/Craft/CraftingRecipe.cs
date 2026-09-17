using UnityEngine;

[CreateAssetMenu(fileName = "CraftingRecipe", menuName = "Zombie Cabin/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string name;

    public Sprite recipeImage;
    public ItemData result;
    public int resultAmount = 1;

    public CraftingIngredient[] ingredients;


    [TextArea(3, 5)]
    public string description;
}