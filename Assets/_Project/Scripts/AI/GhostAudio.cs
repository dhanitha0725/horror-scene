using UnityEngine;

namespace HorrorGame.AI
{
    /// <summary>
    /// Controls 3D spatial horror audio for the Ghost (whispers, chase screams, pain, death).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class GhostAudio : MonoBehaviour
    {
        [Header("Audio Clips")]
        [Tooltip("Looping or triggered ambient whisper/breathing.")]
        [SerializeField] private AudioClip[] whisperClips;
        [Tooltip("Loud sting/scream when revealed.")]
        [SerializeField] private AudioClip revealScreamClip;
        [Tooltip("Screams or aggressive sounds played during chase.")]
        [SerializeField] private AudioClip[] chaseScreamClips;
        [Tooltip("Sounds played when struck by the bat.")]
        [SerializeField] private AudioClip[] painClips;
        [Tooltip("Final shriek when defeated/banished.")]
        [SerializeField] private AudioClip deathClip;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField] private Vector2 pitchVariation = new Vector2(0.92f, 1.08f);
        [SerializeField, Min(0.5f)] private float min3DDistance = 1f;
        [SerializeField, Min(1f)] private float max3DDistance = 25f;

        private AudioSource primarySource;
        private AudioSource loopSource;

        private void Awake()
        {
            primarySource = GetComponent<AudioSource>();
            SetupSource(primarySource);

            // Add a secondary audio source for overlapping screams and background whispers
            loopSource = gameObject.AddComponent<AudioSource>();
            SetupSource(loopSource);
            loopSource.loop = true;
        }

        private void SetupSource(AudioSource src)
        {
            src.spatialBlend = 1f; // Full 3D
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = min3DDistance;
            src.maxDistance = max3DDistance;
            src.playOnAwake = false;
        }

        public void PlayWhisper(bool loop = false)
        {
            if (whisperClips != null && whisperClips.Length > 0)
            {
                AudioClip clip = whisperClips[Random.Range(0, whisperClips.Length)];
                if (clip == null) return;

                if (loop)
                {
                    loopSource.clip = clip;
                    loopSource.volume = 0.6f * masterVolume;
                    loopSource.pitch = 1f;
                    loopSource.Play();
                }
                else
                {
                    PlayRandomOneShot(whisperClips, 0.7f * masterVolume);
                }
            }
        }

        public void StopWhisperLoop()
        {
            if (loopSource != null && loopSource.isPlaying)
            {
                loopSource.Stop();
            }
        }

        public void PlayRevealScream()
        {
            if (revealScreamClip != null)
            {
                primarySource.pitch = 1f;
                primarySource.PlayOneShot(revealScreamClip, 1f * masterVolume);
            }
        }

        public void PlayChaseScream()
        {
            PlayRandomOneShot(chaseScreamClips, 0.95f * masterVolume);
        }

        public void PlayPainSound()
        {
            PlayRandomOneShot(painClips, 1f * masterVolume);
        }

        public void PlayDeathSound()
        {
            StopWhisperLoop();
            if (deathClip != null)
            {
                primarySource.pitch = Random.Range(0.95f, 1.05f);
                primarySource.PlayOneShot(deathClip, 1f * masterVolume);
            }
        }

        private void PlayRandomOneShot(AudioClip[] clips, float volume)
        {
            if (clips == null || clips.Length == 0) return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null)
            {
                primarySource.pitch = Random.Range(pitchVariation.x, pitchVariation.y);
                primarySource.PlayOneShot(clip, volume);
            }
        }
    }
}
