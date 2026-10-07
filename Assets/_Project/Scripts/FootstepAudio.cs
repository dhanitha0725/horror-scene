using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Plays footstep sounds as the player moves (thumbstick locomotion AND real/simulated head walking).
/// Measures horizontal head movement, plays one step every 'stepDistance' metres while on the ground,
/// ignores teleport jumps and tiny head sway. Attach to the XR Origin (XR Rig).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(XROrigin))]
public class FootstepAudio : MonoBehaviour
{
    [Header("Sounds")]
    [Tooltip("Single footstep clips. A different one is picked each step (never the same twice in a row).")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
    [Tooltip("Random volume multiplier per step.")]
    [SerializeField] private Vector2 volumeJitter = new Vector2(0.85f, 1f);
    [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

    [Header("Stride")]
    [Tooltip("Metres walked between steps. ~0.65 = normal walking pace.")]
    [SerializeField, Min(0.2f)] private float stepDistance = 0.65f;
    [Tooltip("Ignore movement slower than this (m/s) so standing still / head sway makes no steps.")]
    [SerializeField, Min(0f)] private float minSpeed = 0.25f;
    [Tooltip("Head jumps larger than this in one frame are teleports, not walking.")]
    [SerializeField, Min(0.1f)] private float teleportThreshold = 0.75f;
    [Tooltip("Play one soft step when landing from a teleport.")]
    [SerializeField] private bool stepOnTeleportLanding = true;

    [Header("Ground Check")]
    [Tooltip("Layers that count as floor.")]
    [SerializeField] private LayerMask groundLayers = 1; // Default
    [Tooltip("Extra ray length below the rig's floor height.")]
    [SerializeField, Min(0.1f)] private float extraRayLength = 0.6f;

    [Header("3D Audio")]
    [Tooltip("1 = fully 3D from the feet, 0 = flat 2D. 0.8 feels like it comes from below.")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.8f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private XROrigin origin;
    private Transform head;
    private AudioSource source;
    private Transform sourceTransform;
    private Vector3 lastFlatPos;
    private bool hasLastPos;
    private float accumulated;
    private int lastClipIndex = -1;

    private void Awake()
    {
        origin = GetComponent<XROrigin>();
        if (origin == null || origin.Camera == null)
        {
            Debug.LogError("[FootstepAudio] XR Origin or its Camera is missing. Disabling.", this);
            enabled = false;
            return;
        }
        head = origin.Camera.transform;

        if (footstepClips == null || footstepClips.Length == 0)
            Debug.LogWarning("[FootstepAudio] No footstep clips assigned. Footsteps will be silent.", this);

        if (pitchRange.y < pitchRange.x) pitchRange.y = pitchRange.x;
        if (volumeJitter.y < volumeJitter.x) volumeJitter.y = volumeJitter.x;

        // Dedicated child source placed at the player's feet
        var go = new GameObject("FootstepSource");
        go.transform.SetParent(transform, false);
        sourceTransform = go.transform;
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 0.5f;
        source.maxDistance = 10f;
        source.dopplerLevel = 0f;
    }

    private void OnEnable()
    {
        hasLastPos = false;
        accumulated = 0f;
    }

    private void Update()
    {
        if (head == null) return;

        Vector3 headPos = head.position;
        Vector3 flat = new Vector3(headPos.x, 0f, headPos.z);

        if (!hasLastPos)
        {
            lastFlatPos = flat;
            hasLastPos = true;
            return;
        }

        float dist = Vector3.Distance(flat, lastFlatPos);
        lastFlatPos = flat;

        // Teleport: reset stride, optional landing step
        if (dist > teleportThreshold)
        {
            accumulated = 0f;
            if (stepOnTeleportLanding && TryGetFloor(headPos, out float landY))
                PlayStep(headPos, landY, 0.8f);
            Log($"Teleport detected ({dist:F2} m).");
            return;
        }

        float speed = dist / Mathf.Max(Time.deltaTime, 0.0001f);
        if (speed < minSpeed) return;                        // standing still / small sway
        if (!TryGetFloor(headPos, out float floorY)) return; // in the air / no floor

        accumulated += dist;
        if (accumulated >= stepDistance)
        {
            accumulated = Mathf.Min(accumulated - stepDistance, stepDistance);
            float runFactor = Mathf.InverseLerp(0.5f, 3f, speed);
            PlayStep(headPos, floorY, Mathf.Lerp(0.85f, 1.15f, runFactor));
        }
    }

    private bool TryGetFloor(Vector3 headPos, out float floorY)
    {
        float rigY = origin.Origin.transform.position.y;
        float rayLength = Mathf.Max(0.1f, headPos.y - rigY) + extraRayLength;
        if (Physics.Raycast(headPos, Vector3.down, out RaycastHit hit, rayLength, groundLayers, QueryTriggerInteraction.Ignore)
            && !hit.collider.transform.IsChildOf(transform))
        {
            floorY = hit.point.y;
            return true;
        }
        floorY = rigY;
        return false;
    }

    private void PlayStep(Vector3 headPos, float floorY, float loudness)
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        int index = Random.Range(0, footstepClips.Length);
        if (footstepClips.Length > 1 && index == lastClipIndex)
            index = (index + 1) % footstepClips.Length;
        lastClipIndex = index;

        AudioClip clip = footstepClips[index];
        if (clip == null) return;

        sourceTransform.position = new Vector3(headPos.x, floorY + 0.05f, headPos.z);
        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(clip, volume * Random.Range(volumeJitter.x, volumeJitter.y) * loudness);
        Log($"Step: {clip.name}");
    }

    private void Log(string msg)
    {
        if (debugLogs) Debug.Log("[FootstepAudio] " + msg, this);
    }
}
