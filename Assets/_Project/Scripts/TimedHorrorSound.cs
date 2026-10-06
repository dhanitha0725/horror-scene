using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Plays a 3D horror sound after a delay (default 10 s after the scene starts).
/// Can place the sound behind the player's head, circle around them, or stay fixed,
/// and can repeat at random intervals. Fires onPlayed so other effects (light bursts) can sync.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class TimedHorrorSound : MonoBehaviour
{
    public enum Placement
    {
        Fixed,              // plays where this GameObject is
        BehindPlayer,       // spawns just behind the player's head, slightly left or right
        CircleAroundPlayer  // starts behind and slowly orbits the player's head while playing
    }

    [Header("Sound")]
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.9f;
    [Tooltip("Random pitch per play so repeats don't sound identical.")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Timing")]
    [Tooltip("Seconds after the scene starts before the first play.")]
    [SerializeField, Min(0f)] private float startDelay = 10f;
    [SerializeField] private bool repeat = false;
    [Tooltip("Min / max seconds between repeats.")]
    [SerializeField] private Vector2 repeatInterval = new Vector2(25f, 45f);
    [Tooltip("Maximum number of plays when repeating. 0 = unlimited.")]
    [SerializeField, Min(0)] private int maxPlays = 0;

    [Header("Position")]
    [SerializeField] private Placement placement = Placement.BehindPlayer;
    [Tooltip("Player head. Leave empty to use the Main Camera.")]
    [SerializeField] private Transform playerHead;
    [SerializeField, Min(0.2f)] private float distanceFromHead = 1.2f;
    [Tooltip("Random left/right offset so it feels like it comes from one ear.")]
    [SerializeField, Min(0f)] private float sideOffset = 0.5f;
    [Tooltip("Orbit speed for CircleAroundPlayer (degrees per second).")]
    [SerializeField] private float circleSpeed = 45f;

    [Header("3D Audio")]
    [SerializeField, Min(0.1f)] private float minDistance = 0.5f;
    [SerializeField, Min(0.5f)] private float maxDistance = 15f;

    [Header("Events")]
    [Tooltip("Called each time the sound starts. Hook light bursts or other scares here.")]
    [SerializeField] private UnityEvent onPlayed;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private AudioSource source;
    private int playCount;
    private Coroutine routine;

    public int PlayCount => playCount;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = Mathf.Max(minDistance + 0.1f, maxDistance);
        source.dopplerLevel = 0f;
        source.spread = 0f;

        if (pitchRange.y < pitchRange.x) pitchRange.y = pitchRange.x;
        if (repeatInterval.x < 0.1f) repeatInterval.x = 0.1f;
        if (repeatInterval.y < repeatInterval.x) repeatInterval.y = repeatInterval.x;
    }

    private void OnEnable()
    {
        routine = StartCoroutine(Run());
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (source != null) source.Stop();
    }

    private IEnumerator Run()
    {
        if (clip == null)
        {
            Debug.LogWarning($"[TimedHorrorSound:{name}] No AudioClip assigned. Nothing will play.", this);
            yield break;
        }

        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        while (true)
        {
            yield return PlayOnce();

            bool limitReached = maxPlays > 0 && playCount >= maxPlays;
            if (!repeat || limitReached) yield break;

            yield return new WaitForSeconds(Random.Range(repeatInterval.x, repeatInterval.y));
        }
    }

    private IEnumerator PlayOnce()
    {
        Transform head = ResolveHead();
        float side = Random.value < 0.5f ? -1f : 1f;
        float angle = 180f + side * 25f; // start behind, slightly to one side

        if (placement != Placement.Fixed && head != null)
            transform.position = PositionAround(head, angle, side);

        source.clip = clip;
        source.volume = volume;
        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.Play();
        playCount++;
        Log($"Play #{playCount} at {transform.position}");
        onPlayed?.Invoke();

        // Keep following / orbiting the head while the clip plays
        while (source.isPlaying)
        {
            if (head != null && placement != Placement.Fixed)
            {
                if (placement == Placement.CircleAroundPlayer)
                    angle += circleSpeed * side * Time.deltaTime;
                transform.position = PositionAround(head, angle, placement == Placement.BehindPlayer ? side : 0f);
            }
            yield return null;
        }
    }

    private Vector3 PositionAround(Transform head, float angleFromForward, float sideSign)
    {
        Vector3 fwd = head.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        fwd.Normalize();

        Vector3 dir = Quaternion.AngleAxis(angleFromForward, Vector3.up) * fwd;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        return head.position + dir * distanceFromHead + right * (sideSign * sideOffset * 0.5f);
    }

    private Transform ResolveHead()
    {
        if (playerHead != null) return playerHead;
        Camera cam = Camera.main;
        if (cam != null) playerHead = cam.transform;
        else Debug.LogWarning($"[TimedHorrorSound:{name}] No Main Camera found; playing at this object's position.", this);
        return playerHead;
    }

    private void Log(string msg)
    {
        if (debugLogs) Debug.Log($"[TimedHorrorSound:{name}] {msg}", this);
    }

    // ---------- Public API ----------

    /// <summary>Play immediately (e.g. from a trigger zone), ignoring the delay.</summary>
    public void PlayNow()
    {
        if (clip == null || !isActiveAndEnabled) return;
        StartCoroutine(PlayOnce());
    }

    /// <summary>Stop the timer and any sound currently playing.</summary>
    public void StopAll()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        source.Stop();
    }
}
