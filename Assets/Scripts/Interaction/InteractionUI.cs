
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InteractionUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;

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

        HideAll();
    }

    private void HideAll()
    {
        if (interactionPrompt != null)
            interactionPrompt.style.display = DisplayStyle.None;

        if (interactionMenu != null)
            interactionMenu.style.display = DisplayStyle.None;
    }
}
