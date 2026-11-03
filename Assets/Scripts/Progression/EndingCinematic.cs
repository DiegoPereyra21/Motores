using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

//victoria (bengala + heli se queda) o derrota (heli pasa de largo). Al terminar le pide a EndGameUI que muestre el panel
public class EndingCinematic : MonoBehaviour
{
    [SerializeField] private PlayableDirector victoryDirector;
    [SerializeField] private PlayableDirector defeatDirector;
    //grupos, desactivados x defecto
    [SerializeField] private GameObject victoryGroup;
    [SerializeField] private GameObject defeatGroup;
    [SerializeField] private GameObject playerRoot; //se apaga entero, en la cinematica actua unn doble
    [SerializeField] private Behaviour[] disableBeforeFade; //PlayerController, CameraPivot, PlayerAttack y PlayerInteraction, pa q no se mueva durante el fade
    [SerializeField] private EndGameUI endGameUI;
    [SerializeField] private HUDController hudController;
    [SerializeField, Min(0f)] private float fadeDuration = 1f;
    [SerializeField, Min(0f)] private float blackHoldTime = 0.5f; //tiempo en negro mientras se acomoda todo

    //audio (los AudioSource van en cada Heli de la cinematica, con Output en el grupo SFX del MainMixer y Play On Awake activado)
    [Header("Audio")]
    [SerializeField] private AudioSource[] helicopterSources; //los de victoria y derrota, suenan solos cuando el heli se activa
    [SerializeField, Min(0f)] private float helicopterFadeOut = 1.5f; //baja el sonido al terminar la cinematica, para que no siga sonando debajo del panel final
    [SerializeField] private AudioSource rescuedSource; //AudioSource 2D (en Ending_Victory) con la voz del rescate y Output en el grupo Voz
    [SerializeField, Min(0f)] private float rescuedDelay = 2.5f; //segundos desde que arranca la cinematica de victoria hasta que el player dice la frase (cuando entra el helicoptero)

    private GameProgressController gameProgressController;
    private bool isSubscribed;
    private bool isPlaying;

    private void OnEnable()
    {
        TryConnectToGameProgress();
    }

    private void Update()
    {
        if (!isSubscribed)
            TryConnectToGameProgress();
    }

    private void OnDisable()
    {
        if (gameProgressController != null && isSubscribed)
            gameProgressController.OnGameEnded -= HandleGameEnded;

        gameProgressController = null;
        isSubscribed = false;
    }

    private void TryConnectToGameProgress()
    {
        if (isSubscribed)
            return;

        gameProgressController = GameProgressController.Instance;

        if (gameProgressController == null)
            return;

        gameProgressController.OnGameEnded += HandleGameEnded;
        isSubscribed = true;
    }

    private void HandleGameEnded(GameEnding ending)
    {
        if (isPlaying)
            return;

        switch (ending)
        {
            case GameEnding.Victory:
                StartCoroutine(PlaySequence(victoryGroup, victoryDirector, ending));
                break;

            case GameEnding.Defeat:
                StartCoroutine(PlaySequence(defeatGroup, defeatDirector, ending));
                break;
        }
    }

    private IEnumerator PlaySequence(GameObject group, PlayableDirector director, GameEnding ending)
    {
        isPlaying = true;

        //si falta algo, al menos q se vea el panel como antes
        if (group == null || director == null || endGameUI == null)
        {
            Debug.LogWarning("[EndingCinematic] Falta asignar grupo, director o EndGameUI, se muestra solo el panel.", this);

            if (endGameUI != null)
                endGameUI.ShowEndingPanel(ending);

            yield break;
        }

        SetBehaviours(false);

        if (hudController != null)
            hudController.HideHUD();

        //fade a negro
        yield return endGameUI.FadeBlackScreen(0f, 1f, fadeDuration);

        //con la pantalla en negro se acomoda todo
        KillAliveZombies();

        if (playerRoot != null)
            playerRoot.SetActive(false);

        group.SetActive(true);
        director.time = 0;
        director.Play();

        //la voz del rescate solo en la victoria, PlayDelayed no depende del timeScale
        if (ending == GameEnding.Victory && rescuedSource != null)
            rescuedSource.PlayDelayed(rescuedDelay);

        yield return new WaitForSecondsRealtime(blackHoldTime);

        //sale del negro con la cinematica ya corriendo
        yield return endGameUI.FadeBlackScreen(1f, 0f, fadeDuration);

        //espera el resto del timeline (con Hold el ultimo frame queda congelado)
        float remaining = (float)director.duration - blackHoldTime - fadeDuration;
        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);

        StartCoroutine(FadeOutHelicopters());
        endGameUI.ShowEndingPanel(ending);
    }

    //baja el volumen de los helicopteros hasta cortarlos, con tiempo sin escalar porque el panel final puede frenar el tiempo
    private IEnumerator FadeOutHelicopters()
    {
        if (helicopterSources == null || helicopterSources.Length == 0)
            yield break;

        float[] startVolumes = new float[helicopterSources.Length];

        for (int i = 0; i < helicopterSources.Length; i++)
            startVolumes[i] = helicopterSources[i] != null ? helicopterSources[i].volume : 0f;

        float elapsed = 0f;

        while (elapsed < helicopterFadeOut)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(elapsed / helicopterFadeOut);

            for (int i = 0; i < helicopterSources.Length; i++)
                if (helicopterSources[i] != null) helicopterSources[i].volume = startVolumes[i] * k;

            yield return null;
        }

        //se corta y se devuelve el volumen original por si la cinematica se repite
        for (int i = 0; i < helicopterSources.Length; i++)
        {
            if (helicopterSources[i] == null) continue;

            helicopterSources[i].Stop();
            helicopterSources[i].volume = startVolumes[i];
        }
    }

    //usa el mismo sistema de daño q el combate: cada zombie corre su propia muerte (EnemyDeath)
    private void KillAliveZombies()
    {
        foreach (GameObject zombie in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (zombie.TryGetComponent(out Health health))
                health.TakeDamage(health.Max);
        }
    }

    private void SetBehaviours(bool enabled)
    {
        foreach (Behaviour b in disableBeforeFade)
            if (b != null) b.enabled = enabled;
    }
}