using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RadioMenu : MonoBehaviour
{
    [SerializeField] private Image perilla;
    [SerializeField] private Sprite[] posiciones;   
    [SerializeField] private float tiempoPorPaso = 0.08f;
    [SerializeField] private string escenaJuego = "Map";
    [SerializeField] private GameObject panelInfo;

    private int actual = 0;
    private Coroutine giro;

    public void IrA(int destino)
    {
        if (giro != null) StopCoroutine(giro);
        giro = StartCoroutine(Girar(destino));
    }

    private IEnumerator Girar(int destino)
    {
        while (actual != destino)
        {
            actual += (int)Mathf.Sign(destino - actual);
            perilla.sprite = posiciones[actual];
            yield return new WaitForSeconds(tiempoPorPaso);
        }
    }

    public void Jugar() => SceneManager.LoadScene(escenaJuego);
    public void AlternarInfo() => panelInfo.SetActive(!panelInfo.activeSelf);
    public void Salir() => Application.Quit();
}