using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace HorrorGame.Combat
{
    /// <summary>
    /// Bat Weapon component for VR and OpenXR Mouse Simulator.
    /// Tracks swing speed at the tip of the bat, inflicts damage on IDamageable targets,
    /// triggers haptic feedback, and signals pickup events to the encounter director.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class BatWeapon : MonoBehaviour
    {
        [Header("Weapon Stats")]
        [Tooltip("Damage dealt to enemies per hit.")]
        [SerializeField] private int damageAmount = 1;
        [Tooltip("Minimum tip speed (m/s) required to register a hit. Prevents soft taps from dealing damage.")]
        [SerializeField, Min(0.1f)] private float minSwingSpeed = 1.2f;
        [Tooltip("Knockback / impact force applied to hit targets.")]
        [SerializeField, Min(0f)] private float impactForce = 3.5f;
        [Tooltip("Minimum seconds between successive hits.")]
        [SerializeField, Min(0.05f)] private float hitCooldown = 0.35f;

        [Header("Swing & Detection Points")]
        [Tooltip("Transform at the far tip of the bat. If empty, the script will create an offset from this object's forward/up.")]
        [SerializeField] private Transform batTip;
        [Tooltip("Estimated distance to the bat tip from center if batTip is not assigned.")]
        [SerializeField] private float autoTipDistance = 0.75f;
        [Tooltip("Layers that can receive damage (e.g. Ghost, Default, Props).")]
        [SerializeField] private LayerMask targetLayers = ~0;

        [Header("OpenXR Simulator / Desktop Support")]
        [Tooltip("If true, pressing the swing attack key (Left Click or Space) triggers an active swing state with boosted speed calculation for mouse emulation.")]
        [SerializeField] private bool allowSimulatedSwingAction = true;
        [SerializeField] private KeyCode simulatedSwingKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode simulatedSwingKeyAlt = KeyCode.Space;
        [Tooltip("Duration in seconds of the simulated swing window.")]
        [SerializeField] private float simulatedSwingDuration = 0.4f;

        [Header("Audio & Effects")]
        [Tooltip("Sound played on successful hit against an enemy.")]
        [SerializeField] private AudioClip[] hitClips;
        [SerializeField, Range(0f, 1f)] private float hitSoundVolume = 1f;
        [Tooltip("Optional hit particle effect instantiated at the impact point.")]
        [SerializeField] private GameObject hitVfxPrefab;

        [Header("Haptics (VR Controller)")]
        [SerializeField, Range(0f, 1f)] private float hapticIntensity = 0.7f;
        [SerializeField, Min(0.01f)] private float hapticDuration = 0.15f;

        [Header("Events")]
        [Tooltip("Invoked when the player first picks up the bat.")]
        public UnityEvent onBatFirstPickedUp;
        public UnityEvent onBatPickedUp;
        public UnityEvent onBatDropped;
        public UnityEvent onHitTarget;

        /// <summary>
        /// Global event fired when any bat is picked up for the first time.
        /// Useful for Encounter Directors without direct scene references.
        /// </summary>
        public static event Action<BatWeapon> OnAnyBatFirstPickedUp;

        private Rigidbody rb;
        private AudioSource audioSource;
        private Vector3 lastTipPosition;
        private float currentTipSpeed;
        private float lastHitTime = -999f;
        private bool isHeld = false;
        private bool hasBeenPickedUpOnce = false;
        private float simulatedSwingTimer = 0f;
        private XRGrabInteractable grabInteractable;

        public bool IsHeld => isHeld;
        public float CurrentTipSpeed => currentTipSpeed;
        public bool HasBeenPickedUpOnce => hasBeenPickedUpOnce;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // 3D spatial
                audioSource.playOnAwake = false;
            }

            if (batTip == null)
            {
                // Create a virtual tip child if none was explicitly wired
                GameObject tipObj = new GameObject("BatTip_Auto");
                tipObj.transform.SetParent(transform, false);
                tipObj.transform.localPosition = Vector3.forward * autoTipDistance;
                batTip = tipObj.transform;
            }

            lastTipPosition = batTip.position;

            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnSelectEntered);
                grabInteractable.selectExited.AddListener(OnSelectExited);
            }
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
                grabInteractable.selectExited.RemoveListener(OnSelectExited);
            }
        }

        private void Update()
        {
            // Calculate instantaneous tip speed
            Vector3 tipPos = batTip != null ? batTip.position : transform.position;
            if (Time.deltaTime > 0.0001f)
            {
                float calculatedSpeed = Vector3.Distance(tipPos, lastTipPosition) / Time.deltaTime;
                // Smooth speed slightly to prevent 1-frame jitter
                currentTipSpeed = Mathf.Lerp(currentTipSpeed, calculatedSpeed, 0.4f);
            }
            lastTipPosition = tipPos;

            // Handle Desktop / OpenXR Mouse Simulator manual swing helper
            if (allowSimulatedSwingAction && isHeld)
            {
                if (Input.GetKeyDown(simulatedSwingKey) || Input.GetKeyDown(simulatedSwingKeyAlt))
                {
                    simulatedSwingTimer = simulatedSwingDuration;
                }
            }

            if (simulatedSwingTimer > 0f)
            {
                simulatedSwingTimer -= Time.deltaTime;
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            isHeld = true;
            onBatPickedUp?.Invoke();

            if (!hasBeenPickedUpOnce)
            {
                hasBeenPickedUpOnce = true;
                onBatFirstPickedUp?.Invoke();
                OnAnyBatFirstPickedUp?.Invoke(this);
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            isHeld = false;
            onBatDropped?.Invoke();
        }

        public void SetHeldManually(bool held)
        {
            isHeld = held;
            if (isHeld && !hasBeenPickedUpOnce)
            {
                hasBeenPickedUpOnce = true;
                onBatFirstPickedUp?.Invoke();
                OnAnyBatFirstPickedUp?.Invoke(this);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            ProcessHit(collision.gameObject, collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position, collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.up);
        }

        private void OnTriggerEnter(Collider other)
        {
            ProcessHit(other.gameObject, other.ClosestPoint(batTip != null ? batTip.position : transform.position), (transform.position - other.transform.position).normalized);
        }

        private void ProcessHit(GameObject hitObj, Vector3 hitPoint, Vector3 hitNormal)
        {
            // Cooldown check
            if (Time.time - lastHitTime < hitCooldown)
                return;

            // Check if layer is in target layers
            if ((targetLayers.value & (1 << hitObj.layer)) == 0)
                return;

            // Check swing velocity or simulated swing window
            bool isSwinging = currentTipSpeed >= minSwingSpeed || simulatedSwingTimer > 0f;
            if (!isSwinging && isHeld)
            {
                // In case of fast physics sweep where delta speed wasn't captured in Update:
                if (rb.linearVelocity.magnitude >= minSwingSpeed * 0.8f || rb.angularVelocity.magnitude >= 3f)
                {
                    isSwinging = true;
                }
            }

            // If not held or not swinging with enough force, skip damage
            if (!isSwinging)
                return;

            // Look for IDamageable on hit object or its parents
            IDamageable damageable = hitObj.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                lastHitTime = Time.time;

                Vector3 swingDir = (batTip.position - lastTipPosition).normalized;
                if (swingDir.sqrMagnitude < 0.01f)
                    swingDir = transform.forward;

                DamageInfo damageInfo = new DamageInfo(
                    amount: damageAmount,
                    hitPoint: hitPoint,
                    hitNormal: hitNormal,
                    hitDirection: swingDir,
                    impactForce: impactForce,
                    damageSource: gameObject
                );

                damageable.TakeDamage(damageInfo);
                onHitTarget?.Invoke();

                // Sound & VFX
                PlayHitSound(hitPoint);
                SpawnHitVfx(hitPoint, hitNormal);

                // VR Haptics
                SendHapticFeedback();
            }
        }

        private void PlayHitSound(Vector3 position)
        {
            if (hitClips != null && hitClips.Length > 0)
            {
                AudioClip clip = hitClips[UnityEngine.Random.Range(0, hitClips.Length)];
                if (clip != null)
                {
                    audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                    audioSource.PlayOneShot(clip, hitSoundVolume);
                }
            }
        }

        private void SpawnHitVfx(Vector3 point, Vector3 normal)
        {
            if (hitVfxPrefab != null)
            {
                Quaternion rot = normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity;
                GameObject vfx = Instantiate(hitVfxPrefab, point, rot);
                Destroy(vfx, 3f);
            }
        }

        private void SendHapticFeedback()
        {
            if (grabInteractable != null && grabInteractable.isSelected)
            {
                foreach (var interactor in grabInteractable.interactorsSelecting)
                {
                    if (interactor is XRBaseInputInteractor inputInteractor)
                    {
                        inputInteractor.SendHapticImpulse(hapticIntensity, hapticDuration);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (batTip != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(batTip.position, 0.06f);
            }
        }
    }
}
