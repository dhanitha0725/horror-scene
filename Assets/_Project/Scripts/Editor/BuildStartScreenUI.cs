using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using HorrorGame.UI;

namespace HorrorGame.Editor
{
    public static class BuildStartScreenUI
    {
        [MenuItem("Tools/Build Horror Start Screen")]
        public static void CreateOrUpdateStartScreen()
        {
            // Check if already exists in scene
            GameObject existing = GameObject.Find("StartScreen_VR");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            // Load resources
            Sprite coverSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Textures/horror_cover_keyart.png");
            AudioClip clickSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/freesound_community-electric-switch-click-clicking-43832.mp3");
            if (clickSound == null)
            {
                clickSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/light_click_single.wav");
            }
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            // 1. Root World-Space Canvas
            GameObject canvasGO = new GameObject("StartScreen_VR");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Horror Start Screen");

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(1260, 760);
            canvasRT.localScale = new Vector3(0.0012f, 0.0012f, 0.0012f);

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Add XRI TrackedDeviceGraphicRaycaster if available
            System.Type raycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (raycasterType != null)
            {
                canvasGO.AddComponent(raycasterType);
            }

            CanvasGroup canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            VRStartScreenController controller = canvasGO.AddComponent<VRStartScreenController>();
            AudioSource audioSource = canvasGO.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            // 2. Backdrop Panel (Dark Charcoal Glassmorphism with subtle crimson rim)
            GameObject bgGO = CreateUIElement("BackgroundPanel", canvasGO.transform);
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.06f, 0.06f, 0.08f, 0.94f);

            Outline bgOutline = bgGO.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.7f, 0.12f, 0.12f, 0.55f);
            bgOutline.effectDistance = new Vector2(2f, -2f);

            // 3. Left Column: Poster Card
            GameObject posterGO = CreateUIElement("PosterCard", canvasGO.transform);
            RectTransform posterRT = posterGO.GetComponent<RectTransform>();
            posterRT.anchorMin = new Vector2(0f, 0f);
            posterRT.anchorMax = new Vector2(0f, 1f);
            posterRT.pivot = new Vector2(0f, 0.5f);
            posterRT.anchoredPosition = new Vector2(24f, 0f);
            posterRT.sizeDelta = new Vector2(400f, -48f);

            Image posterImage = posterGO.AddComponent<Image>();
            if (coverSprite != null)
            {
                posterImage.sprite = coverSprite;
                posterImage.preserveAspect = true;
            }
            posterImage.color = Color.white;

            Outline posterOutline = posterGO.AddComponent<Outline>();
            posterOutline.effectColor = new Color(0.85f, 0.15f, 0.15f, 0.4f);
            posterOutline.effectDistance = new Vector2(2f, -2f);

            // 4. Right Column: Content Container
            GameObject contentGO = CreateUIElement("ContentContainer", canvasGO.transform);
            RectTransform contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 0f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0f, 1f);
            contentRT.anchoredPosition = new Vector2(450f, 0f);
            contentRT.sizeDelta = new Vector2(-474f, -48f);

            // Header: Title
            GameObject titleGO = CreateUIElement("TitleText", contentGO.transform);
            RectTransform titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -10f);
            titleRT.sizeDelta = new Vector2(0f, 54f);

            TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.font = font;
            titleTMP.text = "ECHOES OF WARD 9";
            titleTMP.fontSize = 42f;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0.96f, 0.96f, 0.98f, 1f);
            titleTMP.characterSpacing = 3f;

            // Header: Subtitle
            GameObject subGO = CreateUIElement("SubtitleText", contentGO.transform);
            RectTransform subRT = subGO.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0f, 1f);
            subRT.anchorMax = new Vector2(1f, 1f);
            subRT.pivot = new Vector2(0f, 1f);
            subRT.anchoredPosition = new Vector2(0f, -66f);
            subRT.sizeDelta = new Vector2(0f, 30f);

            TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
            subTMP.font = font;
            subTMP.text = "THE BUNKER INCIDENT • SURVIVAL HORROR";
            subTMP.fontSize = 17f;
            subTMP.fontStyle = FontStyles.Bold;
            subTMP.color = new Color(0.9f, 0.22f, 0.22f, 1f);
            subTMP.characterSpacing = 4f;

            // Divider Line
            GameObject divGO = CreateUIElement("Divider", contentGO.transform);
            RectTransform divRT = divGO.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0f, 1f);
            divRT.anchorMax = new Vector2(1f, 1f);
            divRT.pivot = new Vector2(0f, 1f);
            divRT.anchoredPosition = new Vector2(0f, -102f);
            divRT.sizeDelta = new Vector2(0f, 2f);

            Image divImage = divGO.AddComponent<Image>();
            divImage.color = new Color(0.75f, 0.15f, 0.15f, 0.5f);

            // Lore Section Header
            GameObject loreHeaderGO = CreateUIElement("LoreHeader", contentGO.transform);
            RectTransform loreHeaderRT = loreHeaderGO.GetComponent<RectTransform>();
            loreHeaderRT.anchorMin = new Vector2(0f, 1f);
            loreHeaderRT.anchorMax = new Vector2(1f, 1f);
            loreHeaderRT.pivot = new Vector2(0f, 1f);
            loreHeaderRT.anchoredPosition = new Vector2(0f, -114f);
            loreHeaderRT.sizeDelta = new Vector2(0f, 26f);

            TextMeshProUGUI loreHeaderTMP = loreHeaderGO.AddComponent<TextMeshProUGUI>();
            loreHeaderTMP.font = font;
            loreHeaderTMP.text = "ABOUT THE SCENARIO";
            loreHeaderTMP.fontSize = 17f;
            loreHeaderTMP.fontStyle = FontStyles.Bold;
            loreHeaderTMP.color = new Color(0.82f, 0.84f, 0.88f, 1f);

            // Lore Body Text
            GameObject loreBodyGO = CreateUIElement("LoreBody", contentGO.transform);
            RectTransform loreBodyRT = loreBodyGO.GetComponent<RectTransform>();
            loreBodyRT.anchorMin = new Vector2(0f, 1f);
            loreBodyRT.anchorMax = new Vector2(1f, 1f);
            loreBodyRT.pivot = new Vector2(0f, 1f);
            loreBodyRT.anchoredPosition = new Vector2(0f, -144f);
            loreBodyRT.sizeDelta = new Vector2(0f, 120f);

            TextMeshProUGUI loreBodyTMP = loreBodyGO.AddComponent<TextMeshProUGUI>();
            loreBodyTMP.font = font;
            loreBodyTMP.text = "Originally sealed deep within an abandoned WWII compound, Corridor 9 holds the memory of a dark tragedy. A young girl perished within these walls under horrific circumstances—and something unnatural stayed behind.\n\nYou are here to investigate the unexplainable phenomena. Expect severe acoustic anomalies, flickering power, and sudden apparitions.";
            loreBodyTMP.fontSize = 15f;
            loreBodyTMP.color = new Color(0.75f, 0.77f, 0.8f, 1f);
            loreBodyTMP.lineSpacing = 16f;

            // Objective Box
            GameObject objBoxGO = CreateUIElement("ObjectiveBox", contentGO.transform);
            RectTransform objBoxRT = objBoxGO.GetComponent<RectTransform>();
            objBoxRT.anchorMin = new Vector2(0f, 1f);
            objBoxRT.anchorMax = new Vector2(1f, 1f);
            objBoxRT.pivot = new Vector2(0f, 1f);
            objBoxRT.anchoredPosition = new Vector2(0f, -276f);
            objBoxRT.sizeDelta = new Vector2(0f, 50f);

            Image objBoxImage = objBoxGO.AddComponent<Image>();
            objBoxImage.color = new Color(0.18f, 0.06f, 0.06f, 0.85f);

            Outline objBoxOutline = objBoxGO.AddComponent<Outline>();
            objBoxOutline.effectColor = new Color(0.9f, 0.2f, 0.2f, 0.45f);

            GameObject objTextGO = CreateUIElement("ObjectiveText", objBoxGO.transform);
            RectTransform objTextRT = objTextGO.GetComponent<RectTransform>();
            objTextRT.anchorMin = Vector2.zero;
            objTextRT.anchorMax = Vector2.one;
            objTextRT.offsetMin = new Vector2(16f, 4f);
            objTextRT.offsetMax = new Vector2(-16f, -4f);

            TextMeshProUGUI objTMP = objTextGO.AddComponent<TextMeshProUGUI>();
            objTMP.font = font;
            objTMP.text = "<color=#FF4D4D><b>PRIMARY OBJECTIVE:</b></color> Explore the corridor, recover the weapon, and survive the entity.";
            objTMP.fontSize = 15.5f;
            objTMP.alignment = TextAlignmentOptions.MidlineLeft;
            objTMP.color = new Color(0.92f, 0.92f, 0.95f, 1f);

            // Controls Section
            GameObject ctrlGO = CreateUIElement("ControlsSection", contentGO.transform);
            RectTransform ctrlRT = ctrlGO.GetComponent<RectTransform>();
            ctrlRT.anchorMin = new Vector2(0f, 1f);
            ctrlRT.anchorMax = new Vector2(1f, 1f);
            ctrlRT.pivot = new Vector2(0f, 1f);
            ctrlRT.anchoredPosition = new Vector2(0f, -338f);
            ctrlRT.sizeDelta = new Vector2(0f, 100f);

            TextMeshProUGUI ctrlTMP = ctrlGO.AddComponent<TextMeshProUGUI>();
            ctrlTMP.font = font;
            ctrlTMP.text = "<color=#CCCCCC><b>HOW TO PLAY / CONTROLS:</b></color>\n" +
                           "• <b>VR Controllers:</b> Left Stick to Move | Right Stick to Turn (Snap)\n" +
                           "• <b>Interaction:</b> Grip to Pick Up Bat / Items | Trigger to Swing Weapon\n" +
                           "• <b>Desktop / Simulator:</b> WASD to Move | Mouse to Look | Space to Swing Bat";
            ctrlTMP.fontSize = 14f;
            ctrlTMP.lineSpacing = 14f;
            ctrlTMP.color = new Color(0.72f, 0.75f, 0.78f, 1f);

            // Button Bar
            GameObject btnBarGO = CreateUIElement("ButtonBar", contentGO.transform);
            RectTransform btnBarRT = btnBarGO.GetComponent<RectTransform>();
            btnBarRT.anchorMin = new Vector2(0f, 0f);
            btnBarRT.anchorMax = new Vector2(1f, 0f);
            btnBarRT.pivot = new Vector2(0f, 0f);
            btnBarRT.anchoredPosition = new Vector2(0f, 32f);
            btnBarRT.sizeDelta = new Vector2(0f, 60f);

            // Play Button (Primary Glowing Red)
            GameObject playBtnGO = CreateUIElement("PlayButton", btnBarGO.transform);
            RectTransform playBtnRT = playBtnGO.GetComponent<RectTransform>();
            playBtnRT.anchorMin = new Vector2(0f, 0.5f);
            playBtnRT.anchorMax = new Vector2(0f, 0.5f);
            playBtnRT.pivot = new Vector2(0f, 0.5f);
            playBtnRT.anchoredPosition = new Vector2(0f, 0f);
            playBtnRT.sizeDelta = new Vector2(250f, 54f);

            Image playBtnImage = playBtnGO.AddComponent<Image>();
            playBtnImage.color = new Color(0.85f, 0.15f, 0.15f, 1f);

            Button playBtn = playBtnGO.AddComponent<Button>();
            ColorBlock playColors = playBtn.colors;
            playColors.normalColor = new Color(0.85f, 0.15f, 0.15f, 1f);
            playColors.highlightedColor = new Color(1.0f, 0.28f, 0.28f, 1f);
            playColors.pressedColor = new Color(0.6f, 0.08f, 0.08f, 1f);
            playColors.selectedColor = playColors.highlightedColor;
            playBtn.colors = playColors;

            GameObject playTextGO = CreateUIElement("Text", playBtnGO.transform);
            RectTransform playTextRT = playTextGO.GetComponent<RectTransform>();
            playTextRT.anchorMin = Vector2.zero;
            playTextRT.anchorMax = Vector2.one;
            playTextRT.sizeDelta = Vector2.zero;

            TextMeshProUGUI playTMP = playTextGO.AddComponent<TextMeshProUGUI>();
            playTMP.font = font;
            playTMP.text = "<b>PLAY NOW  ▶</b>";
            playTMP.fontSize = 22f;
            playTMP.alignment = TextAlignmentOptions.Center;
            playTMP.color = Color.white;

            // Recenter / Shortcut Tip
            GameObject tipGO = CreateUIElement("TipText", btnBarGO.transform);
            RectTransform tipRT = tipGO.GetComponent<RectTransform>();
            tipRT.anchorMin = new Vector2(0f, 0f);
            tipRT.anchorMax = new Vector2(1f, 0f);
            tipRT.pivot = new Vector2(0f, 0f);
            tipRT.anchoredPosition = new Vector2(270f, -22f);
            tipRT.sizeDelta = new Vector2(400f, 24f);

            TextMeshProUGUI tipTMP = tipGO.AddComponent<TextMeshProUGUI>();
            tipTMP.font = font;
            tipTMP.text = "Press [Space] or [Enter] or Click to Play • Press [R] to Recenter";
            tipTMP.fontSize = 12.5f;
            tipTMP.fontStyle = FontStyles.Italic;
            tipTMP.color = new Color(0.55f, 0.58f, 0.62f, 1f);

            // Hook up VRStartScreenController serialized fields using SerializedObject
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("distanceFromPlayer").floatValue = 2.0f;
            so.FindProperty("heightOffset").floatValue = -0.05f;
            so.FindProperty("fadeOutDuration").floatValue = 0.75f;
            so.FindProperty("lockLocomotionUntilPlay").boolValue = true;
            if (clickSound != null)
            {
                so.FindProperty("playStartSound").objectReferenceValue = clickSound;
            }
            so.FindProperty("playButton").objectReferenceValue = playBtn;
            so.ApplyModifiedProperties();

            // Set initial position in front of XR Origin in Scene
            GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin != null)
            {
                Vector3 originPos = xrOrigin.transform.position;
                Vector3 originFwd = xrOrigin.transform.forward;
                originFwd.y = 0;
                originFwd.Normalize();
                canvasGO.transform.position = originPos + (originFwd * 2.0f) + new Vector3(0, 1.45f, 0);
                canvasGO.transform.rotation = Quaternion.LookRotation(originFwd, Vector3.up);
            }
            else
            {
                canvasGO.transform.position = new Vector3(4f, 1.5f, 1.5f);
                canvasGO.transform.rotation = Quaternion.Euler(0, 180f, 0);
            }

            // Mark Scene Dirty and Save
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvasGO.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(canvasGO.scene);
            Debug.Log("[VRStartScreen] Successfully constructed and saved StartScreen_VR to SampleScene!");
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
