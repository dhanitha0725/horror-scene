using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HorrorGame.Combat;
using HorrorGame.AI;

namespace HorrorGame.Encounter
{
    /// <summary>
    /// Coordinates the full 12-second tension timeline and audio-visual choreography
    /// after the player picks up the baseball bat.
    /// </summary>
    [DisallowMultipleComponent]
    public class BatEncounterDirector : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Direct reference to the BatWeapon. If null, automatically listens to global bat pickup.")]
        [SerializeField] private BatWeapon batWeapon;
        [Tooltip("The Ghost Controller in the scene.")]
        [SerializeField] private GhostController ghost;
        [Tooltip("Where the ghost manifests on reveal.")]
        [SerializeField] private Transform ghostSpawnPoint;
        [Tooltip("Flickering corridor lights to control during the encounter.")]
        [SerializeField] private List<FlickeringLight> corridorLights = new List<FlickeringLight>();

        [Header("Timeline Pacing (Seconds)")]
        [Tooltip("Seconds after bat pickup before Hint 1 (Whisper & slight flicker).")]
        [SerializeField, Min(1f)] private float delayToHint1 = 3.5f;
        [Tooltip("Seconds after Hint 1 before Hint 2 (Surge, footsteps, heavier flicker).")]
        [SerializeField, Min(1f)] private float delayToHint2 = 4.5f;
        [Tooltip("Seconds after Hint 2 before the sudden blackout.")]
        [SerializeField, Min(1f)] private float delayToBlackout = 3.5f;
        [Tooltip("Duration in seconds of the total blackout before the ghost appears.")]
        [SerializeField, Range(0.2f, 2f)] private float blackoutDuration = 0.6f;

        [Header("Audio")]
        [Tooltip("AudioSource used by the director for environmental stings.")]
        [SerializeField] private AudioSource directorAudioSource;
        [Tooltip("Sound played on Hint 1 (faint whisper/crying).")]
        [SerializeField] private AudioClip hint1Clip;
        [Tooltip("Sound played on Hint 2 (footsteps/creak/surge).")]
        [SerializeField] private AudioClip hint2Clip;
        [Tooltip("Jumpscare sting played precisely as the ghost appears.")]
        [SerializeField] private AudioClip revealJumpscareSting;

        [Header("Events")]
        public UnityEvent onEncounterStarted;
        public UnityEvent onHint1Triggered;
        public UnityEvent onHint2Triggered;
        public UnityEvent onBlackoutStarted;
        public UnityEvent onGhostRevealed;
        public UnityEvent onEncounterCompleted;

        private bool encounterStarted = false;
        private Coroutine sequenceCoroutine;

        private void Awake()
        {
            if (directorAudioSource == null)
            {
                directorAudioSource = GetComponent<AudioSource>();
                if (directorAudioSource == null)
                {
                    directorAudioSource = gameObject.AddComponent<AudioSource>();
                    directorAudioSource.spatialBlend = 0.5f;
                    directorAudioSource.playOnAwake = false;
                }
            }

            // Auto-discover lights if none manually assigned
            if (corridorLights.Count == 0)
            {
                corridorLights.AddRange(FindObjectsByType<FlickeringLight>(FindObjectsInactive.Exclude));
            }
        }

        private void OnEnable()
        {
            BatWeapon.OnAnyBatFirstPickedUp += HandleBatPickedUp;

            if (batWeapon != null)
            {
                batWeapon.onBatFirstPickedUp.AddListener(StartEncounter);
            }

            if (ghost != null)
            {
                ghost.onGhostDefeated.AddListener(HandleGhostDefeated);
            }
        }

        private void OnDisable()
        {
            BatWeapon.OnAnyBatFirstPickedUp -= HandleBatPickedUp;

            if (batWeapon != null)
            {
                batWeapon.onBatFirstPickedUp.RemoveListener(StartEncounter);
            }

            if (ghost != null)
            {
                ghost.onGhostDefeated.RemoveListener(HandleGhostDefeated);
            }
        }

        private void HandleBatPickedUp(BatWeapon bat)
        {
            if (!encounterStarted)
            {
                StartEncounter();
            }
        }

        /// <summary>
        /// Manually or automatically trigger the encounter sequence.
        /// </summary>
        public void StartEncounter()
        {
            if (encounterStarted) return;
            encounterStarted = true;

            onEncounterStarted?.Invoke();
            sequenceCoroutine = StartCoroutine(EncounterTimelineRoutine());
        }

        private IEnumerator EncounterTimelineRoutine()
        {
            // T=0s to T=3.5s : False sense of security
            yield return new WaitForSeconds(delayToHint1);

            // T=3.5s : Hint 1 (Faint whisper + light burst)
            TriggerHint1();
            yield return new WaitForSeconds(delayToHint2);

            // T=8.0s : Hint 2 (Violent flicker + creak/footsteps)
            TriggerHint2();
            yield return new WaitForSeconds(delayToBlackout);

            // T=11.5s : Blackout
            TriggerBlackout();
            yield return new WaitForSeconds(blackoutDuration);

            // T=12.0s : The Reveal!
            RevealGhost();
        }

        private void TriggerHint1()
        {
            onHint1Triggered?.Invoke();

            if (hint1Clip != null && directorAudioSource != null)
            {
                directorAudioSource.PlayOneShot(hint1Clip, 0.7f);
            }

            foreach (var light in corridorLights)
            {
                if (light != null) light.TriggerBurst();
            }
        }

        private void TriggerHint2()
        {
            onHint2Triggered?.Invoke();

            if (hint2Clip != null && directorAudioSource != null)
            {
                directorAudioSource.PlayOneShot(hint2Clip, 0.9f);
            }

            foreach (var light in corridorLights)
            {
                if (light != null) light.TriggerBurst();
            }
        }

        private void TriggerBlackout()
        {
            onBlackoutStarted?.Invoke();

            foreach (var light in corridorLights)
            {
                if (light != null)
                {
                    light.ForceOff(blackoutDuration);
                }
            }
        }

        private void RevealGhost()
        {
            // Restore lights
            foreach (var light in corridorLights)
            {
                if (light != null)
                {
                    light.SetFlickerEnabled(true);
                }
            }

            // Play jumpscare sting
            if (revealJumpscareSting != null && directorAudioSource != null)
            {
                directorAudioSource.PlayOneShot(revealJumpscareSting, 1f);
            }

            // Spawn & reveal ghost
            if (ghost != null)
            {
                Vector3 spawnPos = ghostSpawnPoint != null ? ghostSpawnPoint.position : ghost.transform.position;
                Quaternion spawnRot = ghostSpawnPoint != null ? ghostSpawnPoint.rotation : ghost.transform.rotation;
                ghost.SpawnAndReveal(spawnPos, spawnRot);
            }

            onGhostRevealed?.Invoke();
        }

        private void HandleGhostDefeated()
        {
            // Ambient calm restored
            foreach (var light in corridorLights)
            {
                if (light != null)
                {
                    light.SetFlickerEnabled(false); // restore steady light
                }
            }

            onEncounterCompleted?.Invoke();
        }
    }
}
