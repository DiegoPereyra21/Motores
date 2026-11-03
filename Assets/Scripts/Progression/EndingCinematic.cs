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

        yield return new WaitForSecondsRealtime(blackHoldTime);

        //sale del negro con la cinematica ya corriendo
        yield return endGameUI.FadeBlackScreen(1f, 0f, fadeDuration);

        //espera el resto del timeline (con Hold el ultimo frame queda congelado)
        float remaining = (float)director.duration - blackHoldTime - fadeDuration;
        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);

        endGameUI.ShowEndingPanel(ending);
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