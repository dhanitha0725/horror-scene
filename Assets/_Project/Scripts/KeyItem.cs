using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// A key that stays hidden until Reveal() is called (hook to the ghost's On Ghost Defeated).
/// It then appears where the ghost fell, hovering and slowly spinning (with a glow from PickupHighlight on the model).
/// The player collects it by walking close, facing it and pressing the pickup key (F).
/// LockedSafe checks IsCollected to decide whether it can open.
/// </summary>
[DisallowMultipleComponent]
public class KeyItem : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The key model (child). Hidden until revealed, hidden again when collected.")]
    [SerializeField] private GameObject keyVisual;
    [SerializeField] private bool hiddenUntilRevealed = true;
    [Tooltip("Where the key appears (the ghost). If empty, it appears where this object is.")]
    [SerializeField] private Transform revealAt;
    [Tooltip("Seconds after Reveal() before the key appears (lets the death animation play).")]
    [SerializeField, Min(0f)] private float revealDelay = 1.5f;
    [Tooltip("Height of the key above the floor (metres).")]
    [SerializeField, Min(0f)] private float hoverHeight = 0.35f;

    [Header("Idle Animation")]
    [SerializeField] private bool spin = true;
    [SerializeField] private float spinDegreesPerSecond = 60f;
    [SerializeField, Min(0f)] private float bobAmplitude = 0.04f;
    [SerializeField, Min(0f)] private float bobSpeed = 0.6f;

    [Header("Pickup")]
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private Key pickupKey = Key.F;
#endif
    [SerializeField] private KeyCode legacyPickupKey = KeyCode.F;
    [Tooltip("How close the player's head must be (metres, horizontal).")]
    [SerializeField, Min(0.3f)] private float pickupRange = 1.8f;
    [Tooltip("How directly the player must face the key (1 = exactly, 0 = 90 degrees, -1 = any direction).")]
    [SerializeField, Range(-1f, 1f)] private float facingThreshold = 0.3f;

    [Header("Feedback")]
    [SerializeField] private AudioClip revealClip;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField, Range(0f, 1f)] private float clipVolume = 0.9f;
    [SerializeField] private string revealTitle = "SHE DROPPED SOMETHING";
    [SerializeField, TextArea(1, 4)] private string revealBody = "A key glints where she fell.\nPress <b>F</b> to pick it up.";
    [SerializeField] private string pickupTitle = "THE KEY";
    [SerializeField, TextArea(1, 4)] private string pickupBody = "Cold iron, still warm from her hand.\nSomewhere in this house, a <b>safe</b> is waiting.";
    [SerializeField, Min(0.5f)] private float messageSeconds = 4f;
    [SerializeField] private Color messageColor = new Color(1f, 0.75f, 0.25f);

    [Header("Events")]
    [SerializeField] private UnityEvent onRevealed;
    [SerializeField] private UnityEvent onCollected;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Vector3 visualBaseLocalPos;
    private bool revealPending;

    public bool IsRevealed { get; private set; }
    public bool IsCollected { get; private set; }

    private void Awake()
    {
        if (keyVisual == null && transform.childCount > 0) keyVisual = transform.GetChild(0).gameObject;
        if (keyVisual != null)
        {
            visualBaseLocalPos = keyVisual.transform.localPosition;
            if (hiddenUntilRevealed) keyVisual.SetActive(false);
            else IsRevealed = true;
        }
        else Debug.LogWarning("[KeyItem] No key visual assigned.", this);
    }

    /// <summary>Make the key appear (at the ghost, after the delay). Hook to On Ghost Defeated.</summary>
    public void Reveal()
    {
        if (IsRevealed || IsCollected || revealPending) return;
        Vector3 at = revealAt != null ? revealAt.position : transform.position; // capture now: the ghost is destroyed later
        if (revealDelay > 0f && isActiveAndEnabled) { revealPending = true; StartCoroutine(RevealAfter(at)); }
        else DoReveal(at);
    }

    private IEnumerator RevealAfter(Vector3 at)
    {
        yield return new WaitForSeconds(revealDelay);
        DoReveal(at);
    }

    private void DoReveal(Vector3 at)
    {
        revealPending = false;
        float floorY = at.y;
        var hits = Physics.RaycastAll(at + Vector3.up * 1.5f, Vector3.down, 5f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        foreach (var h in hits)
        {
            if (revealAt != null && h.collider.transform.IsChildOf(revealAt)) continue;
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.collider.attachedRigidbody != null && !h.collider.attachedRigidbody.isKinematic) continue;
            if (h.distance < best) { best = h.distance; floorY = h.point.y; }
        }
        transform.position = new Vector3(at.x, floorY + hoverHeight, at.z);
        if (keyVisual != null) keyVisual.SetActive(true);
        IsRevealed = true;

        if (revealClip != null) AudioSource.PlayClipAtPoint(revealClip, transform.position, clipVolume);
        if (!string.IsNullOrEmpty(revealTitle)) WorldMessage.ShowGlobal(revealTitle, revealBody, messageSeconds, messageColor);
        onRevealed?.Invoke();
        if (debugLogs) Debug.Log("[KeyItem] Revealed at " + transform.position, this);
    }

    private void Update()
    {
        if (!IsRevealed || IsCollected || keyVisual == null) return;

        if (spin) keyVisual.transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
        if (bobAmplitude > 0f)
            keyVisual.transform.localPosition = visualBaseLocalPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed * Mathf.PI * 2f) * bobAmplitude);

        if (PickupPressed() && PlayerCanReach()) Collect();
    }

    private bool PickupPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[pickupKey].wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(legacyPickupKey)) return true;
#endif
        return false;
    }

    private bool PlayerCanReach()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector3 to = Vector3.ProjectOnPlane(keyVisual.transform.position - cam.transform.position, Vector3.up);
        if (to.magnitude > pickupRange) { if (debugLogs) Debug.Log("[KeyItem] Too far: " + to.magnitude.ToString("F2") + " m", this); return false; }
        Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        bool facing = to.sqrMagnitude < 0.04f || Vector3.Dot(fwd, to.normalized) >= facingThreshold;
        if (!facing && debugLogs) Debug.Log("[KeyItem] Not facing the key.", this);
        return facing;
    }

    /// <summary>Collect the key (also callable from other scripts).</summary>
    public void Collect()
    {
        if (IsCollected || !IsRevealed) return;
        IsCollected = true;
        Vector3 at = keyVisual != null ? keyVisual.transform.position : transform.position;
        if (keyVisual != null) keyVisual.SetActive(false);
        if (pickupClip != null) AudioSource.PlayClipAtPoint(pickupClip, at, clipVolume);
        if (!string.IsNullOrEmpty(pickupTitle)) WorldMessage.ShowGlobal(pickupTitle, pickupBody, messageSeconds, messageColor);
        onCollected?.Invoke();
        if (debugLogs) Debug.Log("[KeyItem] Collected.", this);
    }
}
