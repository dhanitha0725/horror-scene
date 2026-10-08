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
            // Destroy existing if present
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

            // Horror Fonts
            TMP_FontAsset creepsterFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/Creepster-Regular SDF.asset");
            TMP_FontAsset specialEliteFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/SpecialElite-Regular SDF.asset");
            TMP_FontAsset nosiferFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/Nosifer-Regular SDF.asset");
            TMP_FontAsset chillerFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/Chiller SDF.asset");
            TMP_FontAsset fallbackFont = TMP_Settings.defaultFontAsset;

            TMP_FontAsset titleFont = creepsterFont != null ? creepsterFont : fallbackFont;
            TMP_FontAsset typewriterFont = specialEliteFont != null ? specialEliteFont : fallbackFont;
            TMP_FontAsset buttonFont = creepsterFont != null ? creepsterFont : (chillerFont != null ? chillerFont : fallbackFont);

            // 1. Root World-Space Canvas
            GameObject canvasGO = new GameObject("StartScreen_VR");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Horror Start Screen");

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(1360, 800);
            canvasRT.localScale = new Vector3(0.0011f, 0.0011f, 0.0011f);

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

            // 2. Backdrop Panel (Ominous obsidian with bloody crimson border)
            GameObject bgGO = CreateUIElement("BackgroundPanel", canvasGO.transform);
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.035f, 0.025f, 0.035f, 0.96f);

            Outline bgOutline = bgGO.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.85f, 0.12f, 0.12f, 0.65f);
            bgOutline.effectDistance = new Vector2(3f, -3f);

            Shadow bgShadow = bgGO.AddComponent<Shadow>();
            bgShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            bgShadow.effectDistance = new Vector2(6f, -6f);

            // 3. Left Column: Poster Card Frame
            GameObject posterFrameGO = CreateUIElement("PosterFrame", canvasGO.transform);
            RectTransform posterFrameRT = posterFrameGO.GetComponent<RectTransform>();
            posterFrameRT.anchorMin = new Vector2(0f, 0f);
            posterFrameRT.anchorMax = new Vector2(0f, 1f);
            posterFrameRT.pivot = new Vector2(0f, 0.5f);
            posterFrameRT.anchoredPosition = new Vector2(28f, 0f);
            posterFrameRT.sizeDelta = new Vector2(400f, -56f);

            Image posterFrameImage = posterFrameGO.AddComponent<Image>();
            posterFrameImage.color = new Color(0.08f, 0.06f, 0.07f, 1f);

            Outline posterFrameOutline = posterFrameGO.AddComponent<Outline>();
            posterFrameOutline.effectColor = new Color(0.9f, 0.15f, 0.15f, 0.5f);
            posterFrameOutline.effectDistance = new Vector2(2f, -2f);

            // Poster Inner Image
            GameObject posterImageGO = CreateUIElement("PosterImage", posterFrameGO.transform);
            RectTransform posterImageRT = posterImageGO.GetComponent<RectTransform>();
            posterImageRT.anchorMin = Vector2.zero;
            posterImageRT.anchorMax = Vector2.one;
            posterImageRT.offsetMin = new Vector2(8f, 8f);
            posterImageRT.offsetMax = new Vector2(-8f, -8f);

            Image posterImage = posterImageGO.AddComponent<Image>();
            if (coverSprite != null)
            {
                posterImage.sprite = coverSprite;
                posterImage.preserveAspect = true;
            }
            posterImage.color = Color.white;

            // 4. Right Column: Content Container
            GameObject contentGO = CreateUIElement("ContentContainer", canvasGO.transform);
            RectTransform contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 0f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0f, 1f);
            contentRT.anchoredPosition = new Vector2(460f, 0f);
            contentRT.sizeDelta = new Vector2(-488f, -56f);

            // Header: Title in Creepster Horror Font
            GameObject titleGO = CreateUIElement("TitleText", contentGO.transform);
            RectTransform titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -8f);
            titleRT.sizeDelta = new Vector2(0f, 72f);

            TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.font = titleFont;
            titleTMP.text = "ECHOES OF WARD 9";
            titleTMP.fontSize = 62f;
            titleTMP.fontStyle = FontStyles.Normal;
            titleTMP.color = new Color(0.96f, 0.96f, 0.94f, 1f);
            titleTMP.characterSpacing = 2f;

            // Title Shadow / Glow
            Outline titleOutline = titleGO.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.85f, 0.08f, 0.08f, 0.8f);
            titleOutline.effectDistance = new Vector2(3f, -3f);

            // Header: Subtitle in SpecialElite typewriter font
            GameObject subGO = CreateUIElement("SubtitleText", contentGO.transform);
            RectTransform subRT = subGO.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0f, 1f);
            subRT.anchorMax = new Vector2(1f, 1f);
            subRT.pivot = new Vector2(0f, 1f);
            subRT.anchoredPosition = new Vector2(0f, -82f);
            subRT.sizeDelta = new Vector2(0f, 32f);

            TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
            subTMP.font = typewriterFont;
            subTMP.text = "// CLASSIFIED 1944 BUNKER • SURVIVAL HORROR //";
            subTMP.fontSize = 19f;
            subTMP.fontStyle = FontStyles.Bold;
            subTMP.color = new Color(1f, 0.25f, 0.25f, 1f);
            subTMP.characterSpacing = 3f;

            // Divider Line with bloody red glow
            GameObject divGO = CreateUIElement("Divider", contentGO.transform);
            RectTransform divRT = divGO.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0f, 1f);
            divRT.anchorMax = new Vector2(1f, 1f);
            divRT.pivot = new Vector2(0f, 1f);
            divRT.anchoredPosition = new Vector2(0f, -120f);
            divRT.sizeDelta = new Vector2(0f, 3f);

            Image divImage = divGO.AddComponent<Image>();
            divImage.color = new Color(0.85f, 0.15f, 0.15f, 0.7f);

            // Lore Section Header
            GameObject loreHeaderGO = CreateUIElement("LoreHeader", contentGO.transform);
            RectTransform loreHeaderRT = loreHeaderGO.GetComponent<RectTransform>();
            loreHeaderRT.anchorMin = new Vector2(0f, 1f);
            loreHeaderRT.anchorMax = new Vector2(1f, 1f);
            loreHeaderRT.pivot = new Vector2(0f, 1f);
            loreHeaderRT.anchoredPosition = new Vector2(0f, -134f);
            loreHeaderRT.sizeDelta = new Vector2(0f, 30f);

            TextMeshProUGUI loreHeaderTMP = loreHeaderGO.AddComponent<TextMeshProUGUI>();
            loreHeaderTMP.font = typewriterFont;
            loreHeaderTMP.text = ">> CASE FILE BRIEFING:";
            loreHeaderTMP.fontSize = 20f;
            loreHeaderTMP.fontStyle = FontStyles.Bold;
            loreHeaderTMP.color = new Color(0.92f, 0.92f, 0.94f, 1f);

            // Lore Body Text in typewriter font
            GameObject loreBodyGO = CreateUIElement("LoreBody", contentGO.transform);
            RectTransform loreBodyRT = loreBodyGO.GetComponent<RectTransform>();
            loreBodyRT.anchorMin = new Vector2(0f, 1f);
            loreBodyRT.anchorMax = new Vector2(1f, 1f);
            loreBodyRT.pivot = new Vector2(0f, 1f);
            loreBodyRT.anchoredPosition = new Vector2(0f, -170f);
            loreBodyRT.sizeDelta = new Vector2(0f, 150f);

            TextMeshProUGUI loreBodyTMP = loreBodyGO.AddComponent<TextMeshProUGUI>();
            loreBodyTMP.font = typewriterFont;
            loreBodyTMP.text = "Corridor 9 was permanently sealed following a dark occurrence in 1944 that claimed the life of a young girl under horrific circumstances.\n\nDecades later, violent acoustic anomalies and spatial distortions continue to breach containment. Enter at your own risk. Expect flickering power, disorienting whispers, and sudden hostile apparitions.";
            loreBodyTMP.fontSize = 19f;
            loreBodyTMP.color = new Color(0.86f, 0.86f, 0.88f, 1f);
            loreBodyTMP.lineSpacing = 16f;

            // Objective Box (Blood-bordered containment warning)
            GameObject objBoxGO = CreateUIElement("ObjectiveBox", contentGO.transform);
            RectTransform objBoxRT = objBoxGO.GetComponent<RectTransform>();
            objBoxRT.anchorMin = new Vector2(0f, 1f);
            objBoxRT.anchorMax = new Vector2(1f, 1f);
            objBoxRT.pivot = new Vector2(0f, 1f);
            objBoxRT.anchoredPosition = new Vector2(0f, -332f);
            objBoxRT.sizeDelta = new Vector2(0f, 62f);

            Image objBoxImage = objBoxGO.AddComponent<Image>();
            objBoxImage.color = new Color(0.24f, 0.05f, 0.06f, 0.92f);

            Outline objBoxOutline = objBoxGO.AddComponent<Outline>();
            objBoxOutline.effectColor = new Color(1f, 0.2f, 0.2f, 0.7f);

            GameObject objTextGO = CreateUIElement("ObjectiveText", objBoxGO.transform);
            RectTransform objTextRT = objTextGO.GetComponent<RectTransform>();
            objTextRT.anchorMin = Vector2.zero;
            objTextRT.anchorMax = Vector2.one;
            objTextRT.offsetMin = new Vector2(18f, 4f);
            objTextRT.offsetMax = new Vector2(-18f, -4f);

            TextMeshProUGUI objTMP = objTextGO.AddComponent<TextMeshProUGUI>();
            objTMP.font = typewriterFont;
            objTMP.text = "<color=#FF3333><b>[ MISSION DIRECTIVE ]</b></color>  Explore the sealed corridor, locate the defensive bat, and survive the entity.";
            objTMP.fontSize = 18f;
            objTMP.alignment = TextAlignmentOptions.MidlineLeft;
            objTMP.color = new Color(0.97f, 0.97f, 0.98f, 1f);

            // Controls Section in typewriter font
            GameObject ctrlGO = CreateUIElement("ControlsSection", contentGO.transform);
            RectTransform ctrlRT = ctrlGO.GetComponent<RectTransform>();
            ctrlRT.anchorMin = new Vector2(0f, 1f);
            ctrlRT.anchorMax = new Vector2(1f, 1f);
            ctrlRT.pivot = new Vector2(0f, 1f);
            ctrlRT.anchoredPosition = new Vector2(0f, -408f);
            ctrlRT.sizeDelta = new Vector2(0f, 120f);

            TextMeshProUGUI ctrlTMP = ctrlGO.AddComponent<TextMeshProUGUI>();
            ctrlTMP.font = typewriterFont;
            ctrlTMP.text = "<b><color=#D4D4D4>SURVIVAL PROTOCOLS & CONTROLS:</color></b>\n" +
                           "• <color=#FF5555>VR HEADSET:</color> Left Stick (Move) | Right Stick (Snap Turn)\n" +
                           "• <color=#FF5555>COMBAT:</color> Grip (Pick Up Bat) | Swing / Trigger (Attack)\n" +
                           "• <color=#FF5555>DESKTOP / SIMULATOR:</color> WASD (Move) | Mouse (Look) | Space (Swing Bat)";
            ctrlTMP.fontSize = 17.5f;
            ctrlTMP.lineSpacing = 16f;
            ctrlTMP.color = new Color(0.82f, 0.84f, 0.88f, 1f);

            // Button Bar
            GameObject btnBarGO = CreateUIElement("ButtonBar", contentGO.transform);
            RectTransform btnBarRT = btnBarGO.GetComponent<RectTransform>();
            btnBarRT.anchorMin = new Vector2(0f, 0f);
            btnBarRT.anchorMax = new Vector2(1f, 0f);
            btnBarRT.pivot = new Vector2(0f, 0f);
            btnBarRT.anchoredPosition = new Vector2(0f, 32f);
            btnBarRT.sizeDelta = new Vector2(0f, 74f);

            // Play Button with Menacing Red Glow & Creepster Font
            GameObject playBtnGO = CreateUIElement("PlayButton", btnBarGO.transform);
            RectTransform playBtnRT = playBtnGO.GetComponent<RectTransform>();
            playBtnRT.anchorMin = new Vector2(0f, 0.5f);
            playBtnRT.anchorMax = new Vector2(0f, 0.5f);
            playBtnRT.pivot = new Vector2(0f, 0.5f);
            playBtnRT.anchoredPosition = new Vector2(0f, 0f);
            playBtnRT.sizeDelta = new Vector2(330f, 66f);

            Image playBtnImage = playBtnGO.AddComponent<Image>();
            playBtnImage.color = new Color(0.78f, 0.08f, 0.08f, 1f);

            Outline playBtnOutline = playBtnGO.AddComponent<Outline>();
            playBtnOutline.effectColor = new Color(1f, 0.28f, 0.28f, 0.8f);
            playBtnOutline.effectDistance = new Vector2(2f, -2f);

            Button playBtn = playBtnGO.AddComponent<Button>();
            ColorBlock playColors = playBtn.colors;
            playColors.normalColor = new Color(0.78f, 0.08f, 0.08f, 1f);
            playColors.highlightedColor = new Color(0.98f, 0.18f, 0.18f, 1f);
            playColors.pressedColor = new Color(0.5f, 0.05f, 0.05f, 1f);
            playColors.selectedColor = playColors.highlightedColor;
            playBtn.colors = playColors;

            GameObject playTextGO = CreateUIElement("Text", playBtnGO.transform);
            RectTransform playTextRT = playTextGO.GetComponent<RectTransform>();
            playTextRT.anchorMin = Vector2.zero;
            playTextRT.anchorMax = Vector2.one;
            playTextRT.sizeDelta = Vector2.zero;

            TextMeshProUGUI playTMP = playTextGO.AddComponent<TextMeshProUGUI>();
            playTMP.font = buttonFont;
            playTMP.text = "ENTER CORRIDOR >>";
            playTMP.fontSize = 32f;
            playTMP.alignment = TextAlignmentOptions.Center;
            playTMP.color = new Color(1f, 0.96f, 0.92f, 1f);

            // Recenter / Shortcut Tip in typewriter font
            GameObject tipGO = CreateUIElement("TipText", btnBarGO.transform);
            RectTransform tipRT = tipGO.GetComponent<RectTransform>();
            tipRT.anchorMin = new Vector2(0f, 0f);
            tipRT.anchorMax = new Vector2(1f, 0f);
            tipRT.pivot = new Vector2(0f, 0f);
            tipRT.anchoredPosition = new Vector2(350f, -14f);
            tipRT.sizeDelta = new Vector2(460f, 26f);

            TextMeshProUGUI tipTMP = tipGO.AddComponent<TextMeshProUGUI>();
            tipTMP.font = typewriterFont;
            tipTMP.text = "Press [Space / Enter] or Click to Enter • Press [R] to Recenter";
            tipTMP.fontSize = 14f;
            tipTMP.fontStyle = FontStyles.Italic;
            tipTMP.color = new Color(0.65f, 0.67f, 0.72f, 1f);

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
            Debug.Log("[VRStartScreen] Successfully constructed scary horror start screen UI!");
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
