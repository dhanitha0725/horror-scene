using UnityEngine;
using UnityEngine.InputSystem;

public class BatPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Transform playerHand;      // Assign BatSocket
    public Transform gripPoint;       // Assign Bat Grip Attach
    public float pickupDistance = 2f;

    private Transform player;
    private Rigidbody rb;
    private Collider batCollider;

    private bool pickedUp = false;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        rb = GetComponent<Rigidbody>();
        batCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (pickedUp)
            return;

        if (player == null)
            return;

        if (playerHand == null)
            return;

        if (gripPoint == null)
            return;

        if (Keyboard.current == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance <= pickupDistance &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            PickUpBat();
        }
    }

    void PickUpBat()
    {
        pickedUp = true;

        // Stop physics
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Prevent bat collider from pushing the player
        if (batCollider != null)
        {
            batCollider.enabled = false;
        }

        // Parent bat to BatSocket
        transform.SetParent(playerHand, true);

        // -------------------------
        // ALIGN ROTATION
        // -------------------------

        Quaternion rotationDifference =
            playerHand.rotation *
            Quaternion.Inverse(gripPoint.rotation);

        transform.rotation =
            rotationDifference * transform.rotation;

        // -------------------------
        // ALIGN POSITION
        // -------------------------

        Vector3 positionDifference =
            playerHand.position - gripPoint.position;

        transform.position += positionDifference;
    }
}