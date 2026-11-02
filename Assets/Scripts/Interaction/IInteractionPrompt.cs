using UnityEngine;

//opcional para los IInteractable: si lo implementan, el cartel de interaccion muestra el texto que devuelvan mientras el player los mira
public interface IInteractionPrompt
{
    //devuelve false si no hay que mostrar nada
    bool TryGetPrompt(GameObject interactor, out string text);
}
