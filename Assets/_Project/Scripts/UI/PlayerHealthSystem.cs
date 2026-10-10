using System.Collections;
using HorrorGame.AI;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HorrorGame.UI
{
    /// <summary>
    /// Player vitality and its camera-fixed VR HUD. It is created automatically for every scene.
    /// Health is drained only by an active ghost at close range and recovers once the player is safe.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerHealthSystem : MonoBehaviour
    {
        private const float MaxHealth = 100f;
        private const float DamageRange = 2.35f;
        private const float SafeRange = 5.5f;
        private const float MaximumDamagePerSecond = 16f;
        private const float RecoveryPerSecond = 11f;
        private const float ZTestAlways = (float)CompareFunction.Always;

        private float health = MaxHealth;
        private bool defeated;
        private Camera playerCamera;
        private VRStartScreenController startScreen;
        private VREndScreenController endScreen;
        private Image fill;
        private Image dangerGlow;
        private TextMeshProUGUI percentage;
        private Texture2D grungeTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoading()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CreateForScene();
        }

        private static void CreateForScene()
        {
            if (FindAnyObjectByType<PlayerHealthSystem>() != null) return;
            new GameObject("Player Health System").AddComponent<PlayerHealthSystem>();
        }

        private IEnumerator Start()
        {
            yield return null;
            ResolvePlayer();
            if (playerCamera == null) yield break;

            startScreen = FindAnyObjectByType<VRStartScreenController>();
            endScreen = FindAnyObjectByType<VREndScreenController>();
            BuildHud();
            RefreshHud();
            SetHudVisible(startScreen == null || !startScreen.gameObject.activeInHierarchy);
        }

        private void Update()
        {
            if (playerCamera == null)
            {
                ResolvePlayer();
                return;
            }
            if (startScreen != null && startScreen.gameObject.activeInHierarchy)
            {
                SetHudVisible(false);
                return;
            }
            bool gameEnded = defeated || (endScreen != null && endScreen.IsShown);
            SetHudVisible(!gameEnded);
            if (gameEnded) return;

            float nearestGhost = FindNearestActiveGhostDistance();
            if (nearestGhost <= DamageRange)
            {
                float closeness = 1f - Mathf.Clamp01(nearestGhost / DamageRange);
                float damage = Mathf.Lerp(MaximumDamagePerSecond * 0.5f, MaximumDamagePerSecond, closeness);
                SetHealth(health - damage * Time.deltaTime);
            }
            else if (nearestGhost >= SafeRange)
            {
                SetHealth(health + RecoveryPerSecond * Time.deltaTime);
            }
        }

        private float FindNearestActiveGhostDistance()
        {
            float nearest = float.PositiveInfinity;
            foreach (GhostController ghost in FindObjectsByType<GhostController>(FindObjectsInactive.Exclude))
            {
                if (ghost == null || !ghost.IsActive) continue;
                nearest = Mathf.Min(nearest, Vector3.Distance(ghost.transform.position, playerCamera.transform.position));
            }
            return nearest;
        }

        private void SetHealth(float value)
        {
            health = Mathf.Clamp(value, 0f, MaxHealth);
            RefreshHud();
            if (health <= 0f) ShowDefeat();
        }

        private void ShowDefeat()
        {
            if (defeated) return;
            defeated = true;
            SetHudVisible(false);
            if (endScreen == null) endScreen = FindAnyObjectByType<VREndScreenController>();
            if (endScreen != null) endScreen.ShowGhosted();
            else Debug.LogError("[PlayerHealthSystem] End screen missing; cannot show GHOSTED.", this);
        }

        private void ResolvePlayer()
        {
            playerCamera = Camera.main;
            if (playerCamera == null) playerCamera = FindFirstObjectByType<Camera>();
        }

        private void BuildHud()
        {
            GameObject hud = new GameObject("Health HUD", typeof(RectTransform), typeof(Canvas));
            hud.transform.SetParent(playerCamera.transform, false);
            hud.transform.localPosition = new Vector3(0.56f, 0.31f, 0.8f);
            hud.transform.localRotation = Quaternion.identity;
            hud.transform.localScale = Vector3.one * 0.0005f;

            Canvas canvas = hud.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 120;
            RectTransform root = hud.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(360f, 64f);

            Material uiMaterial = new Material(Shader.Find("UI/Default"));
            uiMaterial.SetFloat("unity_GUIZTestMode", ZTestAlways);

            Image shadow = CreateImage("Shadow", hud.transform, new Color(0f, 0f, 0f, 0.7f), uiMaterial);
            Stretch(shadow.rectTransform, Vector2.zero, Vector2.one, new Vector2(7f, -7f), new Vector2(7f, -7f));

            Image background = CreateImage("Background", hud.transform, new Color(0.035f, 0.015f, 0.02f, 0.96f), uiMaterial);
            Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image grunge = CreateImage("Distressed Overlay", hud.transform, new Color(0.72f, 0.08f, 0.06f, 0.84f), uiMaterial);
            grunge.sprite = CreateGrungeSprite();
            Stretch(grunge.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image bloodEdge = CreateImage("Blood Edge", hud.transform, new Color(0.52f, 0.015f, 0.02f, 0.9f), uiMaterial);
            Stretch(bloodEdge.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -5f), Vector2.zero);

            Image border = CreateImage("Worn Border", hud.transform, new Color(0.62f, 0.08f, 0.06f, 0.7f), uiMaterial);
            Stretch(border.rectTransform, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));

            Image track = CreateImage("Track", hud.transform, new Color(0f, 0f, 0f, 0.72f), uiMaterial);
            Stretch(track.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 14f), new Vector2(-104f, -17f));

            fill = CreateImage("Fill", track.transform, Color.green, uiMaterial);
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            for (int i = 1; i < 10; i++)
            {
                Image scar = CreateImage("Scar " + i, track.transform, new Color(0.015f, 0.005f, 0.005f, 0.68f), uiMaterial);
                float x = i / 10f;
                Stretch(scar.rectTransform, new Vector2(x, 0f), new Vector2(x, 1f), new Vector2(-1.2f, 2f), new Vector2(1.2f, -2f));
            }

            dangerGlow = CreateImage("Danger Glow", hud.transform, new Color(0.9f, 0.01f, 0.01f, 0f), uiMaterial);
            Stretch(dangerGlow.rectTransform, Vector2.zero, Vector2.one, new Vector2(-3f, -3f), new Vector2(3f, 3f));

            TextMeshProUGUI label = CreateText("Label", hud.transform, 16f);
            label.text = "V I T A L I T Y";
            label.characterSpacing = 4f;
            label.color = new Color(0.88f, 0.68f, 0.64f, 0.95f);
            label.alignment = TextAlignmentOptions.Left;
            Stretch(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(14f, -1f), new Vector2(150f, -3f));

            percentage = CreateText("Percentage", hud.transform, 30f);
            percentage.alignment = TextAlignmentOptions.Right;
            Stretch(percentage.rectTransform, new Vector2(1f, 0f), Vector2.one, new Vector2(-98f, 0f), new Vector2(-10f, 0f));
        }

        private void RefreshHud()
        {
            if (fill == null || percentage == null) return;
            float ratio = health / MaxHealth;
            fill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            fill.color = ratio > .75f ? Color.Lerp(new Color(0.95f, 0.9f, 0.04f), new Color(0.1f, 0.88f, 0.18f), (ratio - .75f) / .25f)
                : ratio > .5f ? Color.Lerp(new Color(1f, 0.62f, 0.03f), new Color(0.95f, 0.9f, 0.04f), (ratio - .5f) / .25f)
                : ratio > .25f ? Color.Lerp(new Color(0.93f, 0.06f, 0.05f), new Color(1f, 0.62f, 0.03f), (ratio - .25f) / .25f)
                : new Color(0.93f, 0.06f, 0.05f);
            percentage.text = $"{Mathf.CeilToInt(health)}%";

            if (dangerGlow != null)
            {
                float danger = Mathf.InverseLerp(0.35f, 0f, ratio);
                float pulse = 0.58f + Mathf.Sin(Time.time * 8f) * 0.24f;
                dangerGlow.color = new Color(0.9f, 0.01f, 0.01f, danger * pulse);
            }
        }

        private Sprite CreateGrungeSprite()
        {
            const int width = 128;
            const int height = 32;
            grungeTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color[] pixels = new Color[width * height];
            var random = new System.Random(9137);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noise = (float)random.NextDouble();
                    bool scratch = noise > 0.92f || (y % 9 == 0 && noise > 0.68f);
                    float alpha = scratch ? 0.20f + noise * 0.25f : noise * 0.06f;
                    pixels[y * width + x] = new Color(0.5f, 0.02f, 0.02f, alpha);
                }
            }
            grungeTexture.SetPixels(pixels);
            grungeTexture.Apply(false, true);
            return Sprite.Create(grungeTexture, new Rect(0f, 0f, width, height), new Vector2(.5f, .5f), 100f);
        }

        private void SetHudVisible(bool visible)
        {
            if (fill != null && fill.transform.parent != null)
                fill.transform.parent.parent.gameObject.SetActive(visible);
        }

        private static Image CreateImage(string name, Transform parent, Color color, Material material)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.material = material;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, float size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.raycastTarget = false;
            text.fontMaterial.SetFloat("unity_GUIZTestMode", ZTestAlways);
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void OnDestroy()
        {
            if (grungeTexture != null) Destroy(grungeTexture);
        }
    }
}
