using UnityEngine;

/// <summary>
/// Hanging cross evil-hint trap: when the player approaches the ghost area on the
/// left (+X) wall, the cross flips 180 degrees in the wall plane while a creaking
/// rotation sound plays. Rotation duration is synced to the audio.
/// </summary>
[DisallowMultipleComponent]
public class HangingCrossTrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform nailPivot;
    [SerializeField] private AudioSource crossAudio;
    [SerializeField] private AudioClip rotateClip;

    [Header("Trigger (close range and clear gaze)")]
    [SerializeField, Min(0.5f)] private float triggerDistance = 3f;
    [SerializeField, Range(1f, 90f)] private float gazeHalfAngle = 20f;
    [SerializeField, Min(0f)] private float gazeDuration = 0.3f;
    [SerializeField] private float resetHysteresis = 1.5f;
    [SerializeField] private bool resetWhenPlayerLeaves = false;
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Rotation / Audio sync")]
    [SerializeField] private float rotationAngle = 180f;
    [Tooltip("When true, rotation lasts exactly rotateClip.length for perfect sync.")]
    [SerializeField] private bool useClipLengthAsDuration = false;
    [Tooltip("Used when useClipLengthAsDuration is false. Clip plays concurrently and is stopped/faded at the end.")]
    [SerializeField, Min(0.5f)] private float rotationDuration = 6f;
    [SerializeField] private bool stopClipWhenRotationEnds = true;
    [SerializeField, Min(0f)] private float audioFadeOutTime = 0.4f;
    [SerializeField, Range(0f, 1f)] private float volume = 0.9f;
    [Tooltip("Small decaying wobble so it feels hung from a nail, not robotic.")]
    [SerializeField, Range(0f, 10f)] private float wobbleDegrees = 3f;

    [Header("Environment blend")]
    [Tooltip("If set, flicker/raise this light slightly during rotation for dread feel.")]
    [SerializeField] private Light nearbyLight;
    [SerializeField] private float lightIntensityBoost = 0.3f;

    private Transform player;
    private Quaternion pivotStartRotation;
    private bool hasStartedRotation;
    private bool isRotating;
    private bool hasFired;
    private Coroutine activeRoutine;
    private float baseLightIntensity = -1f;
    private bool audioConfigured;
    private float gazeTime;
    private Renderer crossRenderer;

    private void Awake()
    {
        if (crossAudio == null)
            crossAudio = GetComponentInChildren<AudioSource>();

        ConfigureAudio();

        if (nailPivot != null)
        {
            pivotStartRotation = nailPivot.localRotation;
            crossRenderer = nailPivot.GetComponentInChildren<Renderer>();
        }
    }

    private void ConfigureAudio()
    {
        if (crossAudio == null)
            return;

        crossAudio.playOnAwake = false;
        crossAudio.loop = false;
        crossAudio.spatialBlend = 1f; // 3D: creak comes from the wall
        crossAudio.rolloffMode = AudioRolloffMode.Linear;
        crossAudio.minDistance = 1.5f;
        crossAudio.maxDistance = 14f;
        crossAudio.dopplerLevel = 0f;
        crossAudio.volume = volume;
        audioConfigured = true;
    }

    private void Start()
    {
        if (nearbyLight != null)
            baseLightIntensity = nearbyLight.intensity;

        if (Camera.main != null)
            player = Camera.main.transform;
    }

    private void Update()
    {
        if (isRotating || nailPivot == null)
            return;

        if (player == null)
        {
            if (Camera.main != null)
                player = Camera.main.transform;
            if (player == null)
                return;
        }

        Vector3 target = crossRenderer != null ? crossRenderer.bounds.center : nailPivot.position;
        Vector3 delta = player.position - target;
        float distance = delta.magnitude;

        if (!hasStartedRotation && !hasFired)
        {
            bool watchingNearbyCross = distance <= triggerDistance && IsWatching(target);
            if (watchingNearbyCross)
                gazeTime += Time.deltaTime;
            else
                gazeTime = 0f;

            if (watchingNearbyCross && gazeTime >= gazeDuration)
                Trigger();
        }
        else if (resetWhenPlayerLeaves && hasStartedRotation && !triggerOnlyOnce)
        {
            if (distance > triggerDistance + resetHysteresis)
                ResetTrap();
        }
    }

    private bool IsWatching(Vector3 target)
    {
        Vector3 direction = target - player.position;
        float length = direction.magnitude;
        if (length < 0.001f) return false;
        direction /= length;
        if (Vector3.Dot(player.forward, direction) < Mathf.Cos(gazeHalfAngle * Mathf.Deg2Rad))
            return false;

        // Ignore the player's rig and the cross itself, but reject intervening walls/objects.
        foreach (RaycastHit hit in Physics.RaycastAll(player.position, direction, length, ~0, QueryTriggerInteraction.Ignore))
        {
            Transform obstacle = hit.collider.transform;
            if (obstacle.IsChildOf(player.root) || obstacle.IsChildOf(transform) || obstacle.IsChildOf(nailPivot))
                continue;
            return false;
        }
        return true;
    }

    public void Trigger()
    {
        if (isRotating || nailPivot == null || (triggerOnlyOnce && hasFired))
            return;

        hasStartedRotation = true;
        gazeTime = 0f;
        hasFired = true;
        StopActiveRoutine();
        activeRoutine = StartCoroutine(RotateRoutine(0f, rotationAngle));
    }

    public void ResetTrap()
    {
        if (isRotating)
            return;

        hasStartedRotation = false;
        StopActiveRoutine();
        activeRoutine = StartCoroutine(RotateRoutine(rotationAngle, 0f));
    }

    private void StopActiveRoutine()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
    }

    private System.Collections.IEnumerator RotateRoutine(float fromAngle, float toAngle)
    {
        isRotating = true;

        float duration = rotationDuration;
        if (useClipLengthAsDuration && rotateClip != null)
            duration = Mathf.Max(0.5f, rotateClip.length);
        else if (rotateClip != null && rotateClip.length < duration)
            duration = Mathf.Max(0.5f, rotateClip.length > 0f ? rotateClip.length : duration);

        // Sync: start sound and motion on the same frame.
        if (crossAudio != null && rotateClip != null)
        {
            if (!audioConfigured)
                ConfigureAudio();
            crossAudio.volume = volume;
            crossAudio.clip = rotateClip;
            crossAudio.Play();
        }

        if (nearbyLight != null)
        {
            if (baseLightIntensity < 0f)
                baseLightIntensity = nearbyLight.intensity;
            nearbyLight.intensity = baseLightIntensity + lightIntensityBoost;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseInOutCubic(t);
            float angle = Mathf.Lerp(fromAngle, toAngle, eased);

            // Decaying wobble peaks mid-rotation, settles at the end.
            // Main flip is around local X (wall normal = world +X/-X), so the
            // cross inverts in the wall plane; a smaller Z component adds a
            // natural hanging swing.
            float wobble = Mathf.Sin(t * Mathf.PI * 4f) * wobbleDegrees * (1f - t);
            nailPivot.localRotation = pivotStartRotation * Quaternion.Euler(angle + wobble, 0f, wobble * 0.5f);

            yield return null;
        }

        nailPivot.localRotation = pivotStartRotation * Quaternion.Euler(toAngle, 0f, 0f);

        if (crossAudio != null && stopClipWhenRotationEnds && crossAudio.isPlaying)
        {
            if (audioFadeOutTime > 0f)
                yield return FadeOutAudio();
            else
                crossAudio.Stop();
        }

        if (nearbyLight != null && baseLightIntensity >= 0f)
            nearbyLight.intensity = baseLightIntensity;

        isRotating = false;
        activeRoutine = null;
    }

    private System.Collections.IEnumerator FadeOutAudio()
    {
        float startVolume = crossAudio.volume;
        float elapsed = 0f;
        while (elapsed < audioFadeOutTime && crossAudio.isPlaying)
        {
            elapsed += Time.deltaTime;
            crossAudio.volume = Mathf.Lerp(startVolume, 0f, elapsed / audioFadeOutTime);
            yield return null;
        }
        crossAudio.Stop();
        crossAudio.volume = startVolume;
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    private void OnValidate()
    {
        if (rotateClip != null && useClipLengthAsDuration)
            rotationDuration = Mathf.Max(0.5f, rotateClip.length);
    }
}
