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

    private void OnEnable()
    {
        root = uiDocument.rootVisualElement;

        craftMenu = root.Q<VisualElement>("CraftMenu");

        if (craftMenu == null)
        {
            Debug.LogError("[CraftingUI] No se encontró CraftMenu.");
            return;
        }

        adviceLabel = root.Q<Label>("AdviceLabel");

        // botones de recetas
        for (int i = 1; i <= 3; i++)
        {
            Button recipeButton = craftMenu.Q<Button>($"RecipeSlot_{i:00}");

            if (recipeButton != null)
            {
                int recipeIndex = i - 1;

                recipeButton.clicked += () => SelectRecipe(recipeIndex);
            }
        }

        // boton de cerrar
        Button closeButton = craftMenu.Q<Button>("CloseButton");

        if (closeButton != null)
        {
            closeButton.clicked += OnCloseButtonClicked;
        }

        // craft
        craftButton = craftMenu.Q<Button>("CraftButton");

        if (craftButton != null)
        {
            craftButton.clicked += OnCraftButtonClicked;
            craftButton.SetEnabled(false);
        }

        if (inventory != null)
        {
            inventory.OnInventoryChanged += UpdateCraftButton;
        }

        if (adviceLabel != null) // aviso en texto de no poder craftear
        {
            adviceLabel.style.display = DisplayStyle.None;
        }

        Hide();
    }

    private void OnDisable()
    {
        for (int i = 1; i <= 3; i++)
        {
            Button recipeButton = craftMenu?.Q<Button>($"RecipeSlot_{i:00}");

            if (recipeButton != null)
            {
                int recipeIndex = i - 1;

                recipeButton.clicked -= () => SelectRecipe(recipeIndex);
            }
        }

        // close
        Button closeButton = craftMenu?.Q<Button>("CloseButton");

        if (closeButton != null)
        {
            closeButton.clicked -= OnCloseButtonClicked;
        }

        // craft
        if (craftButton != null)
        {
            craftButton.clicked -= OnCraftButtonClicked;
        }

        if (inventory != null)
        {
            inventory.OnInventoryChanged -= UpdateCraftButton;
        }
    }

    public void Show(CraftingRecipe[] recipes)
    {
        if (craftMenu == null)
            return;

        availableRecipes = recipes;

        LoadRecipes();

        craftMenu.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        if (craftMenu == null)
            return;

        craftMenu.style.display = DisplayStyle.None;
    }

    private void OnCloseButtonClicked()
    {
        craftTable.CloseCrafting();
    }

    private void LoadRecipes()
    {
        // limpiar los slots
        for (int i = 1; i <= 3; i++)
        {
            Button recipeButton =
                craftMenu.Q<Button>($"RecipeSlot_{i:00}");

            if (recipeButton == null)
                continue;

            Image image = recipeButton.Q<Image>("RecipeImage");

            if (image != null)
                image.image = null;
        }

        if (availableRecipes == null)
            return;

        // cargar recetas
        for (int i = 0; i < availableRecipes.Length && i < 3; i++)
        {
            CraftingRecipe recipe = availableRecipes[i];

            if (recipe == null)
                continue;

            Button recipeButton =
                craftMenu.Q<Button>($"RecipeSlot_{i + 1:00}");

            if (recipeButton == null)
                continue;

            Image image = recipeButton.Q<Image>("RecipeImage");

            if (image != null && recipe.recipeImage != null)
            {
                image.image = recipe.recipeImage.texture;
            }
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
            Button button = craftMenu.Q<Button>($"RecipeSlot_{i:00}");

            if (button != null)
                button.RemoveFromClassList("selected");
        }

        // marcar el boton seleccionado
        Button selectedButton =
            craftMenu.Q<Button>($"RecipeSlot_{index + 1:00}");

        if (selectedButton != null)
            selectedButton.AddToClassList("selected");

        selectedRecipe = recipe;

        LoadRecipeInfo();
        UpdateCraftButton();

        LoadRecipeInfo();
    }

    private void LoadRecipeInfo()
    {
        if (selectedRecipe == null)
            return;

        Label description =
            craftMenu.Q<Label>("CraftDescription");

        if (description != null)
        {
            description.text = selectedRecipe.description;
        }

        LoadIngredients();
    }

    private void LoadIngredients()
    {
        // limpiar ingredientes
        for (int i = 1; i <= 3; i++)
        {
            VisualElement slot =
                craftMenu.Q<VisualElement>($"IngredientSlot_{i:00}");

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
        for (int i = 0;
             i < selectedRecipe.ingredients.Length && i < 3;
             i++)
        {
            CraftingIngredient ingredient =
                selectedRecipe.ingredients[i];

            if (ingredient == null || ingredient.item == null)
                continue;

            VisualElement slot =
                craftMenu.Q<VisualElement>(
                    $"IngredientSlot_{i + 1:00}");

            if (slot == null)
                continue;

            Image image = slot.Q<Image>("IngredientImage");
            Label count = slot.Q<Label>("IngredientCount");

            // imagen del item
            if (image != null && ingredient.item.icon != null)
            {
                image.image = ingredient.item.icon.texture;
            }

            // cantidad necesaria
            if (count != null)
            {
                count.text = ingredient.amount.ToString();
            }
        }
    }

    private void OnCraftButtonClicked()
    {
        if (selectedRecipe == null)
            return;

        if (inventory == null)
        {
            return;
        }

        // tenemos todos los ingredientes
        foreach (CraftingIngredient ingredient in selectedRecipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null)
                continue;

            if (!inventory.HasItem(ingredient.item, ingredient.amount))
            {
                return;
            }
        }

        // quitar ingredientes
        foreach (CraftingIngredient ingredient in selectedRecipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null)
                continue;

            inventory.RemoveItem(ingredient.item, ingredient.amount);
        }

        // agregar resultado
        if (selectedRecipe.result != null)
        {
            inventory.AddItem(
                selectedRecipe.result,
                selectedRecipe.resultAmount
            );
        }
    }

    private void UpdateCraftButton()
    {
        if (craftButton == null)
            return;

        if (adviceLabel != null)
            adviceLabel.style.display = DisplayStyle.None;

        if (selectedRecipe == null || inventory == null)
        {
            craftButton.SetEnabled(false);
            return;
        }

        foreach (CraftingIngredient ingredient in selectedRecipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null)
                continue;

            if (!inventory.HasItem(ingredient.item, ingredient.amount))
            {
                craftButton.SetEnabled(false);

                if (adviceLabel != null)
                {
                    adviceLabel.text = "Te faltan ingredientes para esta receta";
                    adviceLabel.style.display = DisplayStyle.Flex;
                }

                return;
            }
        }

        craftButton.SetEnabled(true);
    }
}