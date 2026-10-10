using UnityEngine;

/// <summary>
/// Last-resort protection against falling out of the level.
/// Remembers the last position where the player was standing on solid ground.
/// If the XR Origin ever drops below 'killHeight', it is put back at that position.
/// Put it on the XR Origin (XR Rig).
/// </summary>
[DisallowMultipleComponent]
public class FallSafetyNet : MonoBehaviour
{
    [Tooltip("If the rig goes below this world height (metres), the player is rescued.")]
    [SerializeField] private float killHeight = -2f;
    [Tooltip("How often (seconds) the last safe standing position is remembered.")]
    [SerializeField, Min(0.05f)] private float sampleInterval = 0.25f;
    [Tooltip("Ground must be within this distance below the rig to count as a safe spot.")]
    [SerializeField, Min(0.05f)] private float groundCheckDistance = 0.3f;
    [Tooltip("Layers that count as ground.")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private bool debugLogs = false;

    private CharacterController characterController;
    private Vector3 lastSafePosition;
    private float nextSampleTime;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        lastSafePosition = transform.position; // the spawn point is always safe
    }

    private void Update()
    {
        if (transform.position.y < killHeight)
        {
            Rescue();
            return;
        }

        if (Time.time >= nextSampleTime)
        {
            nextSampleTime = Time.time + sampleInterval;
            if (IsStandingOnGround()) lastSafePosition = transform.position;
        }
    }

    private bool IsStandingOnGround()
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        return Physics.Raycast(origin, Vector3.down, 0.2f + groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void Rescue()
    {
        // The CharacterController must be off while moving the rig directly, or it snaps back.
        bool ccWasEnabled = characterController != null && characterController.enabled;
        if (ccWasEnabled) characterController.enabled = false;
        transform.position = lastSafePosition + Vector3.up * 0.05f;
        if (ccWasEnabled) characterController.enabled = true;
        if (debugLogs) Debug.Log("[FallSafetyNet] Player fell out of the level; moved back to " + lastSafePosition, this);
    }
}
