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
            screamClip = CreateScreamClip();
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
                screamSource.PlayOneShot(screamClip, 1f);

            StartCoroutine(CollapseRoutine());
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