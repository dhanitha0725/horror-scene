using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using HorrorGame.Combat;

/// <summary>
/// Keyboard bat swing for desktop / XR Device Simulator testing.
/// While the bat is held, pressing the swing key rotates the holding hand's attach point
/// through a wind-up -> strike -> recover arc, so the bat physically swings.
/// BatWeapon detects the fast-moving tip and deals damage as normal.
/// If the arc misses but a damageable target is right in front of the player,
/// an optional "assist hit" makes sure the strike still counts.
/// Put this on the bat (same object as XRGrabInteractable and BatWeapon).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public class BatKeyboardSwing : MonoBehaviour
{
    [Header("Input")]
#if ENABLE_INPUT_SYSTEM
    [Tooltip("Keyboard key that swings the bat (Input System).")]
    [SerializeField] private Key swingKey = Key.F;
#endif
    [Tooltip("Key used if the project runs the old Input Manager.")]
    [SerializeField] private KeyCode legacySwingKey = KeyCode.F;
    [Tooltip("Swing with the left mouse button.")]
    [SerializeField] private bool swingWithLeftClick = true;
    [Tooltip("Also allow the keyboard key above to swing.")]
    [SerializeField] private bool swingWithKey = false;

    [Header("Swing Motion")]
    [Tooltip("Degrees the bat pulls back before striking.")]
    [SerializeField, Range(10f, 120f)] private float windUpAngle = 70f;
    [Tooltip("Degrees the bat travels past centre on the strike.")]
    [SerializeField, Range(30f, 200f)] private float followThroughAngle = 110f;
    [SerializeField, Min(0.03f)] private float windUpTime = 0.12f;
    [SerializeField, Min(0.03f)] private float strikeTime = 0.16f;
    [SerializeField, Min(0.03f)] private float recoverTime = 0.25f;
    [Tooltip("How far the hand pushes forward during the strike (metres).")]
    [SerializeField, Min(0f)] private float lungeDistance = 0.25f;
    [Tooltip("true = swing right-to-left, false = left-to-right.")]
    [SerializeField] private bool swingRightToLeft = true;
    [Tooltip("Degrees the bat tilts further back over the shoulder during the wind-up.")]
    [SerializeField, Range(-60f, 60f)] private float windUpPitch = -20f;
    [Tooltip("Degrees the bat tips forward on the strike. ~110 brings a shoulder-held bat level in front.")]
    [SerializeField, Range(0f, 180f)] private float strikePitch = 110f;
    [Tooltip("Swing toward the nearest enemy: enemy on the left = right-to-left, on the right = left-to-right.")]
    [SerializeField] private bool autoDirection = true;
    [Tooltip("How far to look for an enemy when choosing the swing direction (metres).")]
    [SerializeField, Min(0.5f)] private float autoDirectionRange = 4f;

    [Header("Assist Hit")]
    [Tooltip("If the physical arc misses, still hit a damageable target directly in front.")]
    [SerializeField] private bool assistHit = true;
    [SerializeField, Min(0.3f)] private float assistRange = 1.8f;
    [SerializeField, Min(0.05f)] private float assistRadius = 0.4f;
    [SerializeField] private LayerMask assistLayers = ~0;
    [SerializeField, Min(1)] private int assistDamage = 1;
    [SerializeField, Min(0f)] private float assistImpactForce = 4.5f;
    [Tooltip("Impact sound for assist hits (BatWeapon plays its own Hit Clips for normal hits).")]
    [SerializeField] private AudioClip[] assistHitClips;
    [SerializeField, Range(0f, 1f)] private float assistHitVolume = 1f;

    [Header("Audio (optional)")]
    [Tooltip("Whoosh played at the start of the strike.")]
    [SerializeField] private AudioClip[] whooshClips;
    [SerializeField, Range(0f, 1f)] private float whooshVolume = 0.8f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private XRGrabInteractable grab;
    private BatWeapon weapon;
    private AudioSource audioSource;
    private Transform head;
    private bool swinging;
    private bool weaponHitDuringSwing;

    private Transform activeAttach;
    private Quaternion attachBaseRot;
    private Vector3 attachBasePos;

    public bool IsSwinging => swinging;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        weapon = GetComponent<BatWeapon>();
        audioSource = GetComponent<AudioSource>();
        if (weapon == null)
            Debug.LogWarning("[BatKeyboardSwing] No BatWeapon on this object; only assist hits will deal damage.", this);
    }

    private void OnEnable()
    {
        if (weapon != null) weapon.onHitTarget.AddListener(OnWeaponHit);
    }

    private void OnDisable()
    {
        if (weapon != null) weapon.onHitTarget.RemoveListener(OnWeaponHit);
        RestoreAttach();
        swinging = false;
        if (weapon != null) weapon.ExternalSwingActive = false;
    }

    private void OnWeaponHit()
    {
        if (swinging) weaponHitDuringSwing = true;
    }

    private void Update()
    {
        if (swinging || !SwingPressed()) return;

        if (!grab.isSelected || grab.interactorsSelecting.Count == 0)
        {
            Log("Pick up the bat first (grip), then press the swing key.");
            return;
        }
        StartCoroutine(SwingRoutine());
    }

    private bool SwingPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (swingWithLeftClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (swingWithKey && Keyboard.current != null && Keyboard.current[swingKey].wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (swingWithLeftClick && Input.GetMouseButtonDown(0)) return true;
        if (swingWithKey && Input.GetKeyDown(legacySwingKey)) return true;
#endif
        return false;
    }

    private IEnumerator SwingRoutine()
    {
        IXRSelectInteractor interactor = grab.interactorsSelecting[0];
        Transform attach = interactor != null ? interactor.GetAttachTransform(grab) : null;
        if (attach == null)
        {
            Log("Holding interactor has no attach transform; doing assist hit only.");
            if (assistHit) TryAssistHit();
            yield break;
        }

        swinging = true;
        weaponHitDuringSwing = false;
        if (weapon != null) weapon.ExternalSwingActive = true;
        activeAttach = attach;
        attachBaseRot = attach.localRotation;
        attachBasePos = attach.localPosition;

        Transform h = Head();
        Transform parent = attach.parent;
        Vector3 worldUp = h != null ? h.up : Vector3.up;
        Vector3 worldFwd = h != null ? Vector3.ProjectOnPlane(h.forward, Vector3.up).normalized : transform.forward;
        if (worldFwd.sqrMagnitude < 0.01f) worldFwd = transform.forward;
        Vector3 axisLocal = parent != null ? parent.InverseTransformDirection(worldUp) : worldUp;
        Vector3 fwdLocal = parent != null ? parent.InverseTransformDirection(worldFwd) : worldFwd;
        Vector3 worldRight = Vector3.Cross(worldUp, worldFwd).normalized;
        Vector3 rightLocal = parent != null ? parent.InverseTransformDirection(worldRight) : worldRight;
        float dir = swingRightToLeft ? -1f : 1f; // default: wind-up right, strike sweeps left
        if (autoDirection && TryGetTargetSide(worldRight, out float side))
        {
            dir = side < 0f ? -1f : 1f; // enemy on the left -> sweep left; on the right -> sweep right
            Log(side < 0f ? "Enemy on the LEFT: swinging right-to-left." : "Enemy on the RIGHT: swinging left-to-right.");
        }

        // 1. Wind-up
        yield return Animate(0f, -windUpAngle * dir, 0f, windUpPitch, 0f, 0f, windUpTime, axisLocal, rightLocal, fwdLocal, easeIn: false);
        if (!StillHeld()) { EndSwing(); yield break; }

        // 2. Strike
        PlayWhoosh();
        yield return Animate(-windUpAngle * dir, followThroughAngle * dir, windUpPitch, strikePitch, 0f, lungeDistance, strikeTime, axisLocal, rightLocal, fwdLocal, easeIn: true);
        if (!StillHeld()) { EndSwing(); yield break; }

        if (assistHit && !weaponHitDuringSwing) TryAssistHit();

        // 3. Recover
        yield return Animate(followThroughAngle * dir, 0f, strikePitch, 0f, lungeDistance, 0f, recoverTime, axisLocal, rightLocal, fwdLocal, easeIn: false);
        EndSwing();
    }

    private IEnumerator Animate(float fromAngle, float toAngle, float fromPitch, float toPitch, float fromLunge, float toLunge, float duration,
                                Vector3 axisLocal, Vector3 rightLocal, Vector3 fwdLocal, bool easeIn)
    {
        float t = 0f;
        while (t < 1f)
        {
            if (activeAttach == null || !StillHeld()) yield break;
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            float e = easeIn ? t * t : 1f - (1f - t) * (1f - t);
            float angle = Mathf.Lerp(fromAngle, toAngle, e);
            float lunge = Mathf.Lerp(fromLunge, toLunge, e);
            float pitch = Mathf.Lerp(fromPitch, toPitch, e);
            activeAttach.localRotation = Quaternion.AngleAxis(angle, axisLocal) * Quaternion.AngleAxis(pitch, rightLocal) * attachBaseRot;
            activeAttach.localPosition = attachBasePos + fwdLocal * lunge;
            yield return null;
        }
    }

    private bool StillHeld()
    {
        return grab != null && grab.isSelected;
    }

    private void EndSwing()
    {
        RestoreAttach();
        swinging = false;
        if (weapon != null) weapon.ExternalSwingActive = false;
        Log(weaponHitDuringSwing ? "Swing finished: BatWeapon registered a hit." : "Swing finished.");
    }

    private void RestoreAttach()
    {
        if (activeAttach != null)
        {
            activeAttach.localRotation = attachBaseRot;
            activeAttach.localPosition = attachBasePos;
        }
        activeAttach = null;
    }

    private void TryAssistHit()
    {
        Transform h = Head();
        if (h == null) return;

        Vector3 origin = h.position;
        Vector3 fwd = Vector3.ProjectOnPlane(h.forward, Vector3.up).normalized;
        if (fwd.sqrMagnitude < 0.01f) fwd = h.forward;

        RaycastHit[] hits = Physics.SphereCastAll(origin, assistRadius, fwd, assistRange, assistLayers, QueryTriggerInteraction.Collide);
        float best = float.MaxValue;
        IDamageable target = null;
        Vector3 point = origin + fwd * assistRange * 0.5f;
        foreach (RaycastHit hit in hits)
        {
            IDamageable d = hit.collider.GetComponentInParent<IDamageable>();
            if (d == null || d.IsDead) continue;
            if (hit.distance < best)
            {
                best = hit.distance;
                target = d;
                point = hit.point == Vector3.zero ? hit.collider.ClosestPoint(origin) : hit.point;
            }
        }

        if (target == null)
        {
            Log("Assist: nothing damageable in front.");
            return;
        }

        target.TakeDamage(new DamageInfo(
            amount: assistDamage,
            hitPoint: point,
            hitNormal: -fwd,
            hitDirection: fwd,
            impactForce: assistImpactForce,
            damageSource: gameObject));
        PlayAssistHitSound(point);
        if (weapon != null) weapon.onHitTarget?.Invoke();
        Log("Assist hit landed.");
    }

    private void PlayAssistHitSound(Vector3 point)
    {
        if (assistHitClips == null || assistHitClips.Length == 0) return;
        AudioClip clip = assistHitClips[Random.Range(0, assistHitClips.Length)];
        if (clip == null) return;
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip, assistHitVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, point, assistHitVolume);
        }
    }

    /// <summary>Finds the nearest living enemy and returns which side of the bat it is on (negative = left).</summary>
    private bool TryGetTargetSide(Vector3 worldRight, out float side)
    {
        side = 0f;
        Collider[] cols = Physics.OverlapSphere(transform.position, autoDirectionRange, assistLayers, QueryTriggerInteraction.Collide);
        float bestDist = float.MaxValue;
        Transform best = null;
        foreach (Collider c in cols)
        {
            IDamageable d = c.GetComponentInParent<IDamageable>();
            if (d == null || d.IsDead) continue;
            Component comp = d as Component;
            if (comp == null) continue;
            float dist = (comp.transform.position - transform.position).sqrMagnitude;
            if (dist < bestDist) { bestDist = dist; best = comp.transform; }
        }
        if (best == null) return false;
        Vector3 to = Vector3.ProjectOnPlane(best.position - transform.position, Vector3.up);
        side = Vector3.Dot(to, worldRight);
        if (Mathf.Abs(side) < 0.05f) side = swingRightToLeft ? -1f : 1f; // dead ahead: use default
        return true;
    }

    private void PlayWhoosh()
    {
        if (whooshClips == null || whooshClips.Length == 0 || audioSource == null) return;
        AudioClip clip = whooshClips[Random.Range(0, whooshClips.Length)];
        if (clip == null) return;
        audioSource.pitch = Random.Range(0.95f, 1.1f);
        audioSource.PlayOneShot(clip, whooshVolume);
    }

    private Transform Head()
    {
        if (head == null && Camera.main != null) head = Camera.main.transform;
        return head;
    }

    private void Log(string msg)
    {
        if (debugLogs) Debug.Log("[BatKeyboardSwing] " + msg, this);
    }
}
