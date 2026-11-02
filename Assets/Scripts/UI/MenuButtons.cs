using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuBotones : MonoBehaviour
{
    public void Jugar(string escena) => SceneManager.LoadScene(escena);
}