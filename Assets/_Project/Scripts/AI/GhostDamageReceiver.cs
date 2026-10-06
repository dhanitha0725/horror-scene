using UnityEngine;
using UnityEngine.Events;
using HorrorGame.Combat;

namespace HorrorGame.AI
{
    /// <summary>
    /// Handles damage reception, health, invulnerability frames, hit VFX, and defeat triggers for the Ghost.
    /// </summary>
    [DisallowMultipleComponent]
    public class GhostDamageReceiver : MonoBehaviour, IDamageable
    {
        [Header("Health & Resistance")]
        [Tooltip("Number of hits required to banish/defeat the ghost.")]
        [SerializeField, Min(1)] private int maxHits = 3;
        [Tooltip("Invulnerability window in seconds after taking a hit.")]
        [SerializeField, Min(0.1f)] private float invulnerabilityDuration = 0.4f;

        [Header("Effects")]
        [Tooltip("Dark mist or blood particle effect spawned on hit.")]
        [SerializeField] private GameObject hitVfxPrefab;
        [Tooltip("Dissolve or vanish effect spawned on defeat.")]
        [SerializeField] private GameObject defeatVfxPrefab;
        [Tooltip("Seconds after defeat before destroying or fully hiding the GameObject.")]
        [SerializeField, Min(0.5f)] private float destroyDelay = 2.5f;

        [Header("Events")]
        public UnityEvent<int> onHealthChanged;
        public UnityEvent<DamageInfo> onDamaged;
        public UnityEvent onDefeated;

        private int currentHealth;
        private bool isDead = false;
        private float lastDamageTime = -999f;
        private GhostController controller;

        public bool IsDead => isDead;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHits;

        private void Awake()
        {
            controller = GetComponent<GhostController>();
            currentHealth = maxHits;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (isDead) return;

            // Invulnerability check
            if (Time.time - lastDamageTime < invulnerabilityDuration)
                return;

            lastDamageTime = Time.time;
            currentHealth -= info.amount;
            onHealthChanged?.Invoke(currentHealth);
            onDamaged?.Invoke(info);

            // Spawn hit particle effect
            SpawnVfx(hitVfxPrefab, info.hitPoint, info.hitNormal);

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                if (controller != null)
                {
                    controller.ApplyStagger(info.hitDirection, info.impactForce);
                }
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            // Spawn defeat VFX
            SpawnVfx(defeatVfxPrefab, transform.position + Vector3.up * 1f, Vector3.up);

            if (controller != null)
            {
                controller.OnDefeated();
            }

            onDefeated?.Invoke();

            // Disable colliders so bat no longer collides
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            Destroy(gameObject, destroyDelay);
        }

        private void SpawnVfx(GameObject prefab, Vector3 position, Vector3 normal)
        {
            if (prefab != null)
            {
                Quaternion rot = normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity;
                GameObject instance = Instantiate(prefab, position, rot);
                Destroy(instance, 3f);
            }
        }

        public void ResetHealth()
        {
            currentHealth = maxHits;
            isDead = false;
        }
    }
}
