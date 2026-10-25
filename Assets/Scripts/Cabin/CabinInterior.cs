using System.Collections.Generic;
using UnityEngine;

// Marca el interior de la cabaña (un BoxCollider en modo Trigger que cubre el piso, de piso a techo).
// Los zombies lo usan para saber si el player esta adentro (y buscar una abertura por donde entrar) o afuera (y simplemente perseguirlo).
// Se pueden poner varios si la cabaña no es un rectangulo.
[RequireComponent(typeof(Collider))]
public class CabinInterior : MonoBehaviour
{
    private static readonly List<CabinInterior> All = new();

    private Collider zone;

    //true si hay al menos una zona de interior en la escena
    public static bool Exists => All.Count > 0;

    private void Awake()
    {
        zone = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    //true si el punto esta adentro de alguna de las zonas
    public static bool Contains(Vector3 point)
    {
        foreach (CabinInterior interior in All)
        {
            //ClosestPoint devuelve el mismo punto si esta adentro del collider
            Vector3 closest = interior.zone.ClosestPoint(point);

            if ((closest - point).sqrMagnitude < 0.0001f)
                return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
        Gizmos.DrawWireCube(box.center, box.size);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
