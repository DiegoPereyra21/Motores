using UnityEngine;

//cuando termina el mensaje de radio de un dia nuevo, cada lugar vacio tiene una chance de que crezca su planta
public class GardenManager : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float growChance = 0.25f;
    [SerializeField] private RadioInteractable radio; //si se deja vacio lo busca solo
    [SerializeField] private PlantSpot[] spots; //si se deja vacio los busca solo

    private int lastRolledDay;

    private void Start()
    {
        if (radio == null) radio = FindFirstObjectByType<RadioInteractable>();

        if (radio == null)
        {
            Debug.LogError("[GardenManager] No se encontro RadioInteractable.", this);
            return;
        }

        if (spots == null || spots.Length == 0)
            spots = FindObjectsByType<PlantSpot>(FindObjectsSortMode.None);

        //el dia inicial no sortea, solo los siguientes
        GameProgressController progress = GameProgressController.Instance;
        lastRolledDay = progress != null ? progress.CurrentDay : 1;

        radio.OnMessageFinished += HandleMessageFinished;
    }

    private void OnDestroy()
    {
        if (radio != null)
            radio.OnMessageFinished -= HandleMessageFinished;
    }

    private void HandleMessageFinished(int day)
    {
        //la radio se puede volver a escuchar: solo se sortea la primera vez de cada dia
        if (day <= lastRolledDay) return;

        lastRolledDay = day;

        foreach (PlantSpot spot in spots)
        {
            if (!spot.IsGrown && Random.value < growChance)
                spot.SetGrown(true);
        }
    }
}