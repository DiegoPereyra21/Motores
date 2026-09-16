using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Health))]//aunque se sobreentienda q el player debe tener health, por las dudas
public class PlayerDeath : MonoBehaviour
{
    //referencias
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraPivot cameraPivot;
    [SerializeField] private GameObject playerVisual; //ESTA SOLUCION HASTA Q TENGAMOS ANIMACION DE MUERTE
    [SerializeField] private GameObject gameOverUI;
    //volver al menu
    [SerializeField] private float secondsBeforeMainMenu = 3f; //cuanto se queda el panel antes de volver
    private string mainMenuSceneName = "MainMenu"; //creo q nunca cambiara de nombre pero x las dudas

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
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

        //ocultar al player al morir (LUEGO ANIMACION DE MUERTE)
        if (playerVisual != null)
            playerVisual.SetActive(false);

        //mensaje de game over
        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        //frena todo
        Time.timeScale = 0f;

        //waitforsecondsrealtime ignora el timescale, por eso funciona con el juego pausado
        StartCoroutine(GoToMainMenuAfterDelay());
    }

    private System.Collections.IEnumerator GoToMainMenuAfterDelay()
    {
        yield return new WaitForSecondsRealtime(secondsBeforeMainMenu);

        //reactivo el timeScale antes de cargar, sino el menu tambien queda congelado
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}