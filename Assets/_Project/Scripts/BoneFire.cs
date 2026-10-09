using System.Collections;
using HorrorGame.AI;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// The final objective: a fire where the player burns the ghost's bones.
/// Walk up with the bones (a collected KeyItem), face the fire and press F.
/// The bones drop into the fire, the flames flare up, the ghost's final scream plays,
/// every ghost still in the scene is removed, the lights calm down and the ending message shows.
/// </summary>
[DisallowMultipleComponent]
public class BoneFire : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The bones item the player must be carrying (collected).")]
    [SerializeField] private KeyItem requiredBones;
    [Tooltip("Bones model shown burning in the fire after the player drops them in (starts hidden).")]
    [SerializeField] private GameObject bonesInFire;
    [Tooltip("Flame particle systems that flare up when the bones burn.")]
    [SerializeField] private ParticleSystem[] flames;
    [Tooltip("Fire light that flares brighter when the bones burn.")]
    [SerializeField] private Light fireLight;
    [SerializeField] private PickupHighlight highlight;

    [Header("Interaction")]
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private Key interactKey = Key.F;
#endif
    [SerializeField] private KeyCode legacyInteractKey = KeyCode.F;
    [SerializeField, Min(0.3f)] private float interactRange = 2f;
    [SerializeField, Range(-1f, 1f)] private float facingThreshold = 0.3f;
    [SerializeField, Min(0f)] private float hintCooldown = 3f;

    [Header("Burn Effect")]
    [Tooltip("How much bigger/faster the flames get while the bones burn.")]
    [SerializeField, Min(1f)] private float flareMultiplier = 2.5f;
    [SerializeField, Min(0.1f)] private float flareSeconds = 4f;
    [SerializeField, Min(1f)] private float lightFlareMultiplier = 3f;
    [Tooltip("Final scream when she is destroyed (e.g. the death scream).")]
    [SerializeField] private AudioClip finalScream;
    [SerializeField] private AudioClip whooshClip;
    [SerializeField, Range(0f, 1f)] private float clipVolume = 1f;
    [Tooltip("Remove every ghost still in the scene when the bones burn.")]
    [SerializeField] private bool destroyAllGhosts = true;
    [Tooltip("Lights to calm (HorrorLightMood.GoNormal) at the end.")]
    [SerializeField] private HorrorLightMood lightMood;

    [Header("Messages")]
    [SerializeField] private string noBonesTitle = "THE FIRE";
    [SerializeField, TextArea(1, 4)] private string noBonesBody = "It burns too hot for an empty room.\nSomething here is meant to be <b>burned</b>.";
    [SerializeField] private string burnTitle = "SHE IS FREE";
    [SerializeField, TextArea(1, 4)] private string burnBody = "The bones crack and turn to ash.\nHer scream fades into the walls... and then, silence.";
    [SerializeField, Min(0.5f)] private float endMessageSeconds = 8f;
    [SerializeField] private Color hintColor = new Color(1f, 0.55f, 0.15f);
    [SerializeField] private Color endColor = new Color(1f, 0.85f, 0.5f);

    [Header("Events")]
    [Tooltip("Called when the bones have burned: the game is won (load an ending scene here).")]
    [SerializeField] private UnityEvent onBonesBurned;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private float lastHintTime = -999f;
    private Bounds bounds;

    public bool IsBurned { get; private set; }

    private void Awake()
    {
        if (bonesInFire != null) bonesInFire.SetActive(false);
        if (requiredBones == null) Debug.LogWarning("[BoneFire] No bones item assigned.", this);
        bounds = ComputeBounds();
    }

    private void Update()
    {
        if (IsBurned || !Pressed() || !PlayerCanReach()) return;

        if (requiredBones != null && requiredBones.IsCollected) StartCoroutine(Burn());
        else if (Time.time - lastHintTime >= hintCooldown)
        {
            lastHintTime = Time.time;
            WorldMessage.ShowGlobal(noBonesTitle, noBonesBody, 4f, hintColor);
        }
    }

    private bool Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(legacyInteractKey)) return true;
#endif
        return false;
    }

    private bool PlayerCanReach()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector3 to = Vector3.ProjectOnPlane(bounds.center - cam.transform.position, Vector3.up);
        float edge = Mathf.Max(0f, to.magnitude - Mathf.Max(bounds.extents.x, bounds.extents.z));
        if (edge > interactRange) return false;
        Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        return to.sqrMagnitude < 0.04f || Vector3.Dot(fwd, to.normalized) >= facingThreshold;
    }

    /// <summary>Burn the bones now (also usable for testing).</summary>
    public void ForceBurn()
    {
        if (!IsBurned) StartCoroutine(Burn());
    }

    private IEnumerator Burn()
    {
        IsBurned = true;
        if (highlight != null) highlight.RemoveHighlight();
        if (bonesInFire != null) bonesInFire.SetActive(true);
        if (whooshClip != null) AudioSource.PlayClipAtPoint(whooshClip, bounds.center, clipVolume);
        if (debugLogs) Debug.Log("[BoneFire] Burning the bones.", this);

        // Flare the flames and the light
        var baseRates = new float[flames != null ? flames.Length : 0];
        var baseSizes = new float[baseRates.Length];
        for (int i = 0; i < baseRates.Length; i++)
        {
            if (flames[i] == null) continue;
            var em = flames[i].emission; baseRates[i] = em.rateOverTimeMultiplier;
            var main = flames[i].main; baseSizes[i] = main.startSizeMultiplier;
        }
        float baseLight = fireLight != null ? fireLight.intensity : 0f;

        yield return new WaitForSeconds(0.4f);
        if (finalScream != null) AudioSource.PlayClipAtPoint(finalScream, bounds.center + Vector3.up, clipVolume);

        float t = 0f;
        while (t < flareSeconds)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / flareSeconds) * Mathf.PI); // up then back down
            for (int i = 0; i < baseRates.Length; i++)
            {
                if (flames[i] == null) continue;
                var em = flames[i].emission; em.rateOverTimeMultiplier = baseRates[i] * Mathf.Lerp(1f, flareMultiplier, k);
                var main = flames[i].main; main.startSizeMultiplier = baseSizes[i] * Mathf.Lerp(1f, flareMultiplier * 0.6f, k);
            }
            if (fireLight != null) fireLight.intensity = baseLight * Mathf.Lerp(1f, lightFlareMultiplier, k);
            if (bonesInFire != null && t > flareSeconds * 0.5f)
                bonesInFire.transform.localScale = Vector3.Lerp(bonesInFire.transform.localScale, Vector3.one * 0.001f, Time.deltaTime * 0.8f); // crumble
            yield return null;
        }
        if (bonesInFire != null) bonesInFire.SetActive(false);

        // Destroy every ghost that is still around
        if (destroyAllGhosts)
        {
            foreach (var g in FindObjectsByType<GhostController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (g != null) Destroy(g.gameObject);
        }
        if (lightMood != null) lightMood.GoNormal();

        WorldMessage.ShowGlobal(burnTitle, burnBody, endMessageSeconds, endColor);
        onBonesBurned?.Invoke();
    }

    private Bounds ComputeBounds()
    {
        var rends = GetComponentsInChildren<Renderer>(true);
        Bounds b = new Bounds(transform.position, Vector3.one * 0.6f);
        bool first = true;
        foreach (var r in rends)
        {
            if (r is ParticleSystemRenderer) continue;
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
        }
        return b;
    }
}
