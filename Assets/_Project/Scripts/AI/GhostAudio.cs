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

        private Coroutine whisperRoutine;


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