using System.Collections.Generic;
using UnityEngine;

public class OpeningInteractable : MonoBehaviour, IInteractable
{
    [Header("Referencias")]
    [SerializeField] private CabinOpening cabinOpening;

    [Header("Acciones")]
    [SerializeField] private List<InteractionActionData> availableActions;

    private void Awake()
    {
        if (cabinOpening == null)
            cabinOpening = GetComponent<CabinOpening>();
    }

    public bool CanOpenInteraction(GameObject interactor)
    {
        if (interactor == null || cabinOpening == null)
            return false;

        return !cabinOpening.IsBuilding;
    }

    public IReadOnlyList<InteractionOption> GetActions(GameObject interactor)
    {
        List<InteractionOption> options = new List<InteractionOption>();

        if (interactor == null || cabinOpening == null)
            return options;

        foreach (InteractionActionData action in availableActions)
        {
            if (action == null)
                continue;

            switch (action.ActionId)
            {
                case "place_barricade":

                    bool canPlace = cabinOpening.CanPlace(interactor, out _, out _, out string reason, out OpeningPlacementError error);

                    options.Add(new InteractionOption(action, canPlace, reason, feedbackType: error));

                    break;
            }
        }

        return options;
    }

    public bool Interact(InteractionActionData action, GameObject interactor)
    {
        if (action == null || interactor == null || cabinOpening == null)
            return false;

        switch (action.ActionId)
        {
            case "place_barricade":
                return cabinOpening.TryPlaceBarricade(interactor);

            default:
                return false;
        }
    }
}