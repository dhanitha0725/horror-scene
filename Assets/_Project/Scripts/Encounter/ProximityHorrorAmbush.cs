using UnityEngine;
using HorrorGame.AI;

namespace HorrorGame.Encounter
{
    /// <summary>
    /// Starts a spatial warning loop as the player nears a hidden ghost and launches
    /// the director sequence only after the player reaches the danger zone.
    /// </summary>
    [DisallowMultipleComponent]
    public class ProximityHorrorAmbush : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GhostController ghost;
        [SerializeField] private BatEncounterDirector encounterDirector;
        [SerializeField] private AudioSource warningAudio;
        [SerializeField] private AudioClip warningClip;

        [Header("Distance Pacing")]
        [SerializeField, Min(1f)] private float warningDistance = 13f;
        [SerializeField, Min(0.5f)] private float revealDistance = 6f;
        [SerializeField, Range(0f, 1f)] private float nearWarningVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float farWarningVolume = 0.06f;

        private Transform player;
        private bool encounterStarted;

private void Awake()
        {
            if (warningAudio == null)
                warningAudio = GetComponent<AudioSource>();

            if (warningAudio != null)
            {
                warningAudio.playOnAwake = false;
                warningAudio.loop = true;
                warningAudio.spatialBlend = 1f;
                warningAudio.rolloffMode = AudioRolloffMode.Linear;
                warningAudio.minDistance = 2f;
                warningAudio.maxDistance = warningDistance + 6f;
                warningAudio.dopplerLevel = 0f;
            }
        }

        private void Start()
        {
            // The director used to start from bat pickup. Keep it dormant until the
            // player reaches this corridor section.
            if (encounterDirector != null)
                encounterDirector.gameObject.SetActive(false);
        }

private void Update()
        {
            if (ghost == null)
                return;

            if (player == null && Camera.main != null)
                player = Camera.main.transform;
            if (player == null)
                return;

            if (warningAudio != null)
                warningAudio.transform.position = ghost.transform.position;

            Vector3 delta = player.position - ghost.transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;

            // Keep this source attached to the chasing ghost. The manual volume
            // curve and Unity's 3D rolloff both increase the threat as she closes in.
            if (warningAudio != null && warningAudio.isPlaying)
            {
                float closeness = Mathf.InverseLerp(warningDistance, 0.75f, distance);
                warningAudio.volume = Mathf.Lerp(farWarningVolume, nearWarningVolume, closeness);
            }

            if (encounterStarted)
            {
                if (ghost.CurrentState == GhostState.Dead)
                    StopWarning();
                return;
            }

            if (distance <= warningDistance)
                StartWarning();

            if (distance <= revealDistance)
            {
                encounterStarted = true;
                if (encounterDirector != null)
                {
                    encounterDirector.gameObject.SetActive(true);
                    encounterDirector.StartEncounter();
                }
                else
                {
                    ghost.SpawnAndReveal(ghost.transform.position, ghost.transform.rotation);
                }
            }
        }

        private void StartWarning()
        {
            if (warningAudio == null || warningAudio.isPlaying || warningClip == null)
                return;

            warningAudio.clip = warningClip;
            warningAudio.volume = farWarningVolume;
            warningAudio.Play();
        }

        public void StopWarning()
        {
            if (warningAudio != null)
                warningAudio.Stop();
        }

        private void OnDisable()
        {
            StopWarning();
        }
    }
}