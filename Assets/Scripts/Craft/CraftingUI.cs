
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CraftingUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private CraftTable craftTable;
    [SerializeField] private Inventory inventory;

    private VisualElement root;
    private VisualElement craftMenu;
    private VisualElement recipeList;
    private VisualElement recipeSlotTemplate;
    private VisualElement ingredientsContainer;
    private float originalSortOrder;

    private Image recipeImage;
    private Label recipeName;
    private Label recipeDescription;
    private Label adviceLabel;
    private Button craftButton;

    private Button allButton;
    private Button consumablesButton;
    private Button equipableButton;
    private Button placeablesButton;

    private CraftingRecipe[] allRecipes;
    private CraftingRecipe selectedRecipe;
    private int currentDay = 1;

    private enum RecipeFilter
    {
        All,
        Consumables,
        Equipable,
        Placeables
    }

    private RecipeFilter currentFilter = RecipeFilter.All;

    private void Update()
    {
        if (craftMenu.style.display == DisplayStyle.None)
            return;

        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            craftTable.CloseCrafting();
        }
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[CraftingUI] No se asigno UIDocument.");
            return;
        }

        root = uiDocument.rootVisualElement;
        craftMenu = root.Q<VisualElement>("CraftMenu");
        originalSortOrder = uiDocument.sortingOrder;

        if (craftMenu == null)
        {
            Debug.LogError("[CraftingUI] No se encontro CraftMenu.");
            return;
        }

        recipeList = craftMenu.Q<VisualElement>("RecipeList");
        recipeSlotTemplate = recipeList?.Q<VisualElement>("RecipeSlot");
        ingredientsContainer = craftMenu.Q<VisualElement>("IngredientsContainer");

        recipeImage = craftMenu.Q<Image>("RecipeImage");
        recipeName = craftMenu.Q<Label>("RecipeName");
        recipeDescription = craftMenu.Q<Label>("RecipeDescription");
        adviceLabel = craftMenu.Q<Label>("AdviceLabel");
        craftButton = craftMenu.Q<Button>("CraftButton");

        allButton = craftMenu.Q<Button>("AllButton");
        consumablesButton = craftMenu.Q<Button>("ConsumablesButton");
        equipableButton = craftMenu.Q<Button>("EquipableButton");
        placeablesButton = craftMenu.Q<Button>("PlaceablesButton");

        if (recipeList == null || recipeSlotTemplate == null)
        {
            Debug.LogError("[CraftingUI] Falta RecipeList o su plantilla RecipeSlot.");
            return;
        }

        // ocultamos la plantilla
        recipeSlotTemplate.style.display = DisplayStyle.None;

        if (allButton != null)
            allButton.clicked += ShowAllRecipes;

        if (consumablesButton != null)
            consumablesButton.clicked += ShowConsumables;

        if (equipableButton != null)
            equipableButton.clicked += ShowEquipable;

        if (placeablesButton != null)
            placeablesButton.clicked += ShowPlaceables;

        Button closeButton = craftMenu.Q<Button>("CloseButton");

        if (closeButton != null)
            closeButton.clicked += OnCloseButtonClicked;

        if (craftButton != null)
        {
            craftButton.clicked += OnCraftButtonClicked;
            craftButton.SetEnabled(false);
        }

        if (inventory != null)
            inventory.OnInventoryChanged += RefreshRecipes;

        if (adviceLabel != null)
            adviceLabel.style.display = DisplayStyle.None;

        Hide();
    }

    private void OnDisable()
    {
        if (allButton != null)
            allButton.clicked -= ShowAllRecipes;

        if (consumablesButton != null)
            consumablesButton.clicked -= ShowConsumables;

        if (equipableButton != null)
            equipableButton.clicked -= ShowEquipable;

        if (placeablesButton != null)
            placeablesButton.clicked -= ShowPlaceables;

        Button closeButton = craftMenu?.Q<Button>("CloseButton");

        if (closeButton != null)
            closeButton.clicked -= OnCloseButtonClicked;

        if (craftButton != null)
            craftButton.clicked -= OnCraftButtonClicked;

        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshRecipes;
    }

    public void Show(CraftingRecipe[] recipes, int day)
    {
        allRecipes = recipes ?? new CraftingRecipe[0];
        currentDay = Mathf.Max(1, day);
        selectedRecipe = null;
        currentFilter = RecipeFilter.All;

        if (allButton != null)
            allButton.AddToClassList("selected");

        if (consumablesButton != null)
            consumablesButton.RemoveFromClassList("selected");

        if (equipableButton != null)
            equipableButton.RemoveFromClassList("selected");

        if (placeablesButton != null)
            placeablesButton.RemoveFromClassList("selected");

        ClearRecipeInfo();
        RefreshRecipes();

        // fix de sort
        uiDocument.sortingOrder = 1;

        if (craftMenu != null)
            craftMenu.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        if (craftMenu == null)
            return;

        craftMenu.style.display = DisplayStyle.None;

        if (uiDocument != null)
            uiDocument.sortingOrder = originalSortOrder;
    }

    private void OnCloseButtonClicked()
    {
        if (craftTable != null)
            craftTable.CloseCrafting();
        else
            Hide();
    }

    private void ShowAllRecipes()
    {
        SetFilter(RecipeFilter.All, allButton);
    }

    private void ShowConsumables()
    {
        SetFilter(RecipeFilter.Consumables, consumablesButton);
    }

    private void ShowEquipable()
    {
        SetFilter(RecipeFilter.Equipable, equipableButton);
    }

    private void ShowPlaceables()
    {
        SetFilter(RecipeFilter.Placeables, placeablesButton);
    }

    private void SetFilter(RecipeFilter filter, Button activeButton)
    {
        Debug.Log($"[CraftingUI] Filtro seleccionado: {filter}");

        currentFilter = filter;

        Button[] buttons =
        {
            allButton,
            consumablesButton,
            equipableButton,
            placeablesButton
        };

        foreach (Button button in buttons)
        {
            if (button != null)
                button.RemoveFromClassList("selected");
        }

        if (activeButton != null)
            activeButton.AddToClassList("selected");

        RefreshRecipes();
    }

    private void RefreshRecipes()
    {
        if (recipeList == null || recipeSlotTemplate == null)
            return;

        // mantiene la plantilla mientras se eliminan las recetas generadas
        List<VisualElement> generatedSlots = new List<VisualElement>();

        foreach (VisualElement child in recipeList.Children())
        {
            if (child != recipeSlotTemplate)
                generatedSlots.Add(child);
        }

        foreach (VisualElement slot in generatedSlots)
            slot.RemoveFromHierarchy();

        if (allRecipes == null)
        {
            UpdateCraftButton();
            return;
        }

        foreach (CraftingRecipe recipe in allRecipes)
        {
            if (recipe == null || recipe.result == null)
                continue;

            if (!MatchesFilter(recipe))
                continue;

            CreateRecipeSlot(recipe);
        }

        UpdateCraftButton();
    }

    private bool MatchesFilter(CraftingRecipe recipe)
    {
        switch (currentFilter)
        {
            case RecipeFilter.Consumables:
                return recipe.result.category == ItemCategory.Consumable;

            case RecipeFilter.Equipable:
                return recipe.result.category == ItemCategory.Weapon;

            case RecipeFilter.Placeables:
                return recipe.result.category == ItemCategory.Placeable || recipe.result.category == ItemCategory.Trap;

            default:
                return true;
        }
    }

    private void CreateRecipeSlot(CraftingRecipe recipe)
    {
        Button slot = new Button();
        slot.name = "GeneratedRecipeSlot";
        slot.AddToClassList("recipe-slot");
        slot.text = "";

        VisualElement iconFrame = new VisualElement();
        iconFrame.name = "RecipeIconFrame";
        iconFrame.AddToClassList("recipe-icon-frame");

        Image icon = new Image();
        icon.name = "RecipeIcon";
        icon.AddToClassList("recipe-icon");
        icon.scaleMode = ScaleMode.ScaleToFit;

        if (recipe.result.icon != null)
            icon.image = recipe.result.icon.texture;

        iconFrame.Add(icon);

        VisualElement info = new VisualElement();
        info.name = "RecipeSlotInfo";
        info.AddToClassList("recipe-slot-info");

        Label nameLabel = new Label(recipe.result.itemName);
        nameLabel.name = "RecipeName";
        nameLabel.AddToClassList("recipe-name");

        Label statusLabel = new Label();
        statusLabel.name = "RecipeStatus";
        statusLabel.AddToClassList("recipe-status");

        Label stateIcon = new Label();
        stateIcon.name = "RecipeStateIcon";
        stateIcon.AddToClassList("recipe-state-icon");

        info.Add(nameLabel);
        info.Add(statusLabel);

        slot.Add(iconFrame);
        slot.Add(info);
        slot.Add(stateIcon);

        UpdateRecipeSlotState(recipe, slot, statusLabel, stateIcon);

        bool unlocked = IsUnlocked(recipe);

        if (unlocked)
        {
            slot.clicked += () => SelectRecipe(recipe, slot);
        }
        else
        {
            slot.SetEnabled(false);
        }

        recipeList.Add(slot);
    }

    private void UpdateRecipeSlotState(CraftingRecipe recipe, Button slot, Label statusLabel, Label stateIcon)
    {
        bool unlocked = IsUnlocked(recipe);
        bool hasIngredients = HasAllIngredients(recipe);

        slot.RemoveFromClassList("selected");
        slot.RemoveFromClassList("locked");
        slot.RemoveFromClassList("insufficient");

        statusLabel.RemoveFromClassList("locked");
        statusLabel.RemoveFromClassList("insufficient");

        stateIcon.RemoveFromClassList("locked");
        stateIcon.RemoveFromClassList("insufficient");

        if (!unlocked)
        {
            slot.AddToClassList("locked");
            statusLabel.AddToClassList("locked");
            stateIcon.AddToClassList("locked");

            statusLabel.text = $"DESBLOQUEO: DIA {GetUnlockDay(recipe)}";
            stateIcon.text = "×";
        }
        else if (!hasIngredients)
        {
            slot.AddToClassList("insufficient");
            statusLabel.AddToClassList("insufficient");
            stateIcon.AddToClassList("insufficient");

            statusLabel.text = "FALTAN MATERIALES";
            stateIcon.text = "!";
        }
        else
        {
            statusLabel.text = "LISTA PARA FABRICAR";
            stateIcon.text = "✓";
        }
    }

    private void SelectRecipe(CraftingRecipe recipe, Button selectedSlot)
    {
        Debug.Log($"[CraftingUI] Receta seleccionada: {recipe.result.itemName}");

        selectedRecipe = recipe;

        foreach (VisualElement child in recipeList.Children())
            child.RemoveFromClassList("selected");

        selectedSlot.AddToClassList("selected");

        LoadRecipeInfo();
        UpdateCraftButton();
    }

    private void LoadRecipeInfo()
    {
        if (selectedRecipe == null)
            return;

        if (recipeName != null)
            recipeName.text = selectedRecipe.result.itemName;

        if (recipeDescription != null)
            recipeDescription.text = selectedRecipe.description;

        if (recipeImage != null)
        {
            recipeImage.image = selectedRecipe.result.icon != null ? selectedRecipe.result.icon.texture : null;
        }

        LoadIngredients();
    }

    private void LoadIngredients()
    {
        if (ingredientsContainer == null)
            return;

        ingredientsContainer.Clear();

        if (selectedRecipe == null || selectedRecipe.ingredients == null)
            return;

        foreach (CraftingIngredient ingredient in selectedRecipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null)
                continue;

            VisualElement row = new VisualElement();
            row.AddToClassList("ingredient-row");

            Image icon = new Image();
            icon.AddToClassList("ingredient-icon");
            icon.scaleMode = ScaleMode.ScaleToFit;

            if (ingredient.item.icon != null)
                icon.image = ingredient.item.icon.texture;

            Label name = new Label(ingredient.item.itemName);
            name.AddToClassList("ingredient-name");

            int available = inventory != null ? inventory.GetItemCount(ingredient.item) : 0;

            Label amount = new Label($"{available}/{ingredient.amount}");
            amount.AddToClassList("ingredient-count");

            if (available < ingredient.amount)
                amount.AddToClassList("insufficient");

            row.Add(icon);
            row.Add(name);
            row.Add(amount);

            ingredientsContainer.Add(row);
        }
    }

    private void ClearRecipeInfo()
    {
        if (recipeName != null)
            recipeName.text = "Selecciona una receta";

        if (recipeDescription != null)
            recipeDescription.text = "";

        if (recipeImage != null)
            recipeImage.image = null;

        if (ingredientsContainer != null)
            ingredientsContainer.Clear();

        if (adviceLabel != null)
            adviceLabel.style.display = DisplayStyle.None;

        if (craftButton != null)
            craftButton.SetEnabled(false);
    }

    private bool IsUnlocked(CraftingRecipe recipe)
    {
        return craftTable != null && GetProgression()?.IsRecipeUnlocked(recipe, currentDay) == true;
    }

    private int GetUnlockDay(CraftingRecipe recipe)
    {
        CraftingProgression progression = GetProgression();

        if (progression == null)
            return 1;

        for (int day = 1; day <= 3; day++)
        {
            if (progression.IsRecipeUnlocked(recipe, day))
                return day;
        }

        return 1;
    }

    private CraftingProgression GetProgression()
    {
        // consulta la progresion desde la propiedad Progression de crafttable
        return craftTable != null ? craftTable.Progression : null;
    }

    private bool HasAllIngredients(CraftingRecipe recipe)
    {
        if (recipe == null || inventory == null || recipe.result == null)
            return false;

        if (recipe.resultAmount <= 0)
            return false;

        if (recipe.ingredients == null)
            return true;

        foreach (CraftingIngredient ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null)
                continue;

            if (ingredient.amount <= 0)
                return false;

            if (!inventory.HasItem(ingredient.item, ingredient.amount))
                return false;
        }

        return true;
    }

    private void OnCraftButtonClicked()
    {
        if (selectedRecipe == null || inventory == null)
            return;

        if (!IsUnlocked(selectedRecipe) || !HasAllIngredients(selectedRecipe))
        {
            UpdateCraftButton();
            return;
        }

        if (selectedRecipe.ingredients == null)
        {
            inventory.AddItem(selectedRecipe.result, selectedRecipe.resultAmount);
            RefreshRecipes();
            UpdateCraftButton();
            return;
        }

        // revisamos los items antes de consumir los ingredientes
        foreach (CraftingIngredient ingredient in selectedRecipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null)
                continue;

            if (!inventory.HasItem(ingredient.item, ingredient.amount))
            {
                UpdateCraftButton();
                return;
            }
        }

        foreach (CraftingIngredient ingredient in selectedRecipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null)
                continue;

            if (!inventory.RemoveItem(ingredient.item, ingredient.amount))
            {
                Debug.LogError("[CraftingUI] Fallo el consumo de ingredientes despues de validarlos.");
                UpdateCraftButton();
                return;
            }
        }

        inventory.AddItem(selectedRecipe.result, selectedRecipe.resultAmount);

        LoadRecipeInfo();
        RefreshRecipes();
        UpdateCraftButton();
    }

    private void UpdateCraftButton()
    {
        if (craftButton == null)
            return;

        bool unlocked = selectedRecipe != null && IsUnlocked(selectedRecipe);
        bool canCraft = unlocked && HasAllIngredients(selectedRecipe);

        craftButton.SetEnabled(canCraft);

        if (adviceLabel != null)
        {
            if (selectedRecipe == null)
            {
                adviceLabel.style.display = DisplayStyle.None;
            }
            else if (!unlocked)
            {
                adviceLabel.text = $"Esta receta se desbloquea el dia {GetUnlockDay(selectedRecipe)}";
                adviceLabel.style.display = DisplayStyle.Flex;
            }
            else if (!canCraft)
            {
                adviceLabel.text = "Faltan materiales para fabricar";
                adviceLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                adviceLabel.style.display = DisplayStyle.None;
            }
        }

        if (selectedRecipe != null)
            LoadIngredients();
    }
}
