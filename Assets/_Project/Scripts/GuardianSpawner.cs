using System.Collections;
using HorrorGame.AI;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Second ghost encounter: a guardian ghost that only appears once the player HAS the key
/// and comes near this object (the safe). Without the key she never appears.
/// The guardian GameObject stays switched off (so it can't be hit or heard) until spawned,
/// then is revealed through GhostController.SpawnAndReveal, standing between the player and the safe.
/// Put it on the safe.
/// </summary>
[DisallowMultipleComponent]
public class GuardianSpawner : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private KeyItem requiredKey;
    [Tooltip("The guardian ghost (a separate ghost GameObject, switched off in the scene).")]
    [SerializeField] private GhostController guardian;
    [Tooltip("Where she appears. If empty: in front of this object by 'spawnDistance'.")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField, Min(0.5f)] private float spawnDistance = 1.6f;

    [Header("Trigger")]
    [Tooltip("She appears when the player (with the key) is within this distance of the safe (metres).")]
    [SerializeField, Min(1f)] private float triggerRadius = 6f;
    [Tooltip("Small pause after the trigger before she appears.")]
    [SerializeField, Min(0f)] private float appearDelay = 0.6f;

    [Header("Feedback")]
    [SerializeField] private string appearTitle = "SHE'S BACK";
    [SerializeField, TextArea(1, 4)] private string appearBody = "She won't let you take it.\nFinish her.";
    [SerializeField, Min(0.5f)] private float messageSeconds = 3.5f;
    [SerializeField] private Color messageColor = new Color(0.85f, 0.05f, 0.05f);
    [SerializeField] private UnityEvent onGuardianSpawned;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public bool HasSpawned { get; private set; }

    private void Awake()
    {
        if (guardian == null) { Debug.LogWarning("[GuardianSpawner] No guardian assigned.", this); return; }
        if (guardian.gameObject.activeSelf) guardian.gameObject.SetActive(false); // hidden, unhittable, silent until needed
    }

    private void Update()
    {
        if (HasSpawned || guardian == null || requiredKey == null || !requiredKey.IsCollected) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 d = Vector3.ProjectOnPlane(cam.transform.position - transform.position, Vector3.up);
        if (d.magnitude > triggerRadius) return;

        HasSpawned = true;
        StartCoroutine(Spawn(cam.transform));
    }

    private IEnumerator Spawn(Transform player)
    {
        if (appearDelay > 0f) yield return new WaitForSeconds(appearDelay);

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position + transform.forward * spawnDistance;
        Vector3 look = Vector3.ProjectOnPlane(player.position - pos, Vector3.up);
        Quaternion rot = look.sqrMagnitude > 0.001f ? Quaternion.LookRotation(look, Vector3.up) : transform.rotation;

        guardian.gameObject.SetActive(true);
        // let Awake/Start run (Start hides the visuals while Inactive) before revealing
        yield return null;
        yield return null;
        guardian.SpawnAndReveal(pos, rot);

        if (!string.IsNullOrEmpty(appearTitle)) WorldMessage.ShowGlobal(appearTitle, appearBody, messageSeconds, messageColor);
        onGuardianSpawned?.Invoke();
        if (debugLogs) Debug.Log("[GuardianSpawner] Guardian revealed at " + pos, this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position + transform.forward * spawnDistance;
        Gizmos.DrawSphere(pos + Vector3.up * 0.9f, 0.2f);
    }
}
