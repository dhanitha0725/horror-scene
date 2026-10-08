using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Shows a floating world-space message in front of the player the first time the bat is grabbed,
/// then fades it out after a few seconds. VR-friendly (spatial UI, follows the head smoothly,
/// always drawn on top so walls never hide it). Put it on the bat (same object as XRGrabInteractable).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public class BatPickupMessage : MonoBehaviour
{
    [Header("Message")]
    [SerializeField] private string title = "LUCILLE";
    [SerializeField, TextArea(2, 6)] private string body =
        "Negan's bat, wrapped in barbed wire.\n" +
        "Whatever haunts this house... she won't go quietly.\n" +
        "Use it to survive.  <b>Left Click to swing.</b>";
    [SerializeField] private Color titleColor = new Color(0.85f, 0.05f, 0.05f);
    [SerializeField] private Color bodyColor = new Color(0.92f, 0.90f, 0.88f);
    [SerializeField] private Color backgroundColor = new Color(0.03f, 0.02f, 0.02f, 0.82f);

    [Header("Timing")]
    [Tooltip("Seconds the message stays fully visible.")]
    [SerializeField, Min(0f)] private float showSeconds = 3f;
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.35f;
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.6f;
    [Tooltip("Only show the first time the bat is picked up.")]
    [SerializeField] private bool showOnlyOnce = true;

    [Header("Placement")]
    [Tooltip("Distance in front of the player's eyes (metres).")]
    [SerializeField, Min(0.3f)] private float distance = 1.4f;
    [Tooltip("Up/down offset from eye height (metres). Slightly below eye level is most comfortable.")]
    [SerializeField] private float heightOffset = -0.12f;
    [Tooltip("How quickly the panel follows head movement. Higher = snappier.")]
    [SerializeField, Min(0f)] private float followSmoothing = 6f;
    [SerializeField] private Vector2 panelSize = new Vector2(900f, 330f);
    [Tooltip("Canvas units to metres. 0.001 makes a 900-unit panel 0.9 m wide.")]
    [SerializeField, Min(0.0001f)] private float worldScale = 0.001f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioClip showClip;
    [SerializeField, Range(0f, 1f)] private float showVolume = 0.6f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private const float ZTestAlways = (float)CompareFunction.Always;

    private XRGrabInteractable grab;
    private GameObject panelRoot;
    private CanvasGroup group;
    private Transform head;
    private bool hasShown;
    private Coroutine routine;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        BuildPanel();
        panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (panelRoot != null) Destroy(panelRoot);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (showOnlyOnce && hasShown) return;
        hasShown = true;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine());
    }

    /// <summary>Show the message now (e.g. from another script), ignoring Show Only Once.</summary>
    public void ShowNow()
    {
        if (!isActiveAndEnabled) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        head = ResolveHead();
        if (head == null)
        {
            Debug.LogWarning("[BatPickupMessage] No Main Camera found; cannot place the message.", this);
            yield break;
        }

        SnapToTarget();
        group.alpha = 0f;
        panelRoot.SetActive(true);
        if (showClip != null) AudioSource.PlayClipAtPoint(showClip, head.position, showVolume);
        if (debugLogs) Debug.Log("[BatPickupMessage] Showing message.", this);

        yield return Fade(0f, 1f, fadeInSeconds);

        float t = 0f;
        while (t < showSeconds)
        {
            Follow();
            t += Time.deltaTime;
            yield return null;
        }

        yield return Fade(1f, 0f, fadeOutSeconds);
        panelRoot.SetActive(false);
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

    private void TargetPose(out Vector3 pos, out Quaternion rot)
    {
        Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (fwd.sqrMagnitude < 0.0001f) fwd = head.forward;
        fwd.Normalize();
        pos = head.position + fwd * distance + Vector3.up * heightOffset;
        // Canvas faces the viewer when its forward points away from them.
        rot = Quaternion.LookRotation(pos - head.position, Vector3.up);
    }

    private void SnapToTarget()
    {
        TargetPose(out Vector3 pos, out Quaternion rot);
        panelRoot.transform.SetPositionAndRotation(pos, rot);
    }

    private void Follow()
    {
        if (head == null) return;
        TargetPose(out Vector3 pos, out Quaternion rot);
        float k = followSmoothing <= 0f ? 1f : 1f - Mathf.Exp(-followSmoothing * Time.deltaTime);
        Transform tr = panelRoot.transform;
        tr.SetPositionAndRotation(Vector3.Lerp(tr.position, pos, k), Quaternion.Slerp(tr.rotation, rot, k));
    }

    private Transform ResolveHead()
    {
        if (head != null) return head;
        return Camera.main != null ? Camera.main.transform : null;
    }

    // ---------- UI construction ----------

    private void BuildPanel()
    {
        panelRoot = new GameObject("BatPickupMessage_UI");
        var canvas = panelRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        var rect = panelRoot.GetComponent<RectTransform>();
        rect.sizeDelta = panelSize;
        panelRoot.transform.localScale = Vector3.one * worldScale;

        group = panelRoot.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Material uiOnTop = new Material(Shader.Find("UI/Default"));
        uiOnTop.SetFloat("unity_GUIZTestMode", ZTestAlways);

        // Background
        var bg = CreateImage("Background", panelRoot.transform, backgroundColor, uiOnTop);
        Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Red accent bars top and bottom
        var top = CreateImage("TopBar", panelRoot.transform, titleColor, uiOnTop);
        Stretch(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), Vector2.zero);
        var bottom = CreateImage("BottomBar", panelRoot.transform, new Color(titleColor.r, titleColor.g, titleColor.b, 0.6f), uiOnTop);
        Stretch(bottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 4f));

        // Title
        var titleText = CreateText("Title", panelRoot.transform, title, 78f, titleColor, FontStyles.Bold);
        titleText.characterSpacing = 18f;
        Stretch(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -120f), new Vector2(-30f, -20f));

        // Body
        var bodyText = CreateText("Body", panelRoot.transform, body, 38f, bodyColor, FontStyles.Normal);
        bodyText.lineSpacing = 8f;
        Stretch(bodyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(40f, 25f), new Vector2(-40f, -125f));
    }

    private static Image CreateImage(string name, Transform parent, Color color, Material mat)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.material = mat;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        // Draw on top of walls/fog so it is always readable
        tmp.fontMaterial.SetFloat("unity_GUIZTestMode", ZTestAlways);
        return tmp;
    }

    private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
