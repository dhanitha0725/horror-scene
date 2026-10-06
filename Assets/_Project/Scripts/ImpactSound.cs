using UnityEngine;

/// <summary>
/// Plays a 3D impact sound when this physics object hits something hard enough
/// (e.g. the bat dropping on the floor or hitting a wall).
/// Volume and pitch scale with impact speed. Put it on the object with the Rigidbody.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class ImpactSound : MonoBehaviour
{
    [Header("Sounds")]
    [Tooltip("One is picked at random per impact.")]
    [SerializeField] private AudioClip[] impactClips;
    [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);

    [Header("Impact Strength")]
    [Tooltip("Hits slower than this (m/s) are silent. Stops tiny rolls/settling making noise.")]
    [SerializeField, Min(0f)] private float minImpactSpeed = 0.6f;
    [Tooltip("Hits at or above this speed (m/s) play at full volume.")]
    [SerializeField, Min(0.1f)] private float fullVolumeSpeed = 5f;
    [Tooltip("Minimum seconds between sounds so bounces don't machine-gun.")]
    [SerializeField, Min(0f)] private float cooldown = 0.12f;
    [Tooltip("Ignore hits during the first moments after the scene starts (object settling).")]
    [SerializeField, Min(0f)] private float startupGrace = 0.75f;

    [Header("Filtering")]
    [Tooltip("Only collisions with these layers make sound.")]
    [SerializeField] private LayerMask soundLayers = ~0;
    [Tooltip("Ignore collisions with the player rig (hands, body).")]
    [SerializeField] private bool ignorePlayer = true;
    [SerializeField] private string playerRootName = "XR Origin (XR Rig)";

    [Header("3D Audio")]
    [SerializeField, Min(0.1f)] private float minDistance = 0.5f;
    [SerializeField, Min(0.5f)] private float maxDistance = 20f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private AudioSource source;
    private float lastPlayTime = -999f;
    private float enabledTime;
    private Transform playerRoot;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = Mathf.Max(minDistance + 0.1f, maxDistance);
        source.dopplerLevel = 0f;

        if (pitchRange.y < pitchRange.x) pitchRange.y = pitchRange.x;
        if (fullVolumeSpeed <= minImpactSpeed) fullVolumeSpeed = minImpactSpeed + 0.5f;

        if (impactClips == null || impactClips.Length == 0)
            Debug.LogWarning($"[ImpactSound:{name}] No impact clips assigned.", this);

        if (ignorePlayer && !string.IsNullOrEmpty(playerRootName))
        {
            var p = GameObject.Find(playerRootName);
            if (p != null) playerRoot = p.transform;
        }
    }

    private void OnEnable()
    {
        enabledTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time - enabledTime < startupGrace) return;
        if (Time.time - lastPlayTime < cooldown) return;
        if (impactClips == null || impactClips.Length == 0) return;

        GameObject other = collision.gameObject;
        if ((soundLayers.value & (1 << other.layer)) == 0) return;
        if (playerRoot != null && other.transform.IsChildOf(playerRoot)) return;

        float speed = collision.relativeVelocity.magnitude;
        if (speed < minImpactSpeed) return;

        AudioClip clip = impactClips[Random.Range(0, impactClips.Length)];
        if (clip == null) return;

        float strength = Mathf.InverseLerp(minImpactSpeed, fullVolumeSpeed, speed);
        float volume = Mathf.Lerp(0.15f, 1f, strength) * maxVolume;

        // Play from the contact point for accurate 3D position
        if (collision.contactCount > 0)
            source.transform.position = transform.position; // source lives on this object
        source.pitch = Random.Range(pitchRange.x, pitchRange.y) * Mathf.Lerp(1.05f, 0.95f, strength);
        source.PlayOneShot(clip, volume);
        lastPlayTime = Time.time;

        if (debugLogs)
            Debug.Log($"[ImpactSound:{name}] Hit {other.name} at {speed:F2} m/s -> vol {volume:F2}", this);
    }

    /// <summary>Play the impact sound manually (e.g. from another script).</summary>
    public void PlayImpact(float strength01 = 1f)
    {
        if (impactClips == null || impactClips.Length == 0) return;
        AudioClip clip = impactClips[Random.Range(0, impactClips.Length)];
        if (clip == null) return;
        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(clip, Mathf.Clamp01(strength01) * maxVolume);
        lastPlayTime = Time.time;
    }
}
