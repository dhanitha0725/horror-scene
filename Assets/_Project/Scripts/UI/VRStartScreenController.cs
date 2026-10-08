using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace HorrorGame.UI
{
    /// <summary>
    /// Adaptive World-Space VR Start Screen and Scenario Briefing Controller.
    /// Automatically frames in front of the active XR Camera on startup regardless
    /// of player spawn position, pauses locomotion during reading, and smoothly
    /// transitions into gameplay upon pressing Play.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class VRStartScreenController : MonoBehaviour
    {
        [Header("Framing & Placement")]
        [Tooltip("The camera representing the player's eyes. If null, automatically resolves Camera.main.")]
        [SerializeField] private Camera playerCamera;
        [Tooltip("Distance in meters directly in front of the player's gaze.")]
        [SerializeField, Range(1.0f, 4.0f)] private float distanceFromPlayer = 2.0f;
        [Tooltip("Vertical height offset relative to player eye level.")]
        [SerializeField, Range(-1.0f, 1.0f)] private float heightOffset = -0.05f;
        [Tooltip("Whether to re-align directly in front of the player every time the canvas is enabled.")]
        [SerializeField] private bool autoPositionOnEnable = true;

        [Header("Transition Settings")]
        [Tooltip("Duration in seconds for the smooth canvas fade out when starting.")]
        [SerializeField, Range(0.2f, 2.0f)] private float fadeOutDuration = 0.75f;
        [Tooltip("Audio clip played immediately upon clicking Play Now.")]
        [SerializeField] private AudioClip playStartSound;
        [SerializeField] private AudioSource audioSource;

        [Header("Background Music")]
        [Tooltip("Background music track played in a loop while the start screen is open.")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private AudioSource musicAudioSource;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;

        [Header("UI Panels")]
        [SerializeField] private GameObject overviewPanel;
        [SerializeField] private GameObject controlsInfoPanel;
        [SerializeField] private Button playButton;
        [SerializeField] private Button infoToggleButton;
        [SerializeField] private TextMeshProUGUI infoButtonText;

        [Header("Player Control Lock (Optional)")]
        [Tooltip("If true, freezes CharacterController movement until Play is clicked.")]
        [SerializeField] private bool lockLocomotionUntilPlay = true;

        private CanvasGroup canvasGroup;
        private CharacterController playerCharacterController;
        private bool isStarting = false;
        private bool isShowingControls = false;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f; // 2D menu sound for clarity
                }
            }

            SetupMusicAudioSource();

            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);

            if (infoToggleButton != null)
                infoToggleButton.onClick.AddListener(ToggleInfoPanel);
        }

        private void SetupMusicAudioSource()
        {
            if (musicAudioSource == null)
            {
                musicAudioSource = gameObject.AddComponent<AudioSource>();
            }

            musicAudioSource.playOnAwake = false;
            musicAudioSource.loop = true;
            musicAudioSource.spatialBlend = 0f; // 2D audio for clear soundtrack
            musicAudioSource.volume = musicVolume;

            if (backgroundMusic != null)
            {
                musicAudioSource.clip = backgroundMusic;
            }
        }

        private void PlayBackgroundMusic()
        {
            if (musicAudioSource == null || backgroundMusic == null) return;
            if (musicAudioSource.isPlaying) return;
            musicAudioSource.volume = musicVolume;
            if (backgroundMusic.loadState == AudioDataLoadState.Loaded)
            {
                musicAudioSource.Play();
                return;
            }
            // Clip uses preloadAudioData=false, so trigger the load explicitly,
            // then play once ready (covers first import / fresh scene load).
            if (backgroundMusic.loadState == AudioDataLoadState.Unloaded)
            {
                backgroundMusic.LoadAudioData();
            }
            StartCoroutine(PlayMusicWhenLoaded());
        }

        private IEnumerator PlayMusicWhenLoaded()
        {
            // Wait until the clip is ready, but don't block the screen if it fails.
            float timeout = 10f;
            while (backgroundMusic != null && backgroundMusic.loadState == AudioDataLoadState.Loading && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (isStarting) yield break;
            if (musicAudioSource != null && backgroundMusic != null
                && backgroundMusic.loadState == AudioDataLoadState.Loaded
                && !musicAudioSource.isPlaying && gameObject.activeSelf)
            {
                musicAudioSource.volume = musicVolume;
                musicAudioSource.Play();
            }
        }

        private void StopBackgroundMusic()
        {
            if (musicAudioSource == null) return;
            if (musicAudioSource.isPlaying)
            {
                musicAudioSource.Stop();
            }
        }

        private void OnDisable()
        {
            StopBackgroundMusic();
        }

        private void Start()
        {
            ResolvePlayerReferences();
            PositionInFrontOfPlayer();
            PlayBackgroundMusic();

            if (lockLocomotionUntilPlay && playerCharacterController != null)
            {
                playerCharacterController.enabled = false;
            }

            // Ensure start screen has full alpha and blocks raycasts initially
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        private void OnEnable()
        {
            if (autoPositionOnEnable)
            {
                ResolvePlayerReferences();
                PositionInFrontOfPlayer();
            }
        }

        private void Update()
        {
            if (isStarting) return;

#if ENABLE_INPUT_SYSTEM
            // New Input System path (project uses activeInputHandler: Input System only)
            var kb = Keyboard.current;
            if (kb != null)
            {
                // Desktop & XR Device Simulator fallback controls
                if (kb[Key.Enter].wasPressedThisFrame || kb[Key.NumpadEnter].wasPressedThisFrame || kb[Key.Space].wasPressedThisFrame)
                {
                    OnPlayClicked();
                }

                // Quick recenter hotkey for development/testing
                if (kb[Key.R].wasPressedThisFrame || kb[Key.F12].wasPressedThisFrame)
                {
                    PositionInFrontOfPlayer();
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            // Legacy Input Manager fallback (only compiled when old Input is enabled)
            // Desktop & XR Device Simulator fallback controls
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                OnPlayClicked();
            }

            // Quick recenter hotkey for development/testing
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.F12))
            {
                PositionInFrontOfPlayer();
            }
#endif
        }

        /// <summary>
        /// Dynamically positions and rotates the start screen directly in front
        /// of the player camera at eye level on the horizontal plane.
        /// </summary>
        [ContextMenu("Center In Front of Player")]
        public void PositionInFrontOfPlayer()
        {
            ResolvePlayerReferences();
            if (playerCamera == null)
            {
                Debug.LogWarning("[VRStartScreen] Player camera not found. Cannot position start screen.");
                return;
            }

            Vector3 headPos = playerCamera.transform.position;
            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0f; // Keep level with the floor to avoid disorienting tilt

            if (forward.sqrMagnitude < 0.001f)
            {
                forward = playerCamera.transform.up;
                forward.y = 0f;
            }

            forward.Normalize();

            Vector3 targetPosition = headPos + (forward * distanceFromPlayer) + (Vector3.up * heightOffset);
            Quaternion targetRotation = Quaternion.LookRotation(forward, Vector3.up);

            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        private void ResolvePlayerReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (playerCharacterController == null)
            {
                playerCharacterController = FindFirstObjectByType<CharacterController>();
            }
        }

        public void OnPlayClicked()
        {
            if (isStarting) return;
            StartCoroutine(StartGameRoutine());
        }

        private IEnumerator StartGameRoutine()
        {
            isStarting = true;

            if (playButton != null)
                playButton.interactable = false;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            // Play audio feedback
            if (playStartSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(playStartSound);
            }

            // Smooth fade out
            float elapsed = 0f;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / fadeOutDuration);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                }
                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            // Restore player locomotion
            if (lockLocomotionUntilPlay && playerCharacterController != null)
            {
                playerCharacterController.enabled = true;
            }

            // Stop the opening-screen soundtrack now that the screen is closed
            StopBackgroundMusic();

            // Deactivate Start Screen GameObject
            gameObject.SetActive(false);
        }

        public void ToggleInfoPanel()
        {
            isShowingControls = !isShowingControls;

            if (controlsInfoPanel != null)
                controlsInfoPanel.SetActive(isShowingControls);

            if (overviewPanel != null)
                overviewPanel.SetActive(!isShowingControls);

            if (infoButtonText != null)
            {
                infoButtonText.text = isShowingControls ? "BRIEFING" : "CONTROLS";
            }
        }
    }
}
