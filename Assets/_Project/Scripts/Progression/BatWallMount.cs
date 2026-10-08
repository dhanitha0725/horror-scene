using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HorrorGame.Progression
{
    /// <summary>
    /// Keeps the bat at its authored wall position until the player grabs it.
    /// It deliberately does not alter the transform, so level design remains in
    /// the scene rather than hidden in code.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BatWallMount : MonoBehaviour
    {
        [SerializeField] private bool lockUntilFirstGrab = true;

        private Rigidbody body;
        private XRGrabInteractable grabInteractable;
        private bool released;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            grabInteractable = GetComponent<XRGrabInteractable>();

            if (lockUntilFirstGrab)
                LockInPlace();
        }

        private void OnEnable()
        {
            if (grabInteractable != null)
                grabInteractable.selectEntered.AddListener(ReleaseFromWall);
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
                grabInteractable.selectEntered.RemoveListener(ReleaseFromWall);
        }

        private void ReleaseFromWall(SelectEnterEventArgs args)
        {
            if (released)
                return;

            released = true;
            body.isKinematic = false;
            body.useGravity = true;
            body.WakeUp();
        }

        private void LockInPlace()
        {
            body.isKinematic = true;
            body.useGravity = false;
            body.position = transform.position;
            body.rotation = transform.rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
