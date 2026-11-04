using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Fin de la ultima noche: mata los zombies y espera a que el jugador escuche el mensaje final en la radio. Despues:
// con bengala -> cartel "E Activar bengala" fijo hasta que el jugador la tire (victoria). Sin bengala -> derrota.
public class FinaleSequence : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RadioInteractable radio;
    [SerializeField] private InteractionUI interactionUI;
    [SerializeField] private InputActionReference flareAction; // la accion Interact (E), la misma que usa PlayerInteraction

    [Header("Cartel")]
    [SerializeField] private string promptKey = "E";
    [SerializeField] private string promptText = "Activar bengala";

    [Header("Tiempos")]
    [SerializeField, Min(0f)] private float ignoreInputTime = 0.4f; // la E que saltea la radio no tira la bengala
    [SerializeField, Min(0f)] private float defeatDelay = 1f; // pausa entre el mensaje y la cinematica de derrota

    private GameProgressController progress;
    private bool isSubscribed;
    private bool finaleStarted;
    private bool messageResolved;
    private bool waitingForFlare;
    private float inputEnabledTime;

    private void OnEnable()
    {
        if (flareAction != null)
            flareAction.action.Enable();

        if (radio != null)
            radio.OnMessageFinished += HandleRadioMessageFinished;

        TryConnectToGameProgress();
    }

    private void OnDisable()
    {
        if (radio != null)
            radio.OnMessageFinished -= HandleRadioMessageFinished;

        if (progress != null && isSubscribed)
            progress.OnFinaleStarted -= HandleFinaleStarted;

        progress = null;
        isSubscribed = false;
    }

    private void Update()
    {
        if (!isSubscribed)
            TryConnectToGameProgress();

        if (!waitingForFlare || flareAction == null || progress == null)
            return;

        if (progress.IsGameEnded)
        {
            waitingForFlare = false;
            if (interactionUI != null) interactionUI.HideForcedPrompt();
            return;
        }

        if (progress.IsWorldPaused || Time.unscaledTime < inputEnabledTime)
            return;

        if (flareAction.action.WasPressedThisFrame())
            FireFlare();
    }

    private void TryConnectToGameProgress()
    {
        if (isSubscribed)
            return;

        progress = GameProgressController.Instance;

        if (progress == null)
            return;

        progress.OnFinaleStarted += HandleFinaleStarted;
        isSubscribed = true;
    }

    private void HandleFinaleStarted()
    {
        if (finaleStarted)
            return;

        finaleStarted = true;
        EndingCinematic.KillAliveZombies();
    }

    // termino (o se salteo) un mensaje de radio: si es el final, se resuelve el final
    private void HandleRadioMessageFinished(int day)
    {
        if (messageResolved || progress == null || !progress.IsFinaleActive || day != progress.FinaleDay)
            return;

        messageResolved = true;
        StartCoroutine(ResolveFinale());
    }

    private IEnumerator ResolveFinale()
    {
        if (progress.HasRequiredEndingItem)
        {
            if (interactionUI != null)
                interactionUI.ShowForcedPrompt(promptKey, promptText);

            inputEnabledTime = Time.unscaledTime + ignoreInputTime;
            waitingForFlare = true;
            yield break;
        }

        yield return new WaitForSecondsRealtime(defeatDelay);
        progress.EndGame(GameEnding.Defeat);
    }

    private void FireFlare()
    {
        waitingForFlare = false;

        if (interactionUI != null)
            interactionUI.HideForcedPrompt();

        progress.EndGame(GameEnding.Victory);
    }
}