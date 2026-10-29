using UnityEngine;

public static class FrameRateLimiter
{
    private const int TargetFps = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // En web no se fuerza: el navegador ya sincroniza con el monitor (60 Hz en la mayoría).
        // Forzar targetFrameRate en WebGL usa setTimeout y puede dar tirones.
        return;
#else
        // Con VSync activo, targetFrameRate se ignora
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFps;
#endif
    }
}