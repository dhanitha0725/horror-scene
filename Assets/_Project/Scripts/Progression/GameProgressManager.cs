using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using HorrorGame.Combat;

namespace HorrorGame.Progression
{
    /// <summary>
    /// Owns the game-wide progression state. Later encounter, item, and ending
    /// components report their completed milestones here instead of controlling
    /// each other directly.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameProgressManager : MonoBehaviour
    {
        public static GameProgressManager Instance { get; private set; }

        public static event Action<GameProgressManager> StateChanged;
        public static event Action<string> ObjectiveChanged;
        public static event Action<string> NotificationRaised;

        public bool HasBat { get; private set; }
        public bool Item1Collected { get; private set; }
        public bool Item2Collected { get; private set; }
        public bool Item3Collected { get; private set; }
        public bool GhostDefeated { get; private set; }
        public bool GameFinished { get; private set; }
        public int Score { get; private set; }
        public string CurrentObjective { get; private set; } = "Find the bat hanging nearby.";

        public bool HasAllObjectiveItems => Item1Collected && Item2Collected && Item3Collected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntimeManager()
        {
            if (Instance != null)
                return;

            GameObject managerObject = new GameObject("Game Progress Manager");
            managerObject.AddComponent<GameProgressManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            BatWeapon.OnAnyBatFirstPickedUp += HandleBatPickedUp;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            BatWeapon.OnAnyBatFirstPickedUp -= HandleBatPickedUp;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            EnsurePhaseOneSetup();
            PublishState();
            ObjectiveChanged?.Invoke(CurrentObjective);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!HasBat && !GameFinished)
                SetObjective("Find the bat hanging nearby.");

            EnsurePhaseOneSetup();
        }

        private void EnsurePhaseOneSetup()
        {
            BatWeapon[] bats = FindObjectsByType<BatWeapon>(FindObjectsInactive.Exclude);
            foreach (BatWeapon bat in bats)
            {
                if (bat.GetComponent<BatWallMount>() == null && !HasBat)
                    bat.gameObject.AddComponent<BatWallMount>();
            }
        }

        private void HandleBatPickedUp(BatWeapon bat)
        {
            if (HasBat)
                return;

            HasBat = true;
            AddScore(25, false);
            SetObjective("Explore the corridors and find the first clue.");
            RaiseNotification("BAT ACQUIRED  +25");
            PublishState();
        }

        public void CollectObjectiveItem(int itemNumber, int scoreValue = 100)
        {
            bool changed = itemNumber switch
            {
                1 when !Item1Collected => SetItem1(),
                2 when !Item2Collected => SetItem2(),
                3 when !Item3Collected => SetItem3(),
                _ => false
            };

            if (!changed)
                return;

            AddScore(scoreValue, false);
            RaiseNotification($"OBJECTIVE ITEM {itemNumber} COLLECTED  +{scoreValue}");

            if (HasAllObjectiveItems)
                SetObjective("Reach the exit and confront the ghost.");

            PublishState();
        }

        public void MarkGhostDefeated()
        {
            if (GhostDefeated)
                return;

            GhostDefeated = true;
            AddScore(500, false);
            SetObjective("Escape through the exit.");
            RaiseNotification("THE GHOST IS BANISHED  +500");
            PublishState();
        }

        public void FinishGame()
        {
            if (GameFinished)
                return;

            GameFinished = true;
            SetObjective("You escaped.");
            RaiseNotification("ESCAPED THE HOUSE");
            PublishState();
        }

        public void AddScore(int amount, bool notify = true)
        {
            if (amount <= 0)
                return;

            Score += amount;
            if (notify)
                RaiseNotification($"SCORE +{amount}");
            PublishState();
        }

        public void SetObjective(string objective)
        {
            if (string.IsNullOrWhiteSpace(objective) || CurrentObjective == objective)
                return;

            CurrentObjective = objective;
            ObjectiveChanged?.Invoke(CurrentObjective);
            PublishState();
        }

        public void RaiseNotification(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                NotificationRaised?.Invoke(message);
        }

        private bool SetItem1() { Item1Collected = true; return true; }
        private bool SetItem2() { Item2Collected = true; return true; }
        private bool SetItem3() { Item3Collected = true; return true; }

        private void PublishState()
        {
            StateChanged?.Invoke(this);
        }
    }
}
