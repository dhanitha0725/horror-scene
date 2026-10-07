using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace HorrorGame.AI
{
    public enum GhostState
    {
        Inactive,
        Spawning,
        StareTwitch,
        Chase,
        Attack,
        Staggered,
        Dead
    }

    /// <summary>
    /// AI Controller for the Creepy Teen Girl horror character.
    /// Drives NavMeshAgent navigation, procedural creepy head-tracking/twitching,
    /// state transitions, attacks, stagger flinches, and death resolution.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public class GhostController : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private GhostState currentState = GhostState.Inactive;

        [Header("Target & Tracking")]
        [Tooltip("Player Transform to chase and face. If null, automatically finds the Main Camera.")]
        [SerializeField] private Transform playerTarget;
        [Tooltip("Optional head or neck bone for procedural creepy snapping/twitching.")]
        [SerializeField] private Transform headBone;

        [Header("Movement & Pacing")]
        [Tooltip("Seconds the ghost stands twitching and staring before starting the chase.")]
        [SerializeField, Min(0.5f)] private float stareDuration = 2.0f;
        [Tooltip("NavMesh chase speed.")]
        [SerializeField, Min(0.05f)] private float chaseSpeed = 3.2f;
        [Tooltip("NavMesh acceleration.")]
        [SerializeField, Min(5f)] private float chaseAcceleration = 14f;
        [Tooltip("Distance at which ghost attacks.")]
        [SerializeField, Min(0.5f)] private float attackDistance = 1.3f;
        [Tooltip("Cooldown between attacks.")]
        [SerializeField, Min(0.5f)] private float attackCooldown = 1.5f;
        [Tooltip("Duration ghost is stunned/staggered when struck by the bat.")]
        [SerializeField, Min(0.2f)] private float staggerDuration = 0.85f;

        [Header("Creepy Procedural Twitch")]
        [Tooltip("Enable procedural twitching in StareTwitch state.")]
        [SerializeField] private bool proceduralTwitch = true;
        [SerializeField] private float twitchIntervalMin = 0.15f;
        [SerializeField] private float twitchIntervalMax = 0.45f;
        [SerializeField] private float maxTwitchAngle = 25f;

        [Header("Events")]
        public UnityEvent onGhostRevealed;
        public UnityEvent onGhostChaseStarted;
        public UnityEvent onGhostAttacked;
        public UnityEvent onGhostStaggered;
        public UnityEvent onGhostDefeated;

        private NavMeshAgent agent;
        private Animator animator;
        private GhostAudio ghostAudio;
        private GhostDamageReceiver damageReceiver;

        private float stateTimer = 0f;
        private float lastAttackTime = -999f;
        private Vector3 targetTwitchOffset;
        private Coroutine twitchRoutine;

        // Animator parameter hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsTwitchingHash = Animator.StringToHash("IsTwitching");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int HitTriggerHash = Animator.StringToHash("Hit");
        private static readonly int DieTriggerHash = Animator.StringToHash("Die");

        public GhostState CurrentState => currentState;
        public bool IsActive => currentState != GhostState.Inactive && currentState != GhostState.Dead;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            ghostAudio = GetComponent<GhostAudio>();
            damageReceiver = GetComponent<GhostDamageReceiver>();

            agent.speed = chaseSpeed;
            agent.acceleration = chaseAcceleration;
            agent.stoppingDistance = attackDistance * 0.8f;
        }

        private void Start()
        {
            if (playerTarget == null && Camera.main != null)
            {
                playerTarget = Camera.main.transform;
            }

            if (currentState == GhostState.Inactive)
            {
                SetVisualsActive(false);
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
            }
            else
            {
                SetState(currentState);
            }
        }

        private void Update()
        {
            if (playerTarget == null && Camera.main != null)
            {
                playerTarget = Camera.main.transform;
            }

            switch (currentState)
            {
                case GhostState.Inactive:
                case GhostState.Dead:
                    break;

                case GhostState.StareTwitch:
                    UpdateStareTwitch();
                    break;

                case GhostState.Chase:
                    UpdateChase();
                    break;

                case GhostState.Attack:
                    UpdateAttack();
                    break;

                case GhostState.Staggered:
                    UpdateStaggered();
                    break;
            }

            // Sync speed parameter to Animator if available
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                float speed = agent.enabled && agent.isOnNavMesh ? agent.velocity.magnitude : 0f;
                animator.SetFloat(SpeedHash, speed);
            }
        }

        /// <summary>
        /// Activates and reveals the ghost at a designated position.
        /// </summary>
        public void SpawnAndReveal(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            transform.rotation = spawnRotation;

            // Warp to a valid point even when the object was initially parked outside
            // the NavMesh. This keeps the reveal visible and lets the chase begin.
            if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                if (agent.enabled)
                {
                    agent.Warp(navHit.position);
                }
                else
                {
                    transform.position = navHit.position;
                }
            }
            else
            {
                transform.position = spawnPosition;
                Debug.LogWarning($"[GhostController:{name}] Reveal point is not on the NavMesh; the ghost will not be able to chase.", this);
            }

            SetVisualsActive(true);
            SetState(GhostState.StareTwitch);

            if (ghostAudio != null)
            {
                ghostAudio.PlayRevealScream();
                ghostAudio.PlayWhisper(loop: true);
            }

            onGhostRevealed?.Invoke();
        }

        public void SetState(GhostState newState)
        {
            currentState = newState;
            stateTimer = 0f;

            switch (newState)
            {
                case GhostState.Inactive:
                    SetVisualsActive(false);
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    if (ghostAudio != null) ghostAudio.StopWhisperLoop();
                    break;

                case GhostState.StareTwitch:
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    if (animator != null && animator.runtimeAnimatorController != null)
                        animator.SetBool(IsTwitchingHash, true);
                    if (proceduralTwitch && twitchRoutine == null)
                        twitchRoutine = StartCoroutine(ProceduralTwitchLoop());
                    break;

                case GhostState.Chase:
                    if (twitchRoutine != null) { StopCoroutine(twitchRoutine); twitchRoutine = null; }
                    if (animator != null && animator.runtimeAnimatorController != null)
                        animator.SetBool(IsTwitchingHash, false);
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        agent.speed = chaseSpeed;
                    }
                    if (ghostAudio != null) ghostAudio.PlayChaseScream();
                    onGhostChaseStarted?.Invoke();
                    break;

                case GhostState.Attack:
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    if (animator != null && animator.runtimeAnimatorController != null)
                        animator.SetTrigger(AttackTriggerHash);
                    onGhostAttacked?.Invoke();
                    break;

                case GhostState.Staggered:
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    if (animator != null && animator.runtimeAnimatorController != null)
                        animator.SetTrigger(HitTriggerHash);
                    if (ghostAudio != null) ghostAudio.PlayPainSound();
                    onGhostStaggered?.Invoke();
                    break;

                case GhostState.Dead:
                    if (twitchRoutine != null) { StopCoroutine(twitchRoutine); twitchRoutine = null; }
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    if (animator != null && animator.runtimeAnimatorController != null)
                        animator.SetTrigger(DieTriggerHash);
                    if (ghostAudio != null) ghostAudio.PlayDeathSound();
                    onGhostDefeated?.Invoke();
                    break;
            }
        }

        private void UpdateStareTwitch()
        {
            stateTimer += Time.deltaTime;

            // Face player
            if (playerTarget != null)
            {
                Vector3 lookDir = (playerTarget.position - transform.position);
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);
                }
            }

            if (stateTimer >= stareDuration)
            {
                SetState(GhostState.Chase);
            }
        }

        private void UpdateChase()
        {
            if (playerTarget == null) return;

            if (agent.isOnNavMesh)
            {
                agent.SetDestination(playerTarget.position);
            }

            float dist = Vector3.Distance(transform.position, playerTarget.position);
            if (dist <= attackDistance && Time.time - lastAttackTime > attackCooldown)
            {
                SetState(GhostState.Attack);
            }
        }

        private void UpdateAttack()
        {
            stateTimer += Time.deltaTime;
            lastAttackTime = Time.time;

            // Look towards player while attacking
            if (playerTarget != null)
            {
                Vector3 lookDir = (playerTarget.position - transform.position);
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
                }
            }

            if (stateTimer >= 1.0f)
            {
                SetState(GhostState.Chase);
            }
        }

        private void UpdateStaggered()
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= staggerDuration)
            {
                SetState(GhostState.Chase);
            }
        }

        public void ApplyStagger(Vector3 knockbackDir, float force)
        {
            if (currentState == GhostState.Dead) return;

            SetState(GhostState.Staggered);

            // Small knockback nudge
            if (agent.isOnNavMesh)
            {
                Vector3 nudge = knockbackDir.normalized * Mathf.Clamp(force * 0.25f, 0.2f, 1.2f);
                nudge.y = 0;
                agent.Move(nudge);
            }
        }

        public void OnDefeated()
        {
            SetState(GhostState.Dead);
        }

        private void SetVisualsActive(bool active)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = active;
            }
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                c.enabled = active;
            }
        }

        private IEnumerator ProceduralTwitchLoop()
        {
            while (currentState == GhostState.StareTwitch)
            {
                float wait = Random.Range(twitchIntervalMin, twitchIntervalMax);
                yield return new WaitForSeconds(wait);

                if (headBone != null)
                {
                    // Sudden snapping rotation
                    headBone.localRotation = Quaternion.Euler(
                        Random.Range(-maxTwitchAngle, maxTwitchAngle),
                        Random.Range(-maxTwitchAngle, maxTwitchAngle),
                        Random.Range(-maxTwitchAngle, maxTwitchAngle)
                    );
                }
            }
        }
    }
}
