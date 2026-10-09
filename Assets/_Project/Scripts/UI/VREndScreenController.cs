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
                if (kb[Key.Enter].wasPressedThisFrame || kb[Key.NumpadEnter].wasPressedThisFrame || kb[Key.Space].wasPressedThisFrame)
                    OnPlayAgainClicked();
                if (kb[Key.R].wasPressedThisFrame) PositionInFrontOfPlayer();
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                OnPlayAgainClicked();
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
            if (isRestarting) return;
            StartCoroutine(RestartRoutine());
        }

        private IEnumerator RestartRoutine()
        {
            isRestarting = true;
            if (playAgainButton != null) playAgainButton.interactable = false;
            if (canvasGroup != null) canvasGroup.interactable = false;
            if (clickSound != null && audioSource != null) audioSource.PlayOneShot(clickSound);

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
    }
}
