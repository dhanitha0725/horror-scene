using System.Collections;
using UnityEngine;

/// <summary>
/// Switches the horror lights between their normal warm colour and a danger red.
/// Put it on the HorrorLights parent. Hook GoRed() to the ghost's On Ghost Revealed event
/// and GoNormal() to On Ghost Defeated. Works together with FlickeringLight (flicker keeps running).
/// </summary>
[DisallowMultipleComponent]
public class HorrorLightMood : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Lights to recolour. Leave empty to use every Light under this object.")]
    [SerializeField] private Light[] lights;

    [Header("Red Mood")]
    [SerializeField] private Color redLightColor = new Color(1f, 0.08f, 0.05f);
    [SerializeField, ColorUsage(false, true)] private Color redBulbEmission = new Color(4f, 0.25f, 0.15f);
    [Tooltip("Multiply light intensity while red (1 = same brightness).")]
    [SerializeField, Range(0.3f, 3f)] private float redIntensityMultiplier = 1.2f;
    [SerializeField] private bool tintFog = true;
    [SerializeField] private Color redFogColor = new Color(0.09f, 0f, 0f);

    [Header("Transition")]
    [Tooltip("Seconds to fade between colours. 0 = instant.")]
    [SerializeField, Min(0f)] private float transitionTime = 0.5f;
    [Tooltip("All lights do a flicker burst at the moment the colour changes.")]
    [SerializeField] private bool flickerBurstOnChange = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Color[] normalLightColors;
    private Color[] normalEmission;
    private FlickeringLight[] flickers;
    private Color normalFog;
    private Coroutine routine;
    private bool isRed;
    private float intensityMul = 1f;

    public bool IsRed => isRed;

    private void Awake()
    {
        if (lights == null || lights.Length == 0) lights = GetComponentsInChildren<Light>(true);
        int n = lights.Length;
        normalLightColors = new Color[n];
        normalEmission = new Color[n];
        flickers = new FlickeringLight[n];
        for (int i = 0; i < n; i++)
        {
            if (lights[i] == null) continue;
            normalLightColors[i] = lights[i].color;
            flickers[i] = lights[i].GetComponent<FlickeringLight>();
            normalEmission[i] = flickers[i] != null ? flickers[i].EmissionColor : Color.black;
        }
        normalFog = RenderSettings.fogColor;
        if (n == 0) Debug.LogWarning("[HorrorLightMood] No lights found under " + name, this);
    }

    private void LateUpdate()
    {
        // FlickeringLight sets intensity every frame; apply the red brightness boost on top.
        if (Mathf.Approximately(intensityMul, 1f)) return;
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) lights[i].intensity *= intensityMul;
    }

    /// <summary>Fade all lights to red. Hook to the ghost's On Ghost Revealed.</summary>
    public void GoRed() { Transition(true); }

    /// <summary>Fade all lights back to their normal colour. Hook to On Ghost Defeated.</summary>
    public void GoNormal() { Transition(false); }

    public void Toggle() { Transition(!isRed); }

    private void Transition(bool toRed)
    {
        if (!isActiveAndEnabled) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Fade(toRed));
    }

    private IEnumerator Fade(bool toRed)
    {
        isRed = toRed;
        if (debugLogs) Debug.Log("[HorrorLightMood] " + (toRed ? "to RED" : "to normal"), this);

        if (flickerBurstOnChange)
            foreach (var f in flickers) if (f != null) f.TriggerBurst();

        int n = lights.Length;
        var fromColor = new Color[n];
        var fromEmission = new Color[n];
        for (int i = 0; i < n; i++)
        {
            if (lights[i] == null) continue;
            fromColor[i] = lights[i].color;
            fromEmission[i] = flickers[i] != null ? flickers[i].EmissionColor : Color.black;
        }
        Color fromFog = RenderSettings.fogColor;
        float fromMul = intensityMul;
        float toMul = toRed ? redIntensityMultiplier : 1f;

        float t = transitionTime <= 0f ? 1f : 0f;
        while (true)
        {
            float e = t * t * (3f - 2f * t); // smoothstep
            for (int i = 0; i < n; i++)
            {
                if (lights[i] == null) continue;
                lights[i].color = Color.Lerp(fromColor[i], toRed ? redLightColor : normalLightColors[i], e);
                if (flickers[i] != null)
                    flickers[i].SetEmissionColor(Color.Lerp(fromEmission[i], toRed ? redBulbEmission : normalEmission[i], e));
            }
            if (tintFog) RenderSettings.fogColor = Color.Lerp(fromFog, toRed ? redFogColor : normalFog, e);
            intensityMul = Mathf.Lerp(fromMul, toMul, e);

            if (t >= 1f) break;
            t = Mathf.Min(1f, t + Time.deltaTime / transitionTime);
            yield return null;
        }
        routine = null;
    }

    private void OnDisable()
    {
        if (tintFog && normalLightColors != null) RenderSettings.fogColor = normalFog;
    }
}
