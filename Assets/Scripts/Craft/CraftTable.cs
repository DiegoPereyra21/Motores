using UnityEngine;
using Unity.Cinemachine;

public class CraftTable : MonoBehaviour
{
    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private CraftingRecipe[] availableRecipes;

    [SerializeField] private PlayerController playerController;
    [SerializeField] private CinemachineBrain cameraBrain;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        craftingUI.Show(availableRecipes);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        playerController.enabled = false;
        cameraBrain.enabled = false;
    }

    public void CloseCrafting()
    {
        craftingUI.Hide();

        playerController.enabled = true;
        cameraBrain.enabled = true;

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}