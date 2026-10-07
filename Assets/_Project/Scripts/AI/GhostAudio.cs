using System.Collections;
using UnityEngine;

namespace HorrorGame.AI
{
    /// <summary>
    /// Controls 3D spatial horror audio for the Ghost.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class GhostAudio : MonoBehaviour
    {
        [Header("Audio Clips")]

        [Tooltip("Ghost ambient whisper/breathing sounds.")]
        [SerializeField] private AudioClip[] whisperClips;

        [Tooltip("Sound played once when ghost appears.")]
        [SerializeField] private AudioClip revealScreamClip;

        [Tooltip("Sounds played when ghost starts chasing.")]
        [SerializeField] private AudioClip[] chaseScreamClips;

        [Tooltip("Sounds played when ghost is hit.")]
        [SerializeField] private AudioClip[] painClips;

        [Tooltip("Sound played when ghost dies.")]
        [SerializeField] private AudioClip deathClip;


        [Header("Ambient Loop (gost.wav)")]
        [Tooltip("Looping ghost drone. If empty, revealScreamClip is used so the existing scene keeps working.")]
        [SerializeField] private AudioClip ambientLoopClip;

        [Tooltip("Beyond this distance the loop is barely audible (Unity rolloff + this curve).")]
        [SerializeField, Min(0.5f)] private float silentDistance = 18f;

        [Tooltip("At or closer than this the loop is at full volume.")]
        [SerializeField, Min(0.1f)] private float fullVolumeDistance = 2f;

        [SerializeField, Range(0f, 1f)] private float ambientMaxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float ambientMinVolume = 0.05f;

        [Tooltip("Extra pitch when the ghost is very close (tension rise).")]
        [SerializeField, Range(0f, 0.3f)] private float nearPitchBoost = 0.08f;

        [Tooltip("Seconds of silence between each repeat of the drone, so it pulses instead of droning.")]
        [SerializeField, Min(0f)] private float loopGapSeconds = 1.7f;


        [Header("Volume & 3D Settings")]
        [SerializeField, Range(0f, 1f)]
        private float masterVolume = 1f;

        [SerializeField]
        private Vector2 pitchVariation =
            new Vector2(0.92f, 1.08f);

        [SerializeField, Min(0.5f)]
        private float min3DDistance = 1f;

        [SerializeField, Min(1f)]
        private float max3DDistance = 12f;


        [Header("Whisper Timing")]

        [Tooltip("Minimum seconds before next ghost sound.")]
        [SerializeField]
        private float whisperIntervalMin = 2f;

        [Tooltip("Maximum seconds before next ghost sound.")]
        [SerializeField]
        private float whisperIntervalMax = 4f;


        private AudioSource primarySource;
        private AudioSource whisperSource;
        private AudioSource ambientSource;

        private Coroutine whisperRoutine;
        private Coroutine ambientRoutine;
        private Transform listenerTransform;


        private void Awake()
        {
            // Main Audio Source
            primarySource = GetComponent<AudioSource>();

            SetupSource(primarySource);

            // Second Audio Source for whisper sounds
            whisperSource = gameObject.AddComponent<AudioSource>();

            SetupSource(whisperSource);

            // IMPORTANT:
            // We control repetition ourselves.
            whisperSource.loop = false;

            // Third Audio Source for the pulsing ghost drone
            ambientSource = gameObject.AddComponent<AudioSource>();

            SetupSource(ambientSource);

            // We handle repetition ourselves so there can be a gap
            // between repeats instead of a seamless drone.
            ambientSource.loop = false;
        }


        private void SetupSource(AudioSource source)
        {
            source.playOnAwake = false;

            // Full 3D audio
            source.spatialBlend = 1f;

            source.rolloffMode =
                AudioRolloffMode.Logarithmic;

            source.minDistance =
                min3DDistance;

            source.maxDistance =
                max3DDistance;

            // A ghost drone should not smear or pitch-shift as it moves.
            source.dopplerLevel = 0f;

            source.spread = 0f;
        }


        private void Update()
        {
            // Keep tracking distance even during the silent gap so the
            // next repeat starts at the correct volume and pitch.
            if (ambientSource == null ||
                ambientRoutine == null)
            {
                return;
            }

            ApplyDistanceVolume();
        }


        private void ApplyDistanceVolume()
        {
            if (listenerTransform == null &&
                Camera.main != null)
            {
                listenerTransform = Camera.main.transform;
            }

            if (listenerTransform == null)
                return;

            Vector3 delta = listenerTransform.position - transform.position;

            // Horizontal distance only: vertical offset should not
            // change how threatening the sound feels.
            delta.y = 0f;

            float distance = delta.magnitude;
            float closeness = Mathf.InverseLerp(
                silentDistance,
                Mathf.Max(0.1f, fullVolumeDistance),
                distance
            );

            ambientSource.volume =
                Mathf.Lerp(ambientMinVolume, ambientMaxVolume, closeness)
                * masterVolume;

            ambientSource.pitch = 1f + (nearPitchBoost * closeness);
        }


        /// <summary>
        /// Starts the ghost drone. It repeats with a silent gap between hits
        /// rather than looping seamlessly. Falls back to the reveal clip so
        /// existing scene setups keep producing sound without re-wiring.
        /// </summary>
        public void StartAmbientLoop()
        {
            if (ambientRoutine != null)
                return;

            AudioClip clip =
                ambientLoopClip != null
                    ? ambientLoopClip
                    : revealScreamClip;

            if (clip == null)
                return;

            ambientSource.clip = clip;
            ambientRoutine = StartCoroutine(AmbientPulseRoutine());
        }


        /// <summary>
        /// Plays the drone, waits for it to finish, then holds silence for
        /// loopGapSeconds before the next hit.
        /// </summary>
        private IEnumerator AmbientPulseRoutine()
        {
            while (true)
            {
                ApplyDistanceVolume();

                ambientSource.Play();

                // Wait for the clip to finish (pitch-shifted, so measure it).
                yield return new WaitForSeconds(
                    ambientSource.clip.length
                    / Mathf.Max(0.01f, Mathf.Abs(ambientSource.pitch))
                );

                if (loopGapSeconds > 0f)
                {
                    yield return new WaitForSeconds(loopGapSeconds);
                }
            }
        }


        public void StopAmbientLoop()
        {
            if (ambientRoutine != null)
            {
                StopCoroutine(ambientRoutine);

                ambientRoutine = null;
            }

            if (ambientSource != null &&
                ambientSource.isPlaying)
            {
                ambientSource.Stop();
            }
        }


        // Called when ghost appears
        public void PlayWhisper(bool loop = false)
        {
            if (whisperClips == null ||
                whisperClips.Length == 0)
            {
                return;
            }

            // GhostController currently calls:
            // PlayWhisper(loop: true)
            if (loop)
            {
                if (whisperRoutine == null)
                {
                    whisperRoutine =
                        StartCoroutine(
                            WhisperIntervalRoutine()
                        );
                }
            }
            else
            {
                PlayRandomOneShot(
                    whisperClips,
                    0.7f * masterVolume
                );
            }
        }


        private IEnumerator WhisperIntervalRoutine()
        {
            while (true)
            {
                // Choose a random whisper
                AudioClip clip =
                    whisperClips[
                        Random.Range(
                            0,
                            whisperClips.Length
                        )
                    ];

                if (clip != null)
                {
                    whisperSource.clip = clip;

                    whisperSource.volume =
                        0.7f * masterVolume;

                    whisperSource.pitch =
                        Random.Range(
                            pitchVariation.x,
                            pitchVariation.y
                        );

                    whisperSource.Play();

                    // Wait until current sound finishes
                    yield return new WaitForSeconds(
                        clip.length
                    );
                }

                // Random silence:
                // 5 to 10 seconds
                float waitTime =
                    Random.Range(
                        whisperIntervalMin,
                        whisperIntervalMax
                    );

                yield return new WaitForSeconds(
                    waitTime
                );
            }
        }


        public void StopWhisperLoop()
        {
            if (whisperRoutine != null)
            {
                StopCoroutine(whisperRoutine);

                whisperRoutine = null;
            }

            if (whisperSource != null)
            {
                whisperSource.Stop();
            }

            StopAmbientLoop();
        }


        public void PlayRevealScream()
        {
            if (revealScreamClip == null)
                return;

            primarySource.pitch = 1f;

            primarySource.PlayOneShot(
                revealScreamClip,
                1f * masterVolume
            );

            // Keep the drone running underneath the reveal.
            StartAmbientLoop();
        }


        public void PlayChaseScream()
        {
            PlayRandomOneShot(
                chaseScreamClips,
                0.95f * masterVolume
            );
        }


        public void PlayPainSound()
        {
            PlayRandomOneShot(
                painClips,
                1f * masterVolume
            );
        }


        public void PlayDeathSound()
        {
            StopWhisperLoop();

            if (deathClip == null)
                return;

            primarySource.pitch =
                Random.Range(
                    0.95f,
                    1.05f
                );

            primarySource.PlayOneShot(
                deathClip,
                1f * masterVolume
            );
        }


        private void PlayRandomOneShot(
            AudioClip[] clips,
            float volume
        )
        {
            if (clips == null ||
                clips.Length == 0)
            {
                return;
            }

            AudioClip clip =
                clips[
                    Random.Range(
                        0,
                        clips.Length
                    )
                ];

            if (clip == null)
                return;

            primarySource.pitch =
                Random.Range(
                    pitchVariation.x,
                    pitchVariation.y
                );

            primarySource.PlayOneShot(
                clip,
                volume
            );
        }


        private void OnDisable()
        {
            StopWhisperLoop();
        }
    }
}