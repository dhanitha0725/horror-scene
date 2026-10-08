using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// A safe that opens with a KeyItem. The player walks up, faces it and presses F.
/// Without the key: shows a LOCKED message. With the key: the door swings open on its hinge,
/// the glow (PickupHighlight) is removed and onOpened fires (e.g. to end the game).
/// </summary>
[DisallowMultipleComponent]
public class LockedSafe : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private KeyItem requiredKey;
    [Tooltip("The door pivot (its origin sits on the hinge).")]
    [SerializeField] private Transform doorHinge;
    [Tooltip("Local rotation added to the door when fully open.")]
    [SerializeField] private Vector3 openRotation = new Vector3(0f, -110f, 0f);
    [SerializeField, Min(0.1f)] private float openSeconds = 1.6f;
    [Tooltip("Glow to remove when the safe opens.")]
    [SerializeField] private PickupHighlight highlight;
    [Tooltip("If true, the safe also needs SetGuardianDefeated() to be called (the second ghost) before it opens.")]
    [SerializeField] private bool requireGuardianDefeated = false;

    [Header("Interaction")]
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private Key interactKey = Key.F;
#endif
    [SerializeField] private KeyCode legacyInteractKey = KeyCode.F;
    [SerializeField, Min(0.3f)] private float interactRange = 2f;
    [SerializeField, Range(-1f, 1f)] private float facingThreshold = 0.3f;
    [Tooltip("Seconds before the LOCKED message can show again.")]
    [SerializeField, Min(0f)] private float lockedMessageCooldown = 2.5f;

    [Header("Feedback")]
    [SerializeField] private AudioClip lockedClip;
    [SerializeField] private AudioClip unlockClip;
    [SerializeField] private AudioClip doorClip;
    [SerializeField, Range(0f, 1f)] private float clipVolume = 0.9f;
    [SerializeField] private string lockedTitle = "LOCKED";
    [SerializeField, TextArea(1, 4)] private string lockedBody = "A heavy iron safe.\nIt won't budge without a <b>key</b>.";
    [SerializeField] private string guardianTitle = "NOT YET";
    [SerializeField, TextArea(1, 4)] private string guardianBody = "Something is still watching over this safe...\nYou can feel her breath on your neck.";
    [SerializeField] private string openedTitle = "GOLD";
    [SerializeField, TextArea(1, 4)] private string openedBody = "The lock gives way with a groan.\nWhatever she was guarding... it's yours now.";
    [SerializeField, Min(0.5f)] private float messageSeconds = 4f;
    [SerializeField] private Color lockedColor = new Color(0.85f, 0.05f, 0.05f);
    [SerializeField] private Color openedColor = new Color(1f, 0.8f, 0.2f);

    [Header("Events")]
    [SerializeField] private UnityEvent onOpened;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Quaternion closedRotation;
    private float lastLockedTime = -999f;
    private Bounds cachedBounds;

    public bool IsOpen { get; private set; }
    public bool GuardianDefeated { get; private set; }

    /// <summary>Call when the safe's guardian ghost is killed (hook to its On Ghost Defeated).</summary>
    public void SetGuardianDefeated()
    {
        GuardianDefeated = true;
        if (debugLogs) Debug.Log("[LockedSafe] Guardian defeated.", this);
    }

    private void Awake()
    {
        if (doorHinge != null) closedRotation = doorHinge.localRotation;
        else Debug.LogWarning("[LockedSafe] No door hinge assigned; the safe will 'open' without moving.", this);
        if (requiredKey == null) Debug.LogWarning("[LockedSafe] No required key assigned; the safe can never open.", this);
        cachedBounds = ComputeBounds();
    }

    private void Update()
    {
        if (IsOpen || !InteractPressed() || !PlayerCanReach()) return;

        bool hasKey = requiredKey != null && requiredKey.IsCollected;
        bool guardianClear = !requireGuardianDefeated || GuardianDefeated;
        if (hasKey && guardianClear) StartCoroutine(Open());
        else if (hasKey && !guardianClear)
        {
            if (Time.time - lastLockedTime >= lockedMessageCooldown)
            {
                lastLockedTime = Time.time;
                WorldMessage.ShowGlobal(guardianTitle, guardianBody, messageSeconds, lockedColor);
            }
        }
        else if (Time.time - lastLockedTime >= lockedMessageCooldown)
        {
            lastLockedTime = Time.time;
            if (lockedClip != null) AudioSource.PlayClipAtPoint(lockedClip, cachedBounds.center, clipVolume);
            WorldMessage.ShowGlobal(lockedTitle, lockedBody, messageSeconds, lockedColor);
            if (debugLogs) Debug.Log("[LockedSafe] Locked: no key.", this);
        }
    }

    private bool InteractPressed()
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
        Vector3 to = Vector3.ProjectOnPlane(cachedBounds.center - cam.transform.position, Vector3.up);
        float edgeDistance = Mathf.Max(0f, to.magnitude - Mathf.Max(cachedBounds.extents.x, cachedBounds.extents.z));
        if (edgeDistance > interactRange) return false;
        Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        return to.sqrMagnitude < 0.04f || Vector3.Dot(fwd, to.normalized) >= facingThreshold;
    }

    /// <summary>Open the safe (also callable from other scripts, e.g. for testing).</summary>
    public void ForceOpen()
    {
        if (!IsOpen) StartCoroutine(Open());
    }

    private IEnumerator Open()
    {
        IsOpen = true;
        if (highlight != null) highlight.RemoveHighlight();
        if (unlockClip != null) AudioSource.PlayClipAtPoint(unlockClip, cachedBounds.center, clipVolume);
        WorldMessage.ShowGlobal(openedTitle, openedBody, messageSeconds, openedColor);
        if (debugLogs) Debug.Log("[LockedSafe] Opening.", this);

        yield return new WaitForSeconds(0.35f);
        if (doorClip != null) AudioSource.PlayClipAtPoint(doorClip, cachedBounds.center, clipVolume);

        if (doorHinge != null)
        {
            Quaternion target = closedRotation * Quaternion.Euler(openRotation);
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / openSeconds);
                float e = 1f - Mathf.Pow(1f - t, 3f); // ease out: heavy door slows down
                doorHinge.localRotation = Quaternion.Slerp(closedRotation, target, e);
                yield return null;
            }
        }
        onOpened?.Invoke();
    }

    private Bounds ComputeBounds()
    {
        var rends = GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return new Bounds(transform.position, Vector3.one * 0.5f);
        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        return b;
    }
}
