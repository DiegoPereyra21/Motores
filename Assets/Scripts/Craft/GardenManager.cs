using UnityEngine;

//EL RESPAWN ES LUEGO DE TOCAR LA RADIO, NO AL ACABAR LA NOCHE
public class GardenManager : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float growChance = 0.25f;
    [SerializeField] private RadioInteractable radio; //lo busca solo x si me olvido
    [SerializeField] private PlantSpot[] spots; //lo mismo
    //privadas
    private int lastRolledDay;

    private void Start()
    {
        if (radio == null) radio = FindFirstObjectByType<RadioInteractable>();

        if (radio == null)
        {
            Debug.LogError("NO HAY RADIO EN EL NIVEL", this);
            return;
        }

        if (spots == null || spots.Length == 0)
            spots = FindObjectsByType<PlantSpot>(FindObjectsSortMode.None);

        //el dia inicial no hay sorteo, los siguientes si
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
        //solo la primera vez de cada dia, para q no respawnee cada que tocas la radio
        if (day <= lastRolledDay) return;

        lastRolledDay = day;

        foreach (PlantSpot spot in spots)
        {
            if (!spot.IsGrown && Random.value < growChance)
                spot.SetGrown(true);
        }
    }
}