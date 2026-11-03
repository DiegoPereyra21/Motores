using System;
using System.Collections.Generic;
using UnityEngine;

public class Barricade : MonoBehaviour, IDamageable
{
    [Header("Bloqueo")]
    [SerializeField] private float blockerThickness = 0.4f;

    private readonly List<GameObject> planks = new();
    private BoxCollider blocker;

    private float maxDurability;
    private float currentDurability;

    public bool IsBroken => currentDurability <= 0f;
    public float Normalized => maxDurability > 0f
        ? currentDurability / maxDurability
        : 0f;

    public event Action<float> OnDurabilityChanged;
    public event Action OnBroken;

    public void Build(GameObject plankPrefab, Vector2 openingSize, float durability, int plankCount, float minAngle, float maxAngle, float depthVariation)
    {
        if (plankPrefab == null)
        {
            Debug.LogError("[Barricade] No se asignp un prefab de tabla.", this);
            return;
        }

        ClearPlanks();

        maxDurability = Mathf.Max(1f, durability);
        currentDurability = maxDurability;

        gameObject.SetActive(true);

        SetupBlocker(openingSize);

        BuildPlanks(plankPrefab, openingSize, plankCount, minAngle, maxAngle, depthVariation);

        OnDurabilityChanged?.Invoke(Normalized);
    }

    public void TakeDamage(float amount)
    {
        if (IsBroken || amount <= 0f)
            return;

        currentDurability = Mathf.Max(currentDurability - amount, 0f);
        OnDurabilityChanged?.Invoke(Normalized);

        if (IsBroken)
        {
            ClearPlanks();
            gameObject.SetActive(false);
            OnBroken?.Invoke();
            return;
        }

        UpdateVisual();
    }

    private void SetupBlocker(Vector2 openingSize)
    {
        if (blocker == null)
            blocker = gameObject.AddComponent<BoxCollider>();

        blocker.isTrigger = false;
        blocker.center = Vector3.zero;
        blocker.size = new Vector3(openingSize.x, openingSize.y, blockerThickness);
    }

    private void BuildPlanks(GameObject plankPrefab, Vector2 openingSize, int plankCount, float minAngle, float maxAngle, float depthVariation)
    {
        Vector3 prefabScale = plankPrefab.transform.localScale;

        float plankLength = openingSize.x * 1.08f;

        int count = Mathf.Clamp(plankCount, 1, 12);

        for (int i = 0; i < count; i++)
        {
            GameObject plank = Instantiate(plankPrefab, transform);
            plank.name = $"Plank_{i + 1}";

            foreach (Collider col in plank.GetComponentsInChildren<Collider>())
                Destroy(col);

            float normalizedPosition = count == 1 ? 0f : (float)i / (count - 1);

            float y = Mathf.Lerp(-openingSize.y * 0.5f, openingSize.y * 0.5f, normalizedPosition);

            y += UnityEngine.Random.Range(-0.12f, 0.12f);

            float angle = UnityEngine.Random.Range(minAngle, maxAngle);

            if (UnityEngine.Random.value < 0.5f)
                angle *= -1f;

            float depth = UnityEngine.Random.Range(-depthVariation, depthVariation);

            plank.transform.localPosition = new Vector3(0f, y, depth);

            plank.transform.localRotation = Quaternion.Euler(0f, 90f, 90f + angle);

            plank.transform.localScale = new Vector3(prefabScale.x, prefabScale.y, plankLength);

            planks.Add(plank);
        }

        for (int i = planks.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (planks[i], planks[j]) = (planks[j], planks[i]);
        }
    }

    private void UpdateVisual()
    {
        int visible = Mathf.CeilToInt(planks.Count * Normalized);

        for (int i = 0; i < planks.Count; i++)
        {
            if (planks[i] != null)
                planks[i].SetActive(i < visible);
        }
    }

    private void ClearPlanks()
    {
        foreach (GameObject plank in planks)
        {
            if (plank != null)
                Destroy(plank);
        }

        planks.Clear();
    }
}