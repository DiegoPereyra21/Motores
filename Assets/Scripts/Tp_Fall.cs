using UnityEngine;

public class Tp_Fall : MonoBehaviour
{
    [Tooltip("Lugar a donde se va a teletransportar el player")]
    public Transform destino;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            other.transform.position = destino.position;

            if (cc != null) cc.enabled = true;
        }
    }
}