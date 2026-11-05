
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InteractionUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Referencias")]
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private Font specialEliteFont;

    private VisualElement interactionRoot;
    private VisualElement interactionPrompt;
    private VisualElement interactionMenu;
    private VisualElement actionsContainer;
    private bool forcedPrompt; // cartel fijo (bengala), ignora los cambios de objetivo
    private Label promptKey;
    private Label promptText;
    private Label menuTitle;
    private Label closeHint;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("[InteractionUI] No se encontro el UIDocument.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        interactionRoot = root.Q<VisualElement>("InteractionRoot");

        interactionPrompt = root.Q<VisualElement>("InteractionPrompt");
        promptKey = root.Q<Label>("PromptKey");
        promptText = root.Q<Label>("PromptText");

        interactionMenu = root.Q<VisualElement>("InteractionMenu");
        menuTitle = root.Q<Label>("MenuTitle");
        actionsContainer = root.Q<VisualElement>("ActionsContainer");
        closeHint = root.Q<Label>("CloseHint");

        if (interactionRoot == null || interactionPrompt == null || interactionMenu == null || actionsContainer == null)
        {
            Debug.LogError("[InteractionUI] Faltan elementos en Interaction.uxml.", this);
        }

        HideAll();
    }

    private void OnEnable()
    {
        if (playerInteraction == null)
            return;

        playerInteraction.OnTargetChanged += HandleTargetChanged;
        playerInteraction.OnMenuOpened += HandleMenuOpened;
        playerInteraction.OnMenuClosed += HandleMenuClosed;

        HandleTargetChanged(playerInteraction.CurrentTarget);
    }

    private void OnDisable()
    {
        if (playerInteraction == null)
            return;

        playerInteraction.OnTargetChanged -= HandleTargetChanged;
        playerInteraction.OnMenuOpened -= HandleMenuOpened;
        playerInteraction.OnMenuClosed -= HandleMenuClosed;
    }

    private void HandleTargetChanged(IInteractable target)
    {
        if (forcedPrompt) return;

        if (target != null && !playerInteraction.IsMenuOpen)
        {
            if (promptText != null)
                promptText.text = playerInteraction.GetPromptText();

            ShowPrompt();
        }
        else
        {
            HidePrompt();
        }
    }

    private void HandleMenuOpened(IReadOnlyList<InteractionOption> actions)
    {
        if (forcedPrompt) return;

        HidePrompt();
        ShowMenu(actions);
    }

    private void HandleMenuClosed()
    {
        if (forcedPrompt) return;

        HideMenu();

        if (playerInteraction.CurrentTarget != null)
        {
            if (promptText != null)
                promptText.text = playerInteraction.GetPromptText();

            ShowPrompt();
        }
        else
        {
            HidePrompt();
        }
    }

    private void HandleActionExecuted(InteractionOption option)
    {
        if (option == null || string.IsNullOrWhiteSpace(option.ResultMessage))
            return;

        if (promptText != null)
            promptText.text = option.ResultMessage;

        ShowPrompt();
    }

    public void ShowForcedPrompt(string key, string text)
    {
        forcedPrompt = true;
        HideMenu();

        if (promptKey != null) promptKey.text = key;
        if (promptText != null) promptText.text = text;

        ShowPrompt();
    }

    public void HideForcedPrompt()
    {
        forcedPrompt = false;
        HidePrompt();
    }

    private void ShowPrompt()
    {
        if (interactionPrompt != null)
            interactionPrompt.style.display = DisplayStyle.Flex;
    }

    private void HidePrompt()
    {
        if (interactionPrompt != null)
            interactionPrompt.style.display = DisplayStyle.None;
    }

    private void ShowMenu(IReadOnlyList<InteractionOption> actions)
    {
        if (actionsContainer == null)
            return;

        actionsContainer.Clear();

        Debug.Log($"[InteractionUI] Acciones recibidas: {actions.Count}");

        if (interactionMenu != null)
            interactionMenu.style.display = DisplayStyle.Flex;

        foreach (InteractionOption option in actions)
        {
            if (option == null || option.Data == null)
                continue;

            Debug.Log($"[InteractionUI] Creando botón: {option.Data.DisplayName}");

            Button actionButton = new Button();
            actionButton.style.unityFontDefinition = FontDefinition.FromFont(specialEliteFont);
            actionButton.text = option.Data.DisplayName;
            actionButton.AddToClassList("action-button");

            actionButton.SetEnabled(option.IsAvailable);

            if (!option.IsAvailable)
                actionButton.tooltip = option.UnavailableReason;

            InteractionActionData selectedAction = option.Data;

            actionButton.clicked += () =>
            {
                playerInteraction.ExecuteAction(selectedAction);
            };

            actionsContainer.Add(actionButton);
        }
    }

    private void HideMenu()
    {
        if (interactionMenu != null)
            interactionMenu.style.display = DisplayStyle.None;
    }
    private void HideAll()
    {
        if (interactionPrompt != null)
            interactionPrompt.style.display = DisplayStyle.None;

        if (interactionMenu != null)
            interactionMenu.style.display = DisplayStyle.None;
    }
}
