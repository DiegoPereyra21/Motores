using UnityEngine;

//Reacciona al final de la demo (cuando se completa la ultima noche configurada en DayNightCycle.cs)
//mostrando un mensaje en pantalla y pausando el juego. Es la contraparte "positiva" de PlayerDeath.cs:
//mientras ese script maneja que el jugador PIERDA (muere a manos de los zombies), este maneja que
//el jugador GANE (sobrevive todas las noches previstas). Ambos usan la misma tecnica para cerrar el
//juego: activar un GameObject de UI y frenar todo con Time.timeScale = 0.
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

    //se ejecuta una sola vez, cuando DayNightCycle.cs dispara onDemoEnd (justo al terminar la Noche 3).
    private void HandleDemoEnd()
    {
        if (demoEndUI != null)
            demoEndUI.SetActive(true);

        //misma pausa total que usa PlayerDeath.cs al morir el jugador: congela zombies, fisica y
        //cualquier timer basado en Time.deltaTime/Time.time, incluido el propio DayNightCycle
        Time.timeScale = 0f;

        Debug.Log("Demo completada: el jugador sobrevivio las 3 noches");
    }
}