using UnityEngine;

//Condicion de victoria si el player sobrevive toda la demo. Cierra todo.
public class DemoEndUI : MonoBehaviour
{
    [SerializeField] private DayNightCycle dayNightCycle; //el "reloj" del que escucha el evento de fin de demo
    [SerializeField] private GameObject demoEndUI; //texto/panel de "Fin de la demo", desactivado por defecto en la escena

    private void OnEnable()
    {
        dayNightCycle.onDemoEnd.AddListener(HandleDemoEnd);
    }

    private void OnDisable()
    {
        dayNightCycle.onDemoEnd.RemoveListener(HandleDemoEnd);
    }

    //se ejecuta una sola vez, cuando DayNightCycle.cs dispara onDemoEnd (justo al terminar la noche 3).
    private void HandleDemoEnd()
    {
        if (demoEndUI != null)
            demoEndUI.SetActive(true);

        //misma pausa total que usa PlayerDeath.cs al morir el jugador: congela zombies, fisica y
        //cualquier timer basado en Time.deltaTime/Time.time, incluido el propio DayNightCycle
        Time.timeScale = 0f;

        Debug.Log("Demo completed: the player survived all nights");
    }
}