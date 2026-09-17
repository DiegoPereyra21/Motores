using System.Collections;
using UnityEngine;

//funcion buenisima q sirve para cualquier objeto con health para q se muestre cuando recibe daño
[RequireComponent(typeof(Health))]
public class DamageFlash : MonoBehaviour
{
    //config
    private Color flashColor = Color.red;
    [SerializeField] private Color deadColor = new Color(0.35f, 0.35f, 0.35f); //gris para cuando muere el zombie, no se reconocia bien antes
    private float flashDuration = 0.15f;//ajustar a mano luego
    [SerializeField] private Renderer[] renderers; //en caos del player conviene dejarlo vacio, porque tiene muchos renderers mezclaods
    //esto es donde mas chance hay de que falle, pero por el momento anda
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");


    //privadas
    private Health health;
    private MaterialPropertyBlock block;
    private float lastNormalized = 1f;
    private Coroutine flashRoutine;

    private void Awake()
    {
        health = GetComponent<Health>();
        block = new MaterialPropertyBlock();
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnEnable()
    {
        health.onHealthChanged.AddListener(OnHealthChanged);
    }
    private void OnDisable()
    {
        health.onHealthChanged.RemoveListener(OnHealthChanged);
        ClearFlash(); //por si se desactiva en medio del flash
    }

    private void OnHealthChanged(float normalized)
    {
        //si murio con este golpe, se queda gris para siempre (no ejecuta el flash normal)
        if (health.IsDead)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            ApplyDeadColor();
            return;
        }

        //solo flashea si bajo la vida  (luego hacer alguna forma para q al curarse no se ponga roojo, sino verde, pero para el futuro)
        if (normalized < lastNormalized)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(Flash());
        }
        lastNormalized = normalized;
    }

    private void ApplyDeadColor()
    {
        //mismo mecanismo que el flash, pero sin corrutina para que no vuelva al color original
        foreach (Renderer r in renderers)
        {
            if (!r) continue;
            for (int i = 0; i < r.sharedMaterials.Length; i++)
            {
                r.GetPropertyBlock(block, i);
                block.SetColor(BaseColorId, deadColor);
                block.SetColor(ColorId, deadColor);
                r.SetPropertyBlock(block, i);
            }
        }
    }
    private IEnumerator Flash()
    {
        //MaterialPropertyBlock cambia el color solo en este renderer, para q no crear copia del material
        foreach (Renderer r in renderers)
        {
            if (!r) continue;
            for (int i = 0; i < r.sharedMaterials.Length; i++)
            {
                r.GetPropertyBlock(block, i);
                block.SetColor(BaseColorId, flashColor);
                block.SetColor(ColorId, flashColor);
                r.SetPropertyBlock(block, i);
            }
        }

        yield return new WaitForSeconds(flashDuration);
        ClearFlash();
        flashRoutine = null;
    }
    private void ClearFlash()
    {
        //quitar el property block restaura el color original del material
        foreach (Renderer r in renderers)
        {
            if (!r) continue;
            for (int i = 0; i < r.sharedMaterials.Length; i++)
                r.SetPropertyBlock(null, i);
        }
    }
}