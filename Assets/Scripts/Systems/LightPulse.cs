using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PointingOutLightPulse : MonoBehaviour
{
    [SerializeField] private Light2D targetLight;
    [SerializeField] private float baseIntensity = 1.5f;
    [SerializeField] private float pulseAmplitude = 0.7f;
    [SerializeField] private float pulseSpeed = 2f;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponentInChildren<Light2D>();
    }

    private void Update()
    {
        if (targetLight == null)
            return;

        float t = Time.time * pulseSpeed;
        float offset = Mathf.Sin(t) * 0.5f + 0.5f; // 0..1
        targetLight.intensity = baseIntensity + offset * pulseAmplitude;
    }
}
