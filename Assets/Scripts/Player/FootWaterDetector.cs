
using UnityEngine;

public class FootWaterDetector : MonoBehaviour
{
    [SerializeField] private GameObject splashPrefab;
    [SerializeField] private LayerMask waterLayer;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<CharacterController>() != null)
            return;

        Debug.Log("aca");

        if ((waterLayer.value & (1 << other.gameObject.layer)) == 0)
            return;

        Vector3 position = transform.position;
        position.y = other.bounds.max.y;

        GameObject splash = Instantiate(
            splashPrefab,
            position,
            Quaternion.identity
        );

        ParticleSystem[] particles =
            splash.GetComponentsInChildren<ParticleSystem>();

        float duration = 0f;

        foreach (ParticleSystem ps in particles)
        {
            ps.Play();

            float particleDuration =
                ps.main.duration + ps.main.startLifetime.constantMax;

            duration = Mathf.Max(duration, particleDuration);
        }

        Destroy(splash, duration > 0f ? duration : 3f);
    }
}
