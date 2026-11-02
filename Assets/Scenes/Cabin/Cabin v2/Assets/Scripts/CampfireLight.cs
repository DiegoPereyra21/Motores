
using UnityEngine;

[RequireComponent(typeof(Light))]
public class CampfireLight : MonoBehaviour
{
    [Header("Intensidad")]
    [SerializeField] private float intensidadBase = 2.5f;
    [SerializeField] private float variacionIntensidad = 0.35f;

    [Header("Alcance")]
    [SerializeField] private float rangoBase = 8f;

    [Range(0f, 0.2f)]
    [SerializeField] private float variacionRango = 0.04f;

    [Header("Parpadeo")]
    [SerializeField] private float velocidad = 2f;

    [Header("Color")]
    [SerializeField]
    private Color colorCalido =
        new Color(1f, 0.45f, 0.15f);

    [SerializeField]
    private Color colorBrillante =
        new Color(1f, 0.65f, 0.3f);

    private Light luz;
    private float semilla;

    private void Awake()
    {
        luz = GetComponent<Light>();
        semilla = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        float ruido = Mathf.PerlinNoise(
            semilla,
            Time.time * velocidad
        );

        float variacion = (ruido - 0.5f) * 2f;

        // Variación suave de intensidad
        luz.intensity = Mathf.Max(
            0f,
            intensidadBase + variacion * variacionIntensidad
        );

        // Variación proporcional del alcance
        luz.range = Mathf.Max(
            0.01f,
            rangoBase * (1f + variacion * variacionRango)
        );

        // Variación de color
        luz.color = Color.Lerp(
            colorCalido,
            colorBrillante,
            ruido
        );
    }
}
