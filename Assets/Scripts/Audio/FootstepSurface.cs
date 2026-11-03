using UnityEngine;

//tipos de piso que tienen sonido de paso propio
public enum FootstepSurfaceType { Grass, Dirt, Wood, Bridge }

//Se pone en los pisos que NO son terreno (piso de la cabaña, puente, etc.) para que PlayerFootsteps sepa que sonido de paso usar.
//Va en el objeto que tiene el collider del piso (o en un padre de ese objeto). El terreno no lo necesita: se detecta por su textura.
public class FootstepSurface : MonoBehaviour
{
    [SerializeField] private FootstepSurfaceType surface = FootstepSurfaceType.Wood;

    public FootstepSurfaceType Surface => surface;
}
