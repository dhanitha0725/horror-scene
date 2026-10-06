using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Stops the VR head (camera) from moving through walls.
/// The CharacterController only blocks thumbstick movement. Real headset walking
/// (and XR Device Simulator head movement) moves the camera directly, so this
/// script pushes the whole XR Origin back whenever the head enters or crosses a wall.
/// Attach to the XR Origin (XR Rig).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(XROrigin))]
public class HeadWallBlocker : MonoBehaviour
{
    [Header("Head Shape")]
    [Tooltip("Radius of the invisible sphere around the player's head (metres).")]
    [SerializeField, Range(0.05f, 0.5f)] private float headRadius = 0.15f;

    [Header("What Counts As A Wall")]
    [Tooltip("Layers the head cannot pass through.")]
    [SerializeField] private LayerMask wallLayers = 1; // Default layer
    [Tooltip("Ignore moving physics objects (held bat, flashlight, key) so they never push the player.")]
    [SerializeField] private bool ignoreDynamicRigidbodies = true;

    [Header("Behaviour")]
    [Tooltip("Only push the player sideways, never up or down.")]
    [SerializeField] private bool horizontalOnly = true;
    [Tooltip("Head jumps larger than this in one frame are treated as teleports and not blocked.")]
    [SerializeField, Min(0.1f)] private float teleportThreshold = 0.75f;
    [SerializeField, Range(1, 5)] private int penetrationIterations = 3;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool drawGizmo = true;

    private XROrigin xrOrigin;
    private Transform head;
    private SphereCollider probe;
    private readonly Collider[] overlaps = new Collider[32];
    private Vector3 lastHeadPos;
    private bool hasLastHeadPos;

    private void Awake()
    {
        xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin == null || xrOrigin.Camera == null)
        {
            Debug.LogError("[HeadWallBlocker] XR Origin or its Camera is missing. Disabling.", this);
            enabled = false;
            return;
        }
        head = xrOrigin.Camera.transform;

        // Probe collider used only for penetration maths. Trigger + Ignore Raycast so it never blocks anything.
        var probeGO = new GameObject("HeadWallBlocker_Probe");
        probeGO.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        probeGO.layer = 2; // Ignore Raycast
        probe = probeGO.AddComponent<SphereCollider>();
        probe.isTrigger = true;
        probe.radius = headRadius;
    }

    private void OnDestroy()
    {
        if (probe != null) Destroy(probe.gameObject);
    }

    private void OnEnable()
    {
        hasLastHeadPos = false;
    }

    private void LateUpdate()
    {
        if (head == null) return;
        probe.radius = headRadius;

        Vector3 headPos = head.position;
        if (!hasLastHeadPos)
        {
            lastHeadPos = headPos;
            hasLastHeadPos = true;
            return;
        }

        Vector3 correction = Vector3.zero;

        // 1. Sweep: stop the head crossing a thin wall in a single frame.
        Vector3 delta = headPos - lastHeadPos;
        float distance = delta.magnitude;
        if (distance > teleportThreshold)
        {
            Log($"Large jump ({distance:F2} m) treated as teleport.");
        }
        else if (distance > 0.0001f)
        {
            Vector3 dir = delta / distance;
            if (Physics.SphereCast(lastHeadPos, headRadius * 0.9f, dir, out RaycastHit hit, distance,
                                   wallLayers, QueryTriggerInteraction.Ignore) && IsWall(hit.collider))
            {
                Vector3 allowed = lastHeadPos + dir * Mathf.Max(0f, hit.distance - 0.01f);
                correction += allowed - headPos;
                Log($"Sweep blocked by {hit.collider.name}.");
            }
        }

        // 2. Penetration: push the head out of any wall it is touching.
        Vector3 probePos = headPos + correction;
        for (int iter = 0; iter < penetrationIterations; iter++)
        {
            int count = Physics.OverlapSphereNonAlloc(probePos, headRadius, overlaps, wallLayers, QueryTriggerInteraction.Ignore);
            Vector3 push = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Collider other = overlaps[i];
                if (!IsWall(other)) continue;
                if (Physics.ComputePenetration(probe, probePos, Quaternion.identity,
                                               other, other.transform.position, other.transform.rotation,
                                               out Vector3 pushDir, out float pushDist))
                {
                    push += pushDir * pushDist;
                }
            }
            if (push.sqrMagnitude < 1e-8f) break;
            probePos += push;
            correction += push;
            Log($"Pushed out of wall by {push.magnitude:F3} m.");
        }

        if (horizontalOnly) correction.y = 0f;

        if (correction.sqrMagnitude > 1e-8f)
        {
            xrOrigin.Origin.transform.position += correction;
            Physics.SyncTransforms();
        }

        lastHeadPos = head.position;
    }

    private bool IsWall(Collider c)
    {
        if (c == null || c == probe || c.isTrigger) return false;
        if (c.transform.IsChildOf(xrOrigin.transform)) return false; // the player's own colliders
        if (ignoreDynamicRigidbodies && c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) return false;
        return true;
    }

    private void Log(string msg)
    {
        if (debugLogs) Debug.Log("[HeadWallBlocker] " + msg, this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo) return;
        var origin = GetComponent<XROrigin>();
        if (origin == null || origin.Camera == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(origin.Camera.transform.position, headRadius);
    }
}
