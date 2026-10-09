using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HorrorGame.UI
{
    /// <summary>
    /// World-space VR end screen. Stays hidden until the bones are burned at the BoneFire,
    /// waits for the ghost's final scream to finish, then fades in in front of the player.
    /// "Play Again" reloads the scene and skips the start screen, so the player
    /// begins again at the spawn position with everything reset.
    /// "Credits" swaps the result text for the credits page; "Back" returns to the result.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class VREndScreenController : MonoBehaviour
    {
        [Header("Trigger")]
        [Tooltip("The fire where the bones are burned. When it reports IsBurned, the end screen is scheduled.")]
        [SerializeField] private BoneFire boneFire;
        [Tooltip("Seconds to wait after the bones start burning (covers the final scream and its echo).")]
        [SerializeField, Min(0f)] private float showDelay = 8f;

        [Header("Framing & Placement")]
        [SerializeField] private Camera playerCamera;
        [SerializeField, Range(1.0f, 4.0f)] private float distanceFromPlayer = 2.0f;
        [SerializeField, Range(-1.0f, 1.0f)] private float heightOffset = -0.05f;

        [Header("UI")]
        [Tooltip("Parent of every visual element of the end screen (hidden until shown).")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private Button playAgainButton;
        [SerializeField, Range(0.2f, 3f)] private float fadeInDuration = 1.2f;

        [Header("Credits")]
        [Tooltip("Result page: heading, message, Play Again and Credits buttons.")]
        [SerializeField] private GameObject resultPanel;
        [Tooltip("Credits page with the Back button (hidden until Credits is clicked).")]
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button backButton;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip showSound;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioSource audioSource;

        [Header("Player")]
        [Tooltip("Freeze CharacterController movement while the end screen is open.")]
        [SerializeField] private bool lockLocomotion = true;

        private CanvasGroup canvasGroup;
        private CharacterController playerCharacterController;
        private bool scheduled;
        private bool isShown;
        private bool isRestarting;
        private bool showingCredits;

        public bool IsShown => isShown;

private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }

            if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            if (creditsButton != null) creditsButton.onClick.AddListener(ShowCredits);
            if (backButton != null) backButton.onClick.AddListener(ShowResult);
            Hide();
        }

private void Hide()
        {
            isShown = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (contentRoot != null) contentRoot.SetActive(false);
            SetCreditsVisible(false);
            SetUiRaycastersEnabled(false);
        }

        private void Update()
        {
            if (!scheduled && boneFire != null && boneFire.IsBurned)
            {
                scheduled = true;
                StartCoroutine(ShowAfterDelay(showDelay));
            }

            if (!isShown || isRestarting) return;
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                bool confirm = kb[Key.Enter].wasPressedThisFrame || kb[Key.NumpadEnter].wasPressedThisFrame || kb[Key.Space].wasPressedThisFrame;
                bool back = kb[Key.Escape].wasPressedThisFrame || kb[Key.Backspace].wasPressedThisFrame;
                if (showingCredits) { if (confirm || back) ShowResult(); }
                else
                {
                    if (confirm) OnPlayAgainClicked();
                    else if (kb[Key.C].wasPressedThisFrame) ShowCredits();
                }
                if (kb[Key.R].wasPressedThisFrame) PositionInFrontOfPlayer();
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            bool lConfirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
            bool lBack = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
            if (showingCredits) { if (lConfirm || lBack) ShowResult(); }
            else
            {
                if (lConfirm) OnPlayAgainClicked();
                else if (Input.GetKeyDown(KeyCode.C)) ShowCredits();
            }
            if (Input.GetKeyDown(KeyCode.R)) PositionInFrontOfPlayer();
#endif
        }

        /// <summary>Show the end screen now (also usable from a UnityEvent or for testing).</summary>
        [ContextMenu("Show End Screen Now")]
        public void ShowNow()
        {
            scheduled = true;
            StopAllCoroutines();
            StartCoroutine(ShowAfterDelay(0f));
        }

private IEnumerator ShowAfterDelay(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            ResolvePlayerReferences();
            PositionInFrontOfPlayer();
            if (contentRoot != null) contentRoot.SetActive(true);
            SetCreditsVisible(false);
            if (lockLocomotion && playerCharacterController != null) playerCharacterController.enabled = false;
            if (showSound != null && audioSource != null) audioSource.PlayOneShot(showSound);

            isShown = true;
            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.deltaTime;
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
                yield return null;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            SetUiRaycastersEnabled(true);
        }

        /// <summary>Open the credits page.</summary>
        public void ShowCredits()
        {
            if (isRestarting) return;
            PlayClick();
            SetCreditsVisible(true);
        }

        /// <summary>Return from the credits page to the result page.</summary>
        public void ShowResult()
        {
            if (isRestarting) return;
            PlayClick();
            SetCreditsVisible(false);
        }

        private void SetCreditsVisible(bool visible)
        {
            showingCredits = visible && creditsPanel != null;
            if (creditsPanel != null) creditsPanel.SetActive(showingCredits);
            if (resultPanel != null) resultPanel.SetActive(!showingCredits);
        }

        private void PlayClick()
        {
            if (clickSound != null && audioSource != null) audioSource.PlayOneShot(clickSound);
        }

        [ContextMenu("Center In Front of Player")]
        public void PositionInFrontOfPlayer()
        {
            ResolvePlayerReferences();
            if (playerCamera == null) return;

            Vector3 headPos = playerCamera.transform.position;
            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) { forward = playerCamera.transform.up; forward.y = 0f; }
            forward.Normalize();

            transform.position = headPos + forward * distanceFromPlayer + Vector3.up * heightOffset;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        private void ResolvePlayerReferences()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCharacterController == null) playerCharacterController = FindAnyObjectByType<CharacterController>();
        }

        public void OnPlayAgainClicked()
        {
            if (isRestarting || showingCredits) return;
            StartCoroutine(RestartRoutine());
        }

        private IEnumerator RestartRoutine()
        {
            isRestarting = true;
            if (playAgainButton != null) playAgainButton.interactable = false;
            if (canvasGroup != null) canvasGroup.interactable = false;
            PlayClick();

            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                if (canvasGroup != null) canvasGroup.alpha = 1f - Mathf.Clamp01(t / 0.5f);
                yield return null;
            }

            // Reload the level and go straight to gameplay at the spawn position.
            VRStartScreenController.SkipStartScreenOnNextLoad = true;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
        }
    

private void SetUiRaycastersEnabled(bool enabled)
        {
            var tracked = GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            if (tracked != null) tracked.enabled = enabled;

            var graphic = GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (graphic != null) graphic.enabled = enabled;
        }
}
}
