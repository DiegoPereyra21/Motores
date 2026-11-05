using System.Collections.Generic;
using UnityEngine;

public class BlockedInteractable : MonoBehaviour, IInteractable
{
    [Header("Configuración")]
    [SerializeField] private InteractionActionData interactionAction;
    [SerializeField] private string blockedMessage;

    private InteractionOption interactionOption;
    private List<InteractionOption> actions;

    private void Awake()
    {
        string message = string.IsNullOrWhiteSpace(blockedMessage)
            ? interactionAction.Description
            : blockedMessage;

        interactionOption = new InteractionOption(
            interactionAction,
            resultMessage: message
        );

        actions = new List<InteractionOption> { interactionOption };
    }

    public bool CanOpenInteraction(GameObject interactor)
    {
        return interactor != null && interactionAction != null;
    }

    public IReadOnlyList<InteractionOption> GetActions(GameObject interactor)
    {
        return actions;
    }

    public bool Interact(InteractionActionData action, GameObject interactor)
    {
        return action == interactionAction && interactor != null;
    }
}