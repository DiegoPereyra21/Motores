using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    public void Jugar(string escena)
    {
        if (SceneLoader.IsLoading) return;//evita doble click mientras carga
        SceneLoader.Load(escena);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}