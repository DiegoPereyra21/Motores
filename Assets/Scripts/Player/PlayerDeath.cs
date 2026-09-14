using UnityEngine;

[RequireComponent(typeof(Health))]//aunque se sobreentienda q el player debe tener health, por las dudas
public class PlayerDeath : MonoBehaviour
{
    //referencias
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraPivot cameraPivot;
    [SerializeField] private GameObject playerVisual; //el modelo/geometria del player (ej. "Geometry"), se oculta al morir (esto es hasta que tengamos death animation)
    [SerializeField] private GameObject gameOverUI; //texto/panel de "Has muerto", desactivado por defecto en la escena

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

        //ocultar al player al morir
        if (playerVisual != null)
            playerVisual.SetActive(false);

        //mensaje de game over
        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        //pausa el juego: frena zombies, fisica y cualquier timer basado en Time.deltaTime/Time.time
        Time.timeScale = 0f;

        Debug.Log("Player died");
    }
}