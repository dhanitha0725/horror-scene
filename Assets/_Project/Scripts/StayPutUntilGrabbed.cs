using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Keeps a grabbable physics object exactly where it was placed (e.g. a bat leaning on a wall)
/// until the player picks it up for the first time. After the first release it behaves as a
/// normal physics object (falls, bounces, can be thrown).
/// Put it on the same object as the Rigidbody and XRGrabInteractable.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class StayPutUntilGrabbed : MonoBehaviour
{
    [Tooltip("Hold the object still at its placed position until the first grab.")]
    [SerializeField] private bool freezeAtStart = true;
    [SerializeField] private bool debugLogs = false;

    private Rigidbody rb;
    private XRGrabInteractable grab;
    private bool released;

    public bool IsFrozen => rb != null && rb.isKinematic && !released;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        if (freezeAtStart)
        {
            rb.isKinematic = true;
            if (debugLogs) Debug.Log("[StayPutUntilGrabbed] Frozen in place until first grab.", this);
        }
    }

    private void OnEnable()
    {
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grab.selectExited.RemoveListener(OnReleased);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (released || !freezeAtStart) return;
        released = true;
        StartCoroutine(BecomeDynamic());
    }

    // XRGrabInteractable restores the 'was kinematic' state on release, so switch to physics a moment later.
    private IEnumerator BecomeDynamic()
    {
        yield return null;
        yield return new WaitForFixedUpdate();
        if (grab.isSelected) { released = false; yield break; } // grabbed again immediately; try on next release
        rb.isKinematic = false;
        rb.WakeUp();
        if (debugLogs) Debug.Log("[StayPutUntilGrabbed] Released: now a normal physics object.", this);
        grab.selectExited.RemoveListener(OnReleased);
    }
}
