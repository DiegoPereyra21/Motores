using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;


public class IntroCinematic : MonoBehaviour
{
    [SerializeField] private CinemachineCamera introCamera; //debe tener CinemachineSplineDolly con priorida mayor q el player
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private RadioInteractable radio;//este y el de abajo para q aparezca el tuto antes para q cargue todo bien
    [SerializeField] private RadioMessageUI radioUI;
    [SerializeField, Min(1f)] private float duration = 10f;
    private float startDelay = 0.3f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private Behaviour[] disableDuringIntro; //minimos PlayerController, CameraPivot, PlayerAttack, PlayerInteraction y algo mas
    [SerializeField] private UIDocument[] hideDuringIntro; //UIHUD y NO UIRadio, quiero q se vea el lore peor no el consejo q sale luego
    [SerializeField] private bool pauseWorld = true;

    private IEnumerator Start()
    {
        CinemachineSplineDolly dolly = introCamera != null ? introCamera.GetComponent<CinemachineSplineDolly>() : null;
        if (dolly == null || dolly.Spline == null)
        {
            Debug.LogWarning("IntroCinematic: la IntroCamera necesita un CinemachineSplineDolly con una Spline asignada, se omite.");
            yield break;
        }

        //posi por distancia normalizada, creo q luego lo cambie 
        dolly.PositionUnits = PathIndexUnit.Normalized;
        dolly.AutomaticDolly.Enabled = false;
        dolly.CameraPosition = 0f;
        SetBehaviours(false);
        SetHidden(true);

        if (radioUI != null) radioUI.HoldHints = true;//para q no muestre la ui molesta en la cinematica

        yield return null;
        while (radio != null && radio.IsListening)
            yield return null;

        //al terminar, la radio devuelve el control al jugador y despausa el mundo
        SetBehaviours(false);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        bool paused = pauseWorld && GameProgressController.Instance != null && GameProgressController.Instance.PauseWorld();

        introCamera.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(startDelay);

        //paneo
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            dolly.CameraPosition = easing.Evaluate(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        if (paused) GameProgressController.Instance.ResumeWorld();

        introCamera.gameObject.SetActive(false); //el blend q tenga el main camera sera el ultimo q llevara la cam al player
        yield return null;
        while (brain != null && brain.IsBlending) yield return null;

        SetHidden(false);
        SetBehaviours(true);

        if (radioUI != null) radioUI.HoldHints = false; //ahora si sale el consejo final
    }

    private void SetBehaviours(bool enabled)
    {
        foreach (Behaviour b in disableDuringIntro)
            if (b != null) b.enabled = enabled;
    }

    private void SetHidden(bool hidden)
    {
        foreach (UIDocument doc in hideDuringIntro)
        {
            if (doc == null || doc.rootVisualElement == null) continue;

            if (hidden) doc.rootVisualElement.style.display = DisplayStyle.None;
            else doc.rootVisualElement.style.display = StyleKeyword.Null;
        }
    }
}