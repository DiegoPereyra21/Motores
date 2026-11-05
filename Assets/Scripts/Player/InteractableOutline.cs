using System.Collections.Generic;
using UnityEngine;

//resalta con outline el interactuable que el player esta mirando, sin tocar cada interactable
public class InteractableOutline : MonoBehaviour
{
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private Material outlineMaterial;

    private readonly List<Renderer> outlined = new();
    private readonly List<Material> tempMaterials = new();

    private void Awake()
    {
        if (!playerInteraction) playerInteraction = GetComponent<PlayerInteraction>();
    }

    private void OnEnable()
    {
        playerInteraction.OnTargetChanged += HandleTargetChanged;
    }

    private void OnDisable()
    {
        playerInteraction.OnTargetChanged -= HandleTargetChanged;
        Clear();
    }

    private void HandleTargetChanged(IInteractable target)
    {
        Clear();

        if (target is not Component component) return;

        foreach (Renderer r in component.GetComponentsInChildren<Renderer>())
        {
            if (r is not MeshRenderer && r is not SkinnedMeshRenderer) continue;

            r.GetSharedMaterials(tempMaterials);
            tempMaterials.Add(outlineMaterial);
            r.SetSharedMaterials(tempMaterials);
            outlined.Add(r);
        }
    }

    private void Clear()
    {
        foreach (Renderer r in outlined)
        {
            if (r == null) continue;

            r.GetSharedMaterials(tempMaterials);
            tempMaterials.Remove(outlineMaterial);
            r.SetSharedMaterials(tempMaterials);
        }

        outlined.Clear();
    }
}