using UnityEngine;
using Unity.Cinemachine;

public class CraftTable : MonoBehaviour
{
    [Header("Crafting")]
    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private CraftingProgression craftingProgression;

    [Header("Jugador")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CinemachineBrain cameraBrain;

    // temp
    private int currentDay = 1;

    private bool isCrafting;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isCrafting)
            return;

        if (craftingUI == null || craftingProgression == null)
        {
            Debug.LogError("[CraftTable] Falta asignar CraftingUI o CraftingProgression.");
            return;
        }

        CraftingRecipe[] availableRecipes = craftingProgression.GetAvailableRecipes(currentDay);

        craftingUI.Show(availableRecipes);

        isCrafting = true;

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerController != null)
            playerController.enabled = false;

        if (cameraBrain != null)
            cameraBrain.enabled = false;
    }

    public void CloseCrafting()
    {
        if (!isCrafting)
            return;

        isCrafting = false;

        if (craftingUI != null)
            craftingUI.Hide();

        if (playerController != null)
            playerController.enabled = true;

        if (cameraBrain != null)
            cameraBrain.enabled = true;

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        if (!isCrafting)
            return;

        isCrafting = false;

        if (craftingUI != null)
            craftingUI.Hide();

        if (playerController != null)
            playerController.enabled = true;

        if (cameraBrain != null)
            cameraBrain.enabled = true;

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}