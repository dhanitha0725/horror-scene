using System.Collections;
using UnityEngine;

/// <summary>
/// Horror light flicker. Drives a Light's intensity with noise, random blackout bursts,
/// and optionally syncs a bulb's emission and a buzz AudioSource.
/// Other scripts (e.g. horror triggers) can call TriggerBurst(), ForceOff() or SetFlickerEnabled().
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    public enum FlickerStyle
    {
        Subtle,  // gentle unstable glow, never goes out
        Broken,  // unstable glow + frequent short blackout bursts
        Dying    // dim, slow dips, long blackouts
    }

    [Header("Style")]
    [SerializeField] private FlickerStyle style = FlickerStyle.Broken;
    [Tooltip("Each light flickers differently when enabled.")]
    [SerializeField] private bool randomizeSeed = true;
    [SerializeField] private int seed = 0;

    [Header("Intensity")]
    [SerializeField, Min(0f)] private float baseIntensity = 3f;
    [Tooltip("Lowest brightness of the normal wobble, as a fraction of base intensity.")]
    [SerializeField, Range(0f, 1f)] private float minIntensityFactor = 0.4f;
    [Tooltip("How fast the wobble changes.")]
    [SerializeField, Min(0.1f)] private float noiseSpeed = 8f;

    [Header("Blackout Bursts (Broken / Dying)")]
    [Tooltip("Chance per second of a flicker burst starting.")]
    [SerializeField, Range(0f, 2f)] private float burstChancePerSecond = 0.2f;
    [Tooltip("Min / max number of on-off flashes in one burst.")]
    [SerializeField] private Vector2Int flashesPerBurst = new Vector2Int(2, 6);
    [Tooltip("Min / max seconds the light stays OFF in each flash.")]
    [SerializeField] private Vector2 offDuration = new Vector2(0.04f, 0.25f);
    [Tooltip("Min / max seconds the light stays ON between flashes.")]
    [SerializeField] private Vector2 onDuration = new Vector2(0.03f, 0.15f);

    [Header("Optional Sync")]
    [Tooltip("Bulb mesh whose emission follows the light.")]
    [SerializeField] private Renderer bulbRenderer;
    [SerializeField, ColorUsage(false, true)] private Color emissionColor = new Color(3f, 2.1f, 1.2f);
    [Tooltip("Looping electrical buzz; volume follows brightness.")]
    [SerializeField] private AudioSource buzzSource;
    [SerializeField, Range(0f, 1f)] private float buzzMaxVolume = 0.35f;
    [Tooltip("Played (on the buzz source) each time the light snaps back on.")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.6f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    private Light lightComp;
    private MaterialPropertyBlock mpb;
    private float noiseOffset;
    private float forcedFactor = -1f;   // >= 0 means a burst/override controls brightness
    private bool flickerEnabled = true;
    private Coroutine burstRoutine;

    public FlickerStyle Style => style;
    public bool IsFlickerEnabled => flickerEnabled;
    public Color EmissionColor => emissionColor;

    /// <summary>Change the bulb glow colour (HDR). Brightness still follows the flicker.</summary>
    public void SetEmissionColor(Color color) { emissionColor = color; }

    private void Awake()
    {
        lightComp = GetComponent<Light>();
        mpb = new MaterialPropertyBlock();
        noiseOffset = randomizeSeed ? Random.Range(0f, 1000f) : seed * 13.37f;
        ValidateRanges();
        if (bulbRenderer != null && !bulbRenderer.sharedMaterial.IsKeywordEnabled("_EMISSION"))
            Log("Bulb material has no _EMISSION keyword; bulb glow may not change.");
    }

    private void OnValidate()
    {
        ValidateRanges();
    }

    private void OnDisable()
    {
        if (burstRoutine != null) StopCoroutine(burstRoutine);
        burstRoutine = null;
        forcedFactor = -1f;
        Apply(1f);
    }

    private void Update()
    {
        float factor;
        if (!flickerEnabled)
        {
            factor = forcedFactor >= 0f ? forcedFactor : 1f;
            Apply(factor);
            return;
        }

        if (forcedFactor >= 0f)
        {
            factor = forcedFactor;
        }
        else
        {
            factor = BaseWobble();
            if (style != FlickerStyle.Subtle && burstRoutine == null &&
                Random.value < burstChancePerSecond * Time.deltaTime)
            {
                burstRoutine = StartCoroutine(Burst(style == FlickerStyle.Dying ? 3f : 1f));
            }
        }

        Apply(factor);
    }

    private float BaseWobble()
    {
        float t = Time.time * noiseSpeed;
        float noise = Mathf.PerlinNoise(noiseOffset, t);
        float wobble = Mathf.Lerp(minIntensityFactor, 1f, noise);

        switch (style)
        {
            case FlickerStyle.Subtle:
                return Mathf.Lerp(0.85f, 1f, wobble);
            case FlickerStyle.Broken:
                // occasional single-frame stutter
                if (Random.value < 0.02f) return wobble * Random.Range(0.1f, 0.4f);
                return wobble;
            case FlickerStyle.Dying:
                float slowDip = 0.65f + 0.35f * Mathf.PerlinNoise(noiseOffset + 50f, Time.time * 0.6f);
                return wobble * slowDip * 0.7f;
            default:
                return wobble;
        }
    }

    private IEnumerator Burst(float durationScale)
    {
        int flashes = Random.Range(flashesPerBurst.x, flashesPerBurst.y + 1);
        Log($"Burst: {flashes} flashes");
        for (int i = 0; i < flashes; i++)
        {
            forcedFactor = 0f;
            yield return new WaitForSeconds(Random.Range(offDuration.x, offDuration.y) * durationScale);
            forcedFactor = Random.Range(0.6f, 1.25f);
            PlayClick();
            yield return new WaitForSeconds(Random.Range(onDuration.x, onDuration.y));
        }
        forcedFactor = -1f;
        burstRoutine = null;
    }

    private IEnumerator OffFor(float seconds)
    {
        forcedFactor = 0f;
        yield return new WaitForSeconds(seconds);
        forcedFactor = -1f;
        PlayClick();
        burstRoutine = null;
    }

    private void Apply(float factor)
    {
        factor = Mathf.Max(0f, factor);

        if (lightComp == null)
            lightComp = GetComponent<Light>();
        if (lightComp != null)
            lightComp.intensity = baseIntensity * factor;

        if (bulbRenderer != null)
        {
            mpb ??= new MaterialPropertyBlock();
            bulbRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionId, emissionColor * factor);
            bulbRenderer.SetPropertyBlock(mpb);
        }

        if (buzzSource != null)
            buzzSource.volume = buzzMaxVolume * Mathf.Clamp01(factor);
    }

    private void PlayClick()
    {
        if (clickClip != null && buzzSource != null)
            buzzSource.PlayOneShot(clickClip, clickVolume);
    }

    private void ValidateRanges()
    {
        flashesPerBurst.x = Mathf.Max(1, flashesPerBurst.x);
        flashesPerBurst.y = Mathf.Max(flashesPerBurst.x, flashesPerBurst.y);
        offDuration.x = Mathf.Max(0.01f, offDuration.x);
        offDuration.y = Mathf.Max(offDuration.x, offDuration.y);
        onDuration.x = Mathf.Max(0.01f, onDuration.x);
        onDuration.y = Mathf.Max(onDuration.x, onDuration.y);
    }

    private void Log(string msg)
    {
        if (debugLogs) Debug.Log($"[FlickeringLight:{name}] {msg}", this);
    }

    // ---------- Public API for horror events ----------

    /// <summary>Start a flicker burst right now (ignored if one is running).</summary>
    public void TriggerBurst()
    {
        if (!isActiveAndEnabled || burstRoutine != null) return;
        burstRoutine = StartCoroutine(Burst(style == FlickerStyle.Dying ? 3f : 1f));
    }

    /// <summary>Turn the light fully off for a number of seconds, then snap back on.</summary>
    public void ForceOff(float seconds)
    {
        if (!isActiveAndEnabled) return;
        if (burstRoutine != null) StopCoroutine(burstRoutine);
        burstRoutine = StartCoroutine(OffFor(Mathf.Max(0.01f, seconds)));
    }

    /// <summary>Enable or disable the automatic flicker (light stays steady when disabled).</summary>
    public void SetFlickerEnabled(bool enabledState)
    {
        flickerEnabled = enabledState;
        if (!enabledState && burstRoutine != null)
        {
            StopCoroutine(burstRoutine);
            burstRoutine = null;
            forcedFactor = -1f;
        }
    }

    /// <summary>Turn the light permanently off (true) or back on (false).</summary>
    public void SetPermanentlyOff(bool off)
    {
        if (burstRoutine != null) { StopCoroutine(burstRoutine); burstRoutine = null; }
        flickerEnabled = !off;
        forcedFactor = off ? 0f : -1f;
    }
}
