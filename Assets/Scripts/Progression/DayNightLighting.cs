using UnityEngine;

public class DayNightLighting : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameProgressController gameProgressController;
    [SerializeField] private Light directionalSunLight;

    [Header("Dia")]
    [SerializeField] private Color dayLightColor = Color.white;
    [SerializeField] private float dayLightIntensity = 1.2f;
    [SerializeField] private float sunriseRotationAngle = 10f;
    [SerializeField] private float sunsetRotationAngle = 170f;

    [Header("Noche")]
    [SerializeField] private Color nightLightColor = new Color(0.15f, 0.2f, 0.35f);
    [SerializeField] private float nightLightIntensity = 0.05f;

    [Header("Skybox")]
    [SerializeField] private float daySkyboxExposure = 1.3f;
    [SerializeField] private float nightSkyboxExposure = 0.1f;

    private float initialSunYRotation;
    private Material originalSkyboxMaterial;
    private Material runtimeSkyboxMaterial;

    // guarda la rotacion inicial del sol y la referencia al skybox original
    private void Awake()
    {
        if (directionalSunLight == null)
        {
            Debug.LogError("[DayNightLighting] No se asigno la luz direccional.", this);
            enabled = false;
            return;
        }

        initialSunYRotation = directionalSunLight.transform.eulerAngles.y;

        originalSkyboxMaterial = RenderSettings.skybox;
    }

    // prepara el skybox, se suscribe a eventos y actualiza el estado visual inicial
    private void OnEnable()
    {
        if (gameProgressController == null)
        {
            Debug.LogError("[DayNightLighting] No se asigno GameProgressController.", this);
            return;
        }

        CreateRuntimeSkybox();

        gameProgressController.OnPhaseChanged += UpdateEnvironmentLighting;
        gameProgressController.OnPhaseProgressChanged += UpdateSunRotation;

        UpdateEnvironmentLighting(gameProgressController.CurrentPhase);
        UpdateSunRotation(CalculatePhaseProgress());
    }

    // cancela las suscripciones a eventos y restaura el skybox original
    private void OnDisable()
    {
        if (gameProgressController != null)
        {
            gameProgressController.OnPhaseChanged -= UpdateEnvironmentLighting;
            gameProgressController.OnPhaseProgressChanged -= UpdateSunRotation;
        }

        RestoreSkybox();
    }

    // actualiza el color y la intensidad del skybox segun la fase actual
    private void UpdateEnvironmentLighting(DayNightPhase phase)
    {
        bool isDay = phase == DayNightPhase.Day;

        directionalSunLight.color = isDay ? dayLightColor : nightLightColor;
        directionalSunLight.intensity = isDay ? dayLightIntensity : nightLightIntensity;

        if (runtimeSkyboxMaterial != null)
        {
            runtimeSkyboxMaterial.SetFloat("_Exposure", isDay ? daySkyboxExposure : nightSkyboxExposure);
        }
    }

    // calcula la rotacion del "sol" segun el progreso de la fase actual
    private void UpdateSunRotation(float progress01)
    {
        float angle;

        if (gameProgressController.CurrentPhase == DayNightPhase.Day)
        {
            angle = Mathf.Lerp(sunriseRotationAngle, sunsetRotationAngle, progress01);
        }
        else
        {
            angle = Mathf.Lerp(sunsetRotationAngle, sunriseRotationAngle + 360f, progress01);
        }

        directionalSunLight.transform.rotation = Quaternion.Euler(angle, initialSunYRotation, 0f);
    }

    // calcula el progreso de la fase actual con un valor entre 0 y 1
    private float CalculatePhaseProgress()
    {
        if (gameProgressController.CurrentPhaseDuration <= 0f)
            return 0f;

        float progress = 1f - gameProgressController.PhaseRemainingTime / gameProgressController.CurrentPhaseDuration;

        return Mathf.Clamp01(progress);
    }

    // crea una copia del material original del skybox para modificarlo sin alterar el asset
    private void CreateRuntimeSkybox()
    {
        if (originalSkyboxMaterial == null)
            return;

        if (runtimeSkyboxMaterial != null)
            return;

        runtimeSkyboxMaterial = new Material(originalSkyboxMaterial);
        runtimeSkyboxMaterial.name = originalSkyboxMaterial.name + " (Runtime)";

        RenderSettings.skybox = runtimeSkyboxMaterial;
    }

    // restaura el skybox original y destruye la instancia temporal
    private void RestoreSkybox()
    {
        if (runtimeSkyboxMaterial == null)
            return;

        if (RenderSettings.skybox == runtimeSkyboxMaterial)
            RenderSettings.skybox = originalSkyboxMaterial;

        Destroy(runtimeSkyboxMaterial);
        runtimeSkyboxMaterial = null;
    }
}