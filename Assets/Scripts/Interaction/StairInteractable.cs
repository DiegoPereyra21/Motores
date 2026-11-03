
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class StairInteractable : MonoBehaviour, IInteractable
{
    [System.Serializable]
    private class MaterialRequirement
    {
        public ItemData item;
        [Min(1)] public int amount = 1;
    }
    [Header("Configuracion")]
    [SerializeField] private bool enableBuildAction = false;

    [Header("Escaleras")]
    [SerializeField] private GameObject brokenStairs;
    [SerializeField] private GameObject repairedStairs;

    [Header("Inventario")]
    [SerializeField] private Inventory inventory;

    [Header("Acciones")]
    [SerializeField] private InteractionActionData examineAction;
    [SerializeField] private InteractionActionData buildAction;

    [Header("Materiales necesarios")]
    [SerializeField] private List<MaterialRequirement> materialsRequired = new();

    [TextArea(2, 4)]
    [SerializeField]
    private string buildSuccessMessage = "Has reconstruido la escalera.";

    //private bool hasBeenExamined;
    private bool isBuilt;

    private void Awake()
    {
        if (brokenStairs == null)
            brokenStairs = gameObject;

        if (repairedStairs != null)
            repairedStairs.SetActive(false);
    }

    public bool CanOpenInteraction(GameObject interactor)
    {
        if (interactor == null || isBuilt)
            return false;

        // si ya fue examinada y la construccion esta deshabilitada,deja de ser un objetivo interactuable
        //if (hasBeenExamined && !enableBuildAction)
        //    return false;

        return brokenStairs != null && brokenStairs.activeInHierarchy;
    }

    public IReadOnlyList<InteractionOption> GetActions(GameObject interactor)
    {
        List<InteractionOption> options = new();

        if (interactor == null || isBuilt)
            return options;

        if (examineAction != null)
        {
            options.Add(new InteractionOption(examineAction, true, resultMessage: examineAction.Description));
        }

        if (enableBuildAction && buildAction != null)
        {
            bool canBuild = CanBuild(interactor, out string reason);

            options.Add(new InteractionOption(buildAction, canBuild, reason, resultMessage: buildSuccessMessage));
        }

        return options;
    }

    public bool Interact(InteractionActionData action, GameObject interactor)
    {
        if (action == null || interactor == null || isBuilt)
            return false;

        switch (action.ActionId)
        {
            case "examine_stairs":
                return true;

            case "build_stairs":
                if (!enableBuildAction)
                    return false;

                return TryBuild(interactor);

            default:
                return false;
        }
    }

    private bool CanBuild(GameObject interactor, out string reason)
    {
        reason = "";

        Inventory playerInventory = GetInventory(interactor);

        if (playerInventory == null)
        {
            reason = "No se encontro el inventario.";
            return false;
        }

        StringBuilder missingMaterials = new();

        foreach (MaterialRequirement requirement in materialsRequired)
        {
            if (requirement == null || requirement.item == null)
                continue;

            int currentAmount = playerInventory.GetItemCount(requirement.item);

            if (currentAmount < requirement.amount)
            {
                if (missingMaterials.Length > 0)
                    missingMaterials.Append(", ");

                int missingAmount = requirement.amount - currentAmount;

                missingMaterials.Append($"{requirement.item.itemName} x{missingAmount}");
            }
        }

        if (missingMaterials.Length > 0)
        {
            reason = $"Faltan materiales: {missingMaterials}.";
            return false;
        }

        return true;
    }

    private bool TryBuild(GameObject interactor)
    {
        if (!CanBuild(interactor, out _))
            return false;

        Inventory playerInventory = GetInventory(interactor);

        // descuenta todos los materiales necesarios
        foreach (MaterialRequirement requirement in materialsRequired)
        {
            if (requirement == null || requirement.item == null)
                continue;

            bool removed = playerInventory.RemoveItem(requirement.item, requirement.amount);

            if (!removed)
            {
                Debug.LogError($"[StairInteractable] No se pudo descontar {requirement.item.itemName}."
                );

                return false;
            }
        }

        isBuilt = true;

        if (repairedStairs != null)
            repairedStairs.SetActive(true);

        if (brokenStairs != null)
            brokenStairs.SetActive(false);

        return true;
    }

    private Inventory GetInventory(GameObject interactor)
    {
        if (inventory != null)
            return inventory;

        inventory = interactor.GetComponent<Inventory>();

        if (inventory == null)
            inventory = interactor.GetComponentInChildren<Inventory>();

        return inventory;
    }
}
