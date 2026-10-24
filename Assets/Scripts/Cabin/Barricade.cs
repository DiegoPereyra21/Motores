using System;
using System.Collections.Generic;
using UnityEngine;

//La barricada ya puesta en una abertura. Tiene durabilidad y recibe daño como cualquier IDamageable (los zombies la golpean).
//Las tablas se arman solas con el prefab del item (PlaceableData.prefab), ajustadas al tamaño de la abertura.
public class Barricade : MonoBehaviour, IDamageable
{
    [SerializeField] private float plankSpacing = 1.15f; //separacion entre tablas, 1 = pegadas, mas = con huecos
    [SerializeField] private float plankOverhang = 1.08f; //las tablas sobresalen un poco del ancho de la abertura

    private readonly List<GameObject> planks = new();
    private float maxDurability;
    private float currentDurability;

    public bool IsBroken => currentDurability <= 0f;
    public float Normalized => maxDurability > 0f ? currentDurability / maxDurability : 0f;

    public event Action<float> OnDurabilityChanged; //durabilidad normalizada (0 a 1)
    public event Action OnBroken;

    //arma la barricada nueva con la durabilidad que dice el item
    public void Build(GameObject plankPrefab, Vector2 openingSize, float durability)
    {
        ClearPlanks();

        maxDurability = Mathf.Max(1f, durability);
        currentDurability = maxDurability;

        gameObject.SetActive(true);
        BuildPlanks(plankPrefab, openingSize);

        OnDurabilityChanged?.Invoke(Normalized);
    }

    public void TakeDamage(float amount)
    {
        if (IsBroken || amount <= 0f) return;

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

    //a medida que pierde durabilidad se van sacando tablas
    private void UpdateVisual()
    {
        int visible = Mathf.CeilToInt(planks.Count * Normalized);

        for (int i = 0; i < planks.Count; i++)
            planks[i].SetActive(i < visible);
    }

    private void BuildPlanks(GameObject plankPrefab, Vector2 openingSize)
    {
        //las tablas del prefab miden unos 0.3 de ancho y 1.85 de largo (el largo es el eje Z)
        Vector3 prefabScale = plankPrefab != null ? plankPrefab.transform.localScale : new Vector3(0.3f, 0.05f, 1.85f);
        float plankWidth = prefabScale.x;
        int count = Mathf.Clamp(Mathf.RoundToInt(openingSize.y / (plankWidth * plankSpacing)), 3, 10);

        for (int i = 0; i < count; i++)
        {
            GameObject plank = plankPrefab != null ? Instantiate(plankPrefab, transform) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.transform.SetParent(transform, false);
            plank.name = $"Plank_{i + 1}";

            //las tablas son solo visuales: no deben bloquear al player ni el raycast
            foreach (Collider col in plank.GetComponentsInChildren<Collider>())
                Destroy(col);

            //horizontales, repartidas en el alto de la abertura, con un poquito de desorden para que no se vean perfectas
            float y = -openingSize.y * 0.5f + openingSize.y * (i + 0.5f) / count;
            float roll = UnityEngine.Random.Range(-4f, 4f);
            float depth = (i % 2 == 0) ? 0.03f : -0.03f;

            plank.transform.localPosition = new Vector3(0f, y, depth);
            plank.transform.localRotation = Quaternion.Euler(0f, 0f, roll) * Quaternion.Euler(0f, 90f, 0f);
            plank.transform.localScale = new Vector3(prefabScale.x, prefabScale.y, openingSize.x * plankOverhang);

            planks.Add(plank);
        }

        //mezcla el orden, asi las tablas que se caen primero son al azar y no siempre las de arriba
        for (int i = planks.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (planks[i], planks[j]) = (planks[j], planks[i]);
        }
    }

    private void ClearPlanks()
    {
        foreach (GameObject plank in planks)
        {
            if (plank != null) Destroy(plank);
        }

        planks.Clear();
    }
}
