using UnityEngine;

[RequireComponent(typeof(Health))]//aunque se sobreentienda q el player debe tener health, por las dudas
public class PlayerDeath : MonoBehaviour
{
    //referencias
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraPivot cameraPivot;

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

        Debug.Log("Player died");
    }
}