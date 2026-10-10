using UnityEngine;
using UnityEngine.UIElements;

public class CraftingUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private CraftTable craftTable;
    [SerializeField] private Inventory inventory;

    private VisualElement root;
    private VisualElement craftMenu;

    private CraftingRecipe[] availableRecipes;
    private CraftingRecipe selectedRecipe;
    private Button craftButton;
    private Label adviceLabel;

    private Button[] recipeButtons = new Button[3];
    private System.Action[] recipeButtonActions = new System.Action[3];

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[CraftingUI] No se asignó UIDocument.");
            return;
        }

        root = uiDocument.rootVisualElement;
        craftMenu = root.Q<VisualElement>("CraftMenu");

        if (craftMenu == null)
        {
            Debug.LogError("[CraftingUI] No se encontró CraftMenu.");
            return;
        }

        adviceLabel = root.Q<Label>("AdviceLabel");

        // botones de recetas
        for (int i = 0; i < recipeButtons.Length; i++)
        {
            recipeButtons[i] = craftMenu.Q<Button>($"RecipeSlot_{i + 1:00}");

            if (recipeButtons[i] != null)
            {
                int recipeIndex = i;

                recipeButtonActions[i] = () => SelectRecipe(recipeIndex);
                recipeButtons[i].clicked += recipeButtonActions[i];
            }
        }

        // boton de cerrar
        Button closeButton = craftMenu.Q<Button>("CloseButton");

        if (closeButton != null)
            closeButton.clicked += OnCloseButtonClicked;

        // craft
        craftButton = craftMenu.Q<Button>("CraftButton");

        if (craftButton != null)
        {
            craftButton.clicked += OnCraftButtonClicked;
            craftButton.SetEnabled(false);
        }

        if (inventory != null)
            inventory.OnInventoryChanged += UpdateCraftButton;

        if (adviceLabel != null)
            adviceLabel.style.display = DisplayStyle.None;

        Hide();
    }

    private void OnDisable()
    {
        for (int i = 0; i < recipeButtons.Length; i++)
        {
            if (recipeButtons[i] != null && recipeButtonActions[i] != null)
            {
                recipeButtons[i].clicked -= recipeButtonActions[i];
                recipeButtonActions[i] = null;
            }

            recipeButtons[i] = null;
        }

        Button closeButton = craftMenu?.Q<Button>("CloseButton");

        if (closeButton != null)
            closeButton.clicked -= OnCloseButtonClicked;

        if (craftButton != null)
            craftButton.clicked -= OnCraftButtonClicked;

        if (inventory != null)
            inventory.OnInventoryChanged -= UpdateCraftButton;
    }

    public void Show(CraftingRecipe[] recipes)
    {
        if (craftMenu == null)
            return;

        availableRecipes = recipes;
        selectedRecipe = null;

        LoadRecipes();
        ClearRecipeInfo();

        craftMenu.style.display = DisplayStyle.Flex;

        if (craftButton != null)
            craftButton.SetEnabled(false);

        if (adviceLabel != null)
            adviceLabel.style.display = DisplayStyle.None;
    }

    public void Hide()
    {
        if (craftMenu == null)
            return;

        craftMenu.style.display = DisplayStyle.None;
    }

    private void OnCloseButtonClicked()
    {
        if (craftTable != null)
            craftTable.CloseCrafting();
    }

    private void LoadRecipes()
    {
        // limpiar los slots
        for (int i = 0; i < recipeButtons.Length; i++)
        {
            Button recipeButton = recipeButtons[i];

            if (recipeButton == null)
                continue;

            Image image = recipeButton.Q<Image>("RecipeImage");

            if (image != null)
                image.image = null;

            recipeButton.RemoveFromClassList("selected");
        }

        if (availableRecipes == null)
            return;

        for (int i = 0; i < availableRecipes.Length && i < recipeButtons.Length; i++)
        {
            CraftingRecipe recipe = availableRecipes[i];

            if (recipe == null || recipe.result == null)
                continue;

            Button recipeButton = recipeButtons[i];

            if (recipeButton == null)
                continue;

            Image image = recipeButton.Q<Image>("RecipeImage");

            if (image != null && recipe.result.icon != null)
                image.image = recipe.result.icon.texture;

            recipeButton.tooltip = recipe.result.itemName;
        }
    }

    private void SelectRecipe(int index)
    {
        if (availableRecipes == null)
            return;

        if (index < 0 || index >= availableRecipes.Length)
            return;

        CraftingRecipe recipe = availableRecipes[index];

        if (recipe == null)
            return;

        // quitar seleccion de todos los botones
        for (int i = 1; i <= 3; i++)
        {
            if (recipeButtons[i] != null)
                recipeButtons[i].RemoveFromClassList("selected");
        }

        // marcar el boton seleccionado
        if (index < recipeButtons.Length && recipeButtons[index] != null)
            recipeButtons[index].AddToClassList("selected");

        selectedRecipe = recipe;

        LoadRecipeInfo();
        UpdateCraftButton();
    }

    private void LoadRecipeInfo()
    {
        if (selectedRecipe == null)
            return;

        Label recipeName = craftMenu.Q<Label>("CraftName");

        if (recipeName != null)
        {
            recipeName.text = selectedRecipe.result != null ? selectedRecipe.result.itemName : "";
        }

        Label description = craftMenu.Q<Label>("CraftDescription");

        if (description != null)
            description.text = selectedRecipe.description;

        LoadIngredients();
    }

    private void LoadIngredients()
    {
        // limpiar ingredientes
        for (int i = 1; i <= 3; i++)
        {
            VisualElement slot = craftMenu.Q<VisualElement>($"IngredientSlot_{i:00}");

            if (slot == null)
                continue;

            Image image = slot.Q<Image>("IngredientImage");
            Label count = slot.Q<Label>("IngredientCount");

            if (image != null)
                image.image = null;

            if (count != null)
                count.text = "";
        }

        if (selectedRecipe.ingredients == null)
            return;

        // cargar ingredientes
        for (int i = 0; i < selectedRecipe.ingredients.Length && i < 3; i++)
        {
            CraftingIngredient ingredient = selectedRecipe.ingredients[i];

            if (ingredient == null || ingredient.item == null)
                continue;

            VisualElement slot = craftMenu.Q<VisualElement>($"IngredientSlot_{i + 1:00}");

            if (slot == null)
                continue;

            Image image = slot.Q<Image>("IngredientImage");
            Label count = slot.Q<Label>("IngredientCount");

            if (image != null && ingredient.item.icon != null)
                image.image = ingredient.item.icon.texture;

            if (count != null)
                count.text = ingredient.amount.ToString();
        }
    }

    private void ClearRecipeInfo()
    {
        Label recipeName = craftMenu.Q<Label>("CraftName");
        Label description = craftMenu.Q<Label>("CraftDescription");

        if (recipeName != null)
            recipeName.text = "";

        if (description != null)
            description.text = "";

        for (int i = 1; i <= 3; i++)
        {
            VisualElement slot = craftMenu.Q<VisualElement>($"IngredientSlot_{i:00}");

            if (slot == null)
                continue;

            Image image = slot.Q<Image>("IngredientImage");
            Label count = slot.Q<Label>("IngredientCount");

            if (image != null)
                image.image = null;

            if (count != null)
                count.text = "";
        }
    }

    private void OnCraftButtonClicked()
    {
        if (selectedRecipe == null || inventory == null)
            return;

        if (selectedRecipe.result == null)
        {
            Debug.LogWarning("[CraftingUI] La receta no tiene un resultado asignado.");
            return;
        }

        if (!HasAllIngredients())
            return;

        foreach (CraftingIngredient ingredient in selectedRecipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null || ingredient.amount <= 0)
                continue;

            if (!inventory.RemoveItem(ingredient.item, ingredient.amount))
            {
                Debug.LogError("[CraftingUI] No se pudieron usar todos los ingredientes.");
                UpdateCraftButton();
                return;
            }
        }

        // Agregar el resultado
        inventory.AddItem(selectedRecipe.result, selectedRecipe.resultAmount);

        UpdateCraftButton();
    }

    private bool HasAllIngredients()
    {
        if (selectedRecipe == null || selectedRecipe.ingredients == null)
            return false;

        foreach (CraftingIngredient ingredient in selectedRecipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null || ingredient.amount <= 0)
                continue;

            if (!inventory.HasItem(ingredient.item, ingredient.amount))
                return false;
        }

        return true;
    }

    private void UpdateCraftButton()
    {
        if (craftButton == null)
            return;

        if (adviceLabel != null)
            adviceLabel.style.display = DisplayStyle.None;

        if (selectedRecipe == null || inventory == null || selectedRecipe.result == null)
        {
            craftButton.SetEnabled(false);
            return;
        }

        if (!HasAllIngredients())
        {
            craftButton.SetEnabled(false);

            if (adviceLabel != null)
            {
                adviceLabel.text = "Te faltan ingredientes para esta receta";
                adviceLabel.style.display = DisplayStyle.Flex;
            }

            return;
        }

        craftButton.SetEnabled(true);
    }
}