using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Reusable floating (world-space) message panel for VR. One instance in the scene (e.g. on the XR Origin).
/// Any script can call WorldMessage.ShowGlobal("TITLE", "body text", seconds).
/// The panel appears in front of the player's eyes, follows the head smoothly, is drawn on top of walls,
/// and fades in and out.
/// </summary>
[DisallowMultipleComponent]
public class WorldMessage : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private Color defaultTitleColor = new Color(0.85f, 0.05f, 0.05f);
    [SerializeField] private Color bodyColor = new Color(0.92f, 0.90f, 0.88f);
    [SerializeField] private Color backgroundColor = new Color(0.03f, 0.02f, 0.02f, 0.82f);
    [SerializeField] private Vector2 panelSize = new Vector2(900f, 330f);
    [SerializeField, Min(0.0001f)] private float worldScale = 0.001f;

    [Header("Placement")]
    [SerializeField, Min(0.3f)] private float distance = 1.4f;
    [SerializeField] private float heightOffset = -0.12f;
    [SerializeField, Min(0f)] private float followSmoothing = 6f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.3f;
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.5f;

    private const float ZTestAlways = (float)CompareFunction.Always;
    private static WorldMessage instance;

    private GameObject panel;
    private CanvasGroup group;
    private Image topBar;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private Transform head;
    private Coroutine routine;

    public static WorldMessage Instance
    {
        get
        {
            if (instance == null) instance = FindAnyObjectByType<WorldMessage>();
            return instance;
        }
    }

    /// <summary>Show a message through the scene's WorldMessage (logs if none exists).</summary>
    public static void ShowGlobal(string title, string body, float seconds)
    {
        var i = Instance;
        if (i != null) i.Show(title, body, seconds);
        else Debug.Log($"[WorldMessage] (no panel in scene) {title}: {body}");
    }

    public static void ShowGlobal(string title, string body, float seconds, Color titleColor)
    {
        var i = Instance;
        if (i != null) i.Show(title, body, seconds, titleColor);
        else Debug.Log($"[WorldMessage] (no panel in scene) {title}: {body}");
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[WorldMessage] More than one WorldMessage in the scene; using the first.", this);
        }
        else instance = this;
        Build();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (panel != null) Destroy(panel);
    }

    public void Show(string title, string body, float seconds)
    {
        Show(title, body, seconds, defaultTitleColor);
    }

    public void Show(string title, string body, float seconds, Color titleColor)
    {
        if (!isActiveAndEnabled) return;
        titleText.text = title;
        titleText.color = titleColor;
        topBar.color = titleColor;
        bodyText.text = body;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run(Mathf.Max(0.1f, seconds)));
    }

    private IEnumerator Run(float seconds)
    {
        if (head == null && Camera.main != null) head = Camera.main.transform;
        if (head == null) yield break;

        bool wasVisible = panel.activeSelf && group.alpha > 0.01f;
        if (!wasVisible) Snap();
        panel.SetActive(true);

        yield return Fade(group.alpha, 1f, wasVisible ? 0.1f : fadeInSeconds);
        float t = 0f;
        while (t < seconds) { Follow(); t += Time.deltaTime; yield return null; }
        yield return Fade(1f, 0f, fadeOutSeconds);
        panel.SetActive(false);
        routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f) { group.alpha = to; yield break; }
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            group.alpha = Mathf.Lerp(from, to, t * t * (3f - 2f * t));
            Follow();
            yield return null;
        }
    }

    private void Target(out Vector3 pos, out Quaternion rot)
    {
        Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (fwd.sqrMagnitude < 0.0001f) fwd = head.forward;
        fwd.Normalize();
        pos = head.position + fwd * distance + Vector3.up * heightOffset;
        rot = Quaternion.LookRotation(pos - head.position, Vector3.up);
    }

    private void Snap()
    {
        Target(out Vector3 p, out Quaternion r);
        panel.transform.SetPositionAndRotation(p, r);
    }

    private void Follow()
    {
        if (head == null) return;
        Target(out Vector3 p, out Quaternion r);
        float k = followSmoothing <= 0f ? 1f : 1f - Mathf.Exp(-followSmoothing * Time.deltaTime);
        panel.transform.SetPositionAndRotation(Vector3.Lerp(panel.transform.position, p, k), Quaternion.Slerp(panel.transform.rotation, r, k));
    }

    private void Build()
    {
        panel = new GameObject("WorldMessage_UI");
        var canvas = panel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 110;
        panel.GetComponent<RectTransform>().sizeDelta = panelSize;
        panel.transform.localScale = Vector3.one * worldScale;
        group = panel.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        var mat = new Material(Shader.Find("UI/Default"));
        mat.SetFloat("unity_GUIZTestMode", ZTestAlways);

        var bg = Img("Background", backgroundColor, mat);
        Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        topBar = Img("TopBar", defaultTitleColor, mat);
        Stretch(topBar.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -8f), Vector2.zero);
        var bottom = Img("BottomBar", new Color(defaultTitleColor.r, defaultTitleColor.g, defaultTitleColor.b, 0.6f), mat);
        Stretch(bottom.rectTransform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 4f));

        titleText = Txt("Title", 78f, defaultTitleColor, FontStyles.Bold);
        titleText.characterSpacing = 18f;
        Stretch(titleText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(30f, -120f), new Vector2(-30f, -20f));
        bodyText = Txt("Body", 38f, bodyColor, FontStyles.Normal);
        bodyText.lineSpacing = 8f;
        Stretch(bodyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(40f, 25f), new Vector2(-40f, -125f));
    }

    private Image Img(string name, Color c, Material m)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(panel.transform, false);
        var img = go.GetComponent<Image>();
        img.color = c;
        img.material = m;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI Txt(string name, float size, Color c, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.color = c;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        t.fontMaterial.SetFloat("unity_GUIZTestMode", ZTestAlways);
        return t;
    }

    private static void Stretch(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
    }
}
