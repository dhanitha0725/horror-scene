using System.Collections;
using UnityEngine;
using HorrorGame.Combat;

namespace HorrorGame.AI
{
    /// <summary>
    /// Adds a physical-looking collapse and a synthesized death shriek when the
    /// ghost has taken the final bat hit.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GhostDamageReceiver))]
    public class GhostDeathCollapse : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float collapseDuration = 0.8f;
        [SerializeField] private float collapseAngle = 82f;
        [SerializeField] private AudioSource proximityWarningAudio;

        [Header("Death Scream")]
        [Tooltip("Recorded horror scream played on death. If empty, a synthesized shriek is used.")]
        [SerializeField] private AudioClip deathScreamClip;
        [SerializeField, Range(0f, 1f)] private float deathScreamVolume = 1f;
        [Tooltip("Random pitch range for the death scream.")]
        [SerializeField] private Vector2 deathScreamPitch = new Vector2(0.95f, 1.05f);

        [Header("Echo & Fade")]
        [Tooltip("Add repeating echoes after the scream.")]
        [SerializeField] private bool addEcho = true;
        [Tooltip("Seconds between echoes.")]
        [SerializeField, Range(0.05f, 1f)] private float echoDelay = 0.32f;
        [Tooltip("How much quieter each echo is (0.5 = half).")]
        [SerializeField, Range(0f, 0.95f)] private float echoDecay = 0.5f;
        [SerializeField, Range(1, 12)] private int echoCount = 6;
        [Tooltip("Extra seconds of echo tail after the scream ends; fades to silence.")]
        [SerializeField, Range(0.5f, 8f)] private float tailSeconds = 3f;
        [Tooltip("Room reverb on top of the echoes.")]
        [SerializeField] private bool addReverb = true;
        [SerializeField] private AudioReverbPreset reverbPreset = AudioReverbPreset.StoneCorridor;

        private GhostDamageReceiver damageReceiver;
        private AudioSource screamSource;
        private AudioClip screamClip;
        private bool collapseStarted;

        private void Awake()
        {
            damageReceiver = GetComponent<GhostDamageReceiver>();

            screamSource = gameObject.AddComponent<AudioSource>();
            screamSource.playOnAwake = false;
            screamSource.spatialBlend = 1f;
            screamSource.rolloffMode = AudioRolloffMode.Logarithmic;
            screamSource.minDistance = 0.8f;
            screamSource.maxDistance = 28f;
            screamSource.dopplerLevel = 0f;
            screamClip = deathScreamClip != null ? BuildEchoClip(deathScreamClip) : CreateScreamClip();
        }

        private void OnEnable()
        {
            damageReceiver.onDefeated.AddListener(BeginCollapse);
        }

        private void OnDisable()
        {
            if (damageReceiver != null)
                damageReceiver.onDefeated.RemoveListener(BeginCollapse);
        }

private void BeginCollapse()
        {
            // The collapse is reserved for the third accepted weapon hit.
            if (collapseStarted || damageReceiver == null || !damageReceiver.IsDead || damageReceiver.CurrentHealth > 0)
                return;

            collapseStarted = true;
            if (proximityWarningAudio != null)
                proximityWarningAudio.Stop();

            // This is the sole fall sound: a 3D death scream, not an impact thud.
            if (screamClip != null)
            {
                PlayDetachedScream();
            }

            StartCoroutine(CollapseRoutine());
        }

        /// <summary>
        /// Plays the scream from a separate object that is NOT destroyed with the ghost,
        /// so the echo tail can fade out naturally.
        /// </summary>
        private void PlayDetachedScream()
        {
            var fx = new GameObject("GhostDeathScream_FX");
            fx.transform.position = transform.position + Vector3.up * 1.4f;
            var src = fx.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 0.8f;
            src.maxDistance = 30f;
            src.dopplerLevel = 0f;
            src.clip = screamClip;
            src.volume = deathScreamVolume;
            src.pitch = Random.Range(deathScreamPitch.x, Mathf.Max(deathScreamPitch.x, deathScreamPitch.y));
            if (addReverb)
            {
                var reverb = fx.AddComponent<AudioReverbFilter>();
                reverb.reverbPreset = reverbPreset;
            }
            src.Play();
            Destroy(fx, screamClip.length / Mathf.Max(0.1f, src.pitch) + 0.5f);
        }

        /// <summary>
        /// Bakes echoes (each one quieter and duller) plus a silent-to-fade tail into a new clip,
        /// so the effect never gets cut off when the sound ends.
        /// </summary>
        private AudioClip BuildEchoClip(AudioClip source)
        {
            if (source == null) return null;
            if (source.loadState != AudioDataLoadState.Loaded) source.LoadAudioData();
            int ch = source.channels, freq = source.frequency, n = source.samples;
            var dry = new float[n * ch];
            if (!source.GetData(dry, 0))
            {
                Debug.LogWarning("[GhostDeathCollapse] Could not read scream samples; set its Load Type to Decompress On Load. Using clip without echo.", this);
                return source;
            }

            int tail = Mathf.CeilToInt(tailSeconds * freq);
            int total = n + tail;
            var buf = new float[total * ch];
            System.Array.Copy(dry, buf, dry.Length);

            if (addEcho)
            {
                int delay = Mathf.Max(1, Mathf.RoundToInt(echoDelay * freq));
                var layer = (float[])dry.Clone();
                float gain = 1f;
                for (int k = 1; k <= echoCount; k++)
                {
                    gain *= echoDecay;
                    // each repeat gets duller (one-pole low-pass), like sound bouncing off far walls
                    for (int c = 0; c < ch; c++)
                    {
                        float y = 0f;
                        for (int i = 0; i < n; i++) { int idx = i * ch + c; y += 0.45f * (layer[idx] - y); layer[idx] = y; }
                    }
                    int offset = delay * k;
                    for (int i = 0; i < n; i++)
                    {
                        int dst = i + offset;
                        if (dst >= total) break;
                        for (int c = 0; c < ch; c++) buf[dst * ch + c] += layer[i * ch + c] * gain;
                    }
                }
            }

            // smooth fade to silence over the tail
            int fade = Mathf.Min(total, Mathf.RoundToInt(Mathf.Max(0.5f, tailSeconds * 0.9f) * freq));
            int fadeStart = total - fade;
            for (int i = fadeStart; i < total; i++)
            {
                float t = (total - i) / (float)fade;
                float f = t * t;
                for (int c = 0; c < ch; c++) buf[i * ch + c] *= f;
            }

            // prevent clipping from stacked echoes
            float peak = 0f;
            for (int i = 0; i < buf.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
            if (peak > 0.98f) { float s = 0.98f / peak; for (int i = 0; i < buf.Length; i++) buf[i] *= s; }

            var clip = AudioClip.Create(source.name + "_Echo", total, ch, freq, false);
            clip.SetData(buf, 0);
            return clip;
        }

        private IEnumerator CollapseRoutine()
        {
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
                agent.enabled = false;

            Quaternion start = transform.rotation;
            Quaternion end = Quaternion.AngleAxis(collapseAngle, transform.right) * start;
            float elapsed = 0f;

            while (elapsed < collapseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / collapseDuration);
                transform.rotation = Quaternion.Slerp(start, end, t);
                yield return null;
            }

            transform.rotation = end;
        }

        private static AudioClip CreateScreamClip()
        {
            const int sampleRate = 44100;
            const float duration = 2.2f;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float time = i / (float)sampleRate;
                float attack = Mathf.Clamp01(time / 0.08f);
                float release = Mathf.Clamp01((duration - time) / 0.45f);
                float envelope = attack * release;
                float pitchT = Mathf.SmoothStep(0f, 1f, time / duration);
                float frequency = Mathf.Lerp(320f, 1750f, pitchT) + Mathf.Sin(time * 32f) * 55f;
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * time);
                float harmonic = Mathf.Sin(2f * Mathf.PI * frequency * 2.03f * time) * 0.38f;
                float harshness = Mathf.Sin(time * 12347f) * Mathf.Sin(time * 6121f) * 0.28f;
                samples[i] = Mathf.Clamp((tone + harmonic + harshness) * envelope * 0.72f, -1f, 1f);
            }

            var clip = AudioClip.Create("Ghost_Death_Shriek_Runtime", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}