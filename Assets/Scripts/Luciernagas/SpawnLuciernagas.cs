using UnityEngine;

public class SpawnLuciernagas : MonoBehaviour
{
    [Header("Luciernaga (padre)")]
    [SerializeField] private GameObject Luciernaga; 

    private GameProgressController progress;

    private void Start()
    {
        progress = GameProgressController.Instance;

        if (progress == null)
        {
            Debug.LogWarning("No hay GameProgressController en la escena.");
            return;
        }

        progress.OnPhaseChanged += HandlePhaseChanged;
        HandlePhaseChanged(progress.CurrentPhase);
    }

    private void OnDestroy()
    {
        if (progress != null)
            progress.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(DayNightPhase phase)
    {
        if (Luciernaga != null)
            Luciernaga.SetActive(phase == DayNightPhase.Night);
    }
}