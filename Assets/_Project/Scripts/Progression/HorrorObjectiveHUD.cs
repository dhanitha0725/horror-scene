using UnityEngine;

namespace HorrorGame.Progression
{
    /// <summary>
    /// Lightweight, dependency-free objective display for the first playable
    /// slice. It works in the Game view and XR mirror; a diegetic wrist UI can
    /// replace it once the gameplay loop is complete.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class HorrorObjectiveHUD : MonoBehaviour
    {
        private string objective = "Find the bat hanging nearby.";
        private string notification;
        private float notificationEndsAt;
        private int score;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle notificationStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimeHud()
        {
            if (FindAnyObjectByType<HorrorObjectiveHUD>() != null)
                return;

            GameObject hudObject = new GameObject("Horror Objective HUD");
            DontDestroyOnLoad(hudObject);
            hudObject.AddComponent<HorrorObjectiveHUD>();
        }

        private void OnEnable()
        {
            GameProgressManager.ObjectiveChanged += HandleObjectiveChanged;
            GameProgressManager.NotificationRaised += HandleNotification;
            GameProgressManager.StateChanged += HandleStateChanged;
        }

        private void Start()
        {
            if (GameProgressManager.Instance != null)
                HandleStateChanged(GameProgressManager.Instance);
        }

        private void OnDisable()
        {
            GameProgressManager.ObjectiveChanged -= HandleObjectiveChanged;
            GameProgressManager.NotificationRaised -= HandleNotification;
            GameProgressManager.StateChanged -= HandleStateChanged;
        }

        private void HandleObjectiveChanged(string newObjective) => objective = newObjective;

        private void HandleNotification(string message)
        {
            notification = message;
            notificationEndsAt = Time.unscaledTime + 3.5f;
        }

        private void HandleStateChanged(GameProgressManager progress)
        {
            score = progress.Score;
            objective = progress.CurrentObjective;
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUI.Box(new Rect(22f, 20f, 500f, 82f), GUIContent.none);
            GUI.Label(new Rect(40f, 29f, 450f, 26f), "OBJECTIVE", headingStyle);
            GUI.Label(new Rect(40f, 56f, 450f, 40f), objective, bodyStyle);
            GUI.Label(new Rect(Screen.width - 195f, 24f, 170f, 34f), $"SCORE  {score:0000}", headingStyle);

            if (!string.IsNullOrEmpty(notification) && Time.unscaledTime < notificationEndsAt)
            {
                float width = Mathf.Min(620f, Screen.width - 40f);
                GUI.Label(new Rect((Screen.width - width) * 0.5f, Screen.height * 0.17f, width, 42f), notification, notificationStyle);
            }
        }

        private void EnsureStyles()
        {
            if (headingStyle != null)
                return;

            headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.17f, 0.12f) }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            notificationStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.22f, 0.16f) }
            };
        }
    }
}
