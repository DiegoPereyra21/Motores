
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

    private VisualElement interactionRoot;
    private VisualElement interactionPrompt;
    private VisualElement interactionMenu;
    private VisualElement actionsContainer;

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
        if (target != null && !playerInteraction.IsMenuOpen)
        {
            ShowPrompt();
        }
        else
        {
            HidePrompt();
        }
    }

    private void HandleMenuOpened(IReadOnlyList<InteractionOption> actions)
    {
        HidePrompt();
        ShowMenu(actions);
    }

    private void HandleMenuClosed()
    {
        HideMenu();

        if (playerInteraction.CurrentTarget != null)
            ShowPrompt();
        else
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
        if (interactionMenu != null)
            interactionMenu.style.display = DisplayStyle.Flex;
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
