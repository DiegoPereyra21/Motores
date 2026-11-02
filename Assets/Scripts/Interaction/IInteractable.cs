using System.Collections.Generic;
using UnityEngine;

//interfaz para cualquier objeto q deba ser interactuable x el player, puertas, cobres, zombies, lo q sea
public interface IInteractable
{
    bool CanOpenInteraction(GameObject interactor);

    IReadOnlyList<InteractionOption> GetActions(GameObject interactor);

    bool Interact(InteractionActionData action, GameObject interactor);
}