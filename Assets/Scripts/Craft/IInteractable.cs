using UnityEngine;

//interfaz para cualquier objeto q deba ser interactuable x el player, puertas, cobres, zombies, lo q sea
public interface IInteractable
{
    void Interact(GameObject interactor);
}