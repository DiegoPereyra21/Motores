using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Health))]//aunque se sobreentienda q el player debe tener health, por las dudas
public class PlayerDeath : MonoBehaviour
{
    //referencias
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraPivot cameraPivot;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject gameOverUI;
    //tiempos en segundos REALES
    [SerializeField] private float secondsBeforeGameOverUI = 1.2f; //lo que dura la animacion de muerte antes de mostrar el panel
    [SerializeField] private float secondsBeforeMainMenu = 3f; //cuanto se queda el panel antes de volver
    private string mainMenuSceneName = "MainMenu"; //creo q nunca cambiara de nombre pero x las dudas

    private static readonly int DeathHash = Animator.StringToHash("Death");
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (!animator) animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        health.onDied.AddListener(HandleDeath);
    }

    private void OnDisable()
    {
        health.onDied.RemoveListener(HandleDeath);
    }

    private void HandleDeath()
    {
        playerController.enabled = false;
        cameraPivot.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //apaga la capa hurt para q no moleste a la anim de death
        int hurtLayer = animator.GetLayerIndex("Hurt");
        if (hurtLayer >= 0)
            animator.SetLayerWeight(hurtLayer, 0f);

        //para q la animacion termine aunque se pause
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.SetTrigger(DeathHash);
        //frena todo, como lo teniamos
        Time.timeScale = 0f;
        StartCoroutine(GameOverRoutine());
    }

    private System.Collections.IEnumerator GameOverRoutine()
    {
        //waitforsecondsrealtime ignora el timescale, por eso funciona con el juego pausado
        yield return new WaitForSecondsRealtime(secondsBeforeGameOverUI);

        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        yield return new WaitForSecondsRealtime(secondsBeforeMainMenu);

        //reactivo el timeScale antes de cargar, sino el menu tambien queda congelado
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}