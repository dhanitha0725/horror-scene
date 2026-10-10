using System.Collections;
using UnityEngine;

/// <summary>
/// Turns a static cupboard into a one-shot proximity trap. It creaks as the
/// player approaches, then tips toward that player and remains where it lands.
/// </summary>
[DisallowMultipleComponent]
public sealed class FallingCupboardTrap : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField, Min(0.5f)] private float triggerDistance = 4.5f;
    [SerializeField, Min(0.1f)] private float fallDuration = 0.9f;
    [SerializeField, Range(75f, 90f)] private float fallAngle = 88f;

    [Header("Optional audio overrides")]
    [SerializeField] private AudioClip warningCreak;
    [SerializeField] private AudioClip impactSound;
    private BoxCollider solidCollider;
    private AudioSource soundSource;
    private Transform player;
    private bool hasFallen;
    private bool impactPlayed;

    private void Awake()
    {
        solidCollider = GetComponent<BoxCollider>();
        if (solidCollider == null)
            solidCollider = gameObject.AddComponent<BoxCollider>();
        FitColliderToVisibleModel();        soundSource = GetComponent<AudioSource>();
        if (soundSource == null)
            soundSource = gameObject.AddComponent<AudioSource>();

        soundSource.playOnAwake = false;
        soundSource.loop = false;
        soundSource.spatialBlend = 1f;
        soundSource.rolloffMode = AudioRolloffMode.Linear;
        soundSource.minDistance = 1.5f;
        soundSource.maxDistance = 16f;
        soundSource.dopplerLevel = 0f;

        if (warningCreak == null)
            warningCreak = CreateWoodCreakClip();
        if (impactSound == null)
            impactSound = CreateWoodImpactClip();
    }

private void Update()
    {
        if (hasFallen)
            return;

        if (player == null && Camera.main != null)
            player = Camera.main.transform;
        if (player == null)
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= triggerDistance * triggerDistance)
            TriggerFall();
    }

private void TriggerFall()
    {
        if (hasFallen)
            return;

        hasFallen = true;
        if (warningCreak != null)
            soundSource.PlayOneShot(warningCreak, 0.85f);

        StartCoroutine(CollapseOntoRightSide());
    }

private IEnumerator CollapseOntoRightSide()
    {
        // Rotate around the lower-right edge so the cupboard comes down onto
        // its right side, without relying on unstable rigidbody physics.
        Vector3 right = transform.right.normalized;
        Vector3 axis = Vector3.Cross(right, Vector3.up).normalized;
        float halfHeight = solidCollider.size.y * Mathf.Abs(transform.lossyScale.y) * 0.5f;
        float halfWidth = solidCollider.size.x * Mathf.Abs(transform.lossyScale.x) * 0.5f;
        Vector3 pivot = solidCollider.bounds.center - Vector3.up * halfHeight + right * halfWidth;

        float elapsed = 0f;
        float appliedAngle = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float targetAngle = fallAngle * eased;
            transform.RotateAround(pivot, axis, targetAngle - appliedAngle);
            appliedAngle = targetAngle;
            yield return null;
        }

        if (appliedAngle < fallAngle)
            transform.RotateAround(pivot, axis, fallAngle - appliedAngle);

        PlayImpact();
    }






    private void PlayImpact()
    {
        impactPlayed = true;
        if (impactSound != null)
            soundSource.PlayOneShot(impactSound, 1f);
    }

    private void FitColliderToVisibleModel()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            solidCollider.size = new Vector3(1.2f, 2.1f, 0.55f);
            solidCollider.center = new Vector3(0f, 1.05f, 0f);
            return;
        }

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);

        Bounds localBounds = WorldBoundsToLocal(worldBounds);
        solidCollider.center = localBounds.center;
        solidCollider.size = localBounds.size + Vector3.one * 0.03f;
        solidCollider.isTrigger = false;
    }

    private Bounds WorldBoundsToLocal(Bounds worldBounds)
    {
        Vector3 c = worldBounds.center;
        Vector3 e = worldBounds.extents;
        Bounds localBounds = new Bounds(transform.InverseTransformPoint(c + new Vector3(-e.x, -e.y, -e.z)), Vector3.zero);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = c + Vector3.Scale(e, new Vector3(x, y, z));
                    localBounds.Encapsulate(transform.InverseTransformPoint(corner));
                }
            }
        }

        return localBounds;
    }

    private static AudioClip CreateWoodCreakClip()
    {
        const int sampleRate = 22050;
        const float duration = 1.1f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            float pitch = Mathf.Lerp(95f, 48f, t / duration);
            float tone = Mathf.Sin(t * pitch * Mathf.PI * 2f);
            float groan = Mathf.Sin(t * (pitch * 0.47f) * Mathf.PI * 2f);
            float grain = Mathf.PerlinNoise(t * 42f, 0.31f) * 2f - 1f;
            data[i] = Mathf.Clamp((tone * 0.20f + groan * 0.12f + grain * 0.10f) * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Cupboard_Wood_Creak", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateWoodImpactClip()
    {
        const int sampleRate = 22050;
        const float duration = 1.5f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = Mathf.Exp(-t * 5.5f);
            float thud = Mathf.Sin(t * 72f * Mathf.PI * 2f) * Mathf.Exp(-t * 7.5f);
            float rattle = (Mathf.PerlinNoise(t * 310f, 0.71f) * 2f - 1f) * decay;
            float knock = Mathf.Sin(t * 180f * Mathf.PI * 2f) * Mathf.Exp(-t * 13f);
            data[i] = Mathf.Clamp(thud * 0.55f + rattle * 0.34f + knock * 0.24f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Cupboard_Wood_Impact", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
