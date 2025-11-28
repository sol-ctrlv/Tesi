using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemSpawner : MonoBehaviour
{
    public enum ParticleType { Death, LAST }

    public static ParticleSystemSpawner Instance;
    [SerializeField] private Dictionary<ParticleType, ParticleSystem> particleSystems;
    [SerializeField] private ParticleSystem deathVFX;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            particleSystems = new Dictionary<ParticleType, ParticleSystem>();
            particleSystems.Add(ParticleType.Death, deathVFX);
        }
    }


    private void Emit(Vector2 position, ParticleType type, Color color, int count = 1)
    {
        var emitParams = new ParticleSystem.EmitParams();
        emitParams.ResetPosition();
        emitParams.ResetVelocity();
        emitParams.position = position;
        emitParams.startColor = color;
        particleSystems[type].Emit(emitParams, count);
    }

    public static void EmitOnPosition(Vector2 position, ParticleType type, Color color, int count)
    {
        Instance.Emit(position, type, color, count);
    }
}
