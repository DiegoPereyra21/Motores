using System;
using UnityEngine;

// Una linea de voz: el clip y su volumen propio. El volumen sirve para parejar clips que vienen mas fuertes que otros (nunca sube de 1).
// La usan PlayerVoice y ZombieVoice.
[Serializable]
public class VoiceLine
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
}
