using UnityEngine;
using UnityEngine.UIElements;

// Cartel de interaccion (UI Toolkit, InteractionPrompt.uxml). Muestra el texto de lo que el player esta mirando,
// si ese objeto implementa IInteractionPrompt (por ahora, las aberturas de la cabaña).
[RequireComponent(typeof(UIDocument))]
public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private PlayerInteraction playerInteraction; //el del Player
    [SerializeField] private PlayerController playerController; //si se deja vacio se busca en el mismo objeto que playerInteraction

    private Label promptLabel;
    private string currentText; //para no tocar el estilo todos los frames

    private void OnEnable()
    {
        // se busca en OnEnable porque el UIDocument reconstruye el arbol cada vez que se activa
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        promptLabel = root.Q<Label>("PromptLabel");

        if (promptLabel == null)
            Debug.LogError("[InteractionPromptUI] No se encontro PromptLabel en el UXML.", this);

        if (playerController == null && playerInteraction != null)
            playerController = playerInteraction.GetComponent<PlayerController>();

        currentText = null;
        Hide();
    }

    private void Update()
    {
        if (promptLabel == null) return;

        if (!TryGetText(out string text))
        {
            Hide();
            return;
        }

        if (text == currentText) return;

        currentText = text;
        promptLabel.text = text;
        promptLabel.style.display = DisplayStyle.Flex;
    }

    private bool TryGetText(out string text)
    {
        text = null;

        if (playerInteraction == null) return false;

        //no mostrar con el mundo pausado (radio), con la partida terminada, ni si el player esta ocupado o paralizado
        GameProgressController progress = GameProgressController.Instance;
        if (progress != null && (progress.IsWorldPaused || progress.IsGameEnded)) return false;
        if (playerController != null && (playerController.IsBusy || playerController.IsStunned)) return false;

        IInteractable target = playerInteraction.CurrentTarget;

        // "is" no sirve con objetos de Unity destruidos, por eso se castea a Object primero
        if (target is Object unityObject && unityObject == null) return false;

        return target is IInteractionPrompt prompt && prompt.TryGetPrompt(playerInteraction.gameObject, out text);
    }

    private void Hide()
    {
        if (promptLabel == null) return;

        if (currentText == null && promptLabel.style.display == DisplayStyle.None) return;

        currentText = null;
        promptLabel.style.display = DisplayStyle.None;
    }
}
