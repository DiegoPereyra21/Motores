using UnityEngine;
using Unity.Cinemachine;

public class CraftTable : MonoBehaviour
{
    [Header("Crafting")]
    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private CraftingProgression craftingProgression;

    [Header("Jugador")]
    [SerializeField] private PlayerController playerController;

    public CraftingProgression Progression => craftingProgression;

    private Animator playerAnimator;
    private int currentDay = 1;
    private bool isCrafting;

    private void Awake()
    {
        if (playerController != null)
            playerAnimator = playerController.GetComponentInChildren<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isCrafting)
            return;

        if (craftingUI == null || craftingProgression == null)
        {
            Debug.LogError("[CraftTable] Falta asignar CraftingUI o CraftingProgression.");
            return;
        }

        if (playerController != null)
        {
            playerController.enabled = false;

            if (playerAnimator != null)
                playerAnimator.speed = 0f;
        }

        currentDay = GameProgressController.Instance.CurrentDay;

        CraftingRecipe[] allRecipes = craftingProgression.GetAllRecipes();

        craftingUI.Show(allRecipes, currentDay);

        isCrafting = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseCrafting()
    {
        if (!isCrafting)
            return;

        if (playerController != null)
        {
            playerController.enabled = true;

            if (playerAnimator != null)
                playerAnimator.speed = 1f;
        }

        isCrafting = false;

        if (craftingUI != null)
            craftingUI.Hide();

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
        {
            playerController.enabled = true;

            if (playerAnimator != null)
                playerAnimator.speed = 1f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}