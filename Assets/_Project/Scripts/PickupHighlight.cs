using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// "Pick me up" highlight for a grabbable object: a soft pulsing glow outline around the mesh
/// plus a small pulsing light that lights up the object and the wall behind it.
/// Disappears permanently the first time the player grabs the object.
/// Put it on the same object as the XRGrabInteractable (e.g. the bat).
/// </summary>
[DisallowMultipleComponent]
public class PickupHighlight : MonoBehaviour
{
    [Header("Outline Glow")]
    [Tooltip("Additive, front-culled transparent material (Highlight_Outline.mat).")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Color glowColor = new Color(1f, 0.72f, 0.3f, 1f);
    [Tooltip("How much bigger the glow shell is than the object (1.06 = 6% bigger).")]
    [SerializeField, Range(1.01f, 1.3f)] private float outlineScale = 1.06f;
    [SerializeField] private Vector2 glowAlphaRange = new Vector2(0.15f, 0.7f);

    [Header("Pulsing Light")]
    [SerializeField] private bool addLight = true;
    [SerializeField] private Color lightColor = new Color(1f, 0.7f, 0.35f);
    [SerializeField, Min(0.1f)] private float lightRange = 1.6f;
    [SerializeField] private Vector2 lightIntensityRange = new Vector2(0.4f, 2.2f);

    [Header("Pulse")]
    [Tooltip("Pulses per second.")]
    [SerializeField, Min(0.05f)] private float pulseSpeed = 0.8f;

    [Header("Behaviour")]
    [Tooltip("Remove the highlight forever after the first grab.")]
    [SerializeField] private bool hideAfterFirstGrab = true;
    [SerializeField] private bool debugLogs = false;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private XRGrabInteractable grab;
    private Renderer[] outlineRenderers;
    private Light glowLight;
    private MaterialPropertyBlock mpb;
    private bool active = true;

    public bool IsActive => active;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        mpb = new MaterialPropertyBlock();
        Build();
    }

    private void OnEnable()
    {
        if (grab != null) grab.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDisable()
    {
        if (grab != null) grab.selectEntered.RemoveListener(OnGrabbed);
    }

private void Update()
    {
        if (!active) return;

        // Recreate the runtime-only block after a domain reload before applying
        // the pulse to any merged-scene highlight renderers.
        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        float t = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f);
        float eased = t * t * (3f - 2f * t);

        if (outlineRenderers != null)
        {
            Color c = glowColor;
            c.a = Mathf.Lerp(glowAlphaRange.x, glowAlphaRange.y, eased);
            mpb.SetColor(BaseColorId, c);
            foreach (var r in outlineRenderers)
                if (r != null) r.SetPropertyBlock(mpb);
        }

        if (glowLight != null)
            glowLight.intensity = Mathf.Lerp(lightIntensityRange.x, lightIntensityRange.y, eased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (!active) return;
        if (hideAfterFirstGrab) RemoveHighlight();
        else SetVisible(false);
    }

    /// <summary>Turn the highlight off permanently.</summary>
    public void RemoveHighlight()
    {
        active = false;
        if (outlineRenderers != null)
            foreach (var r in outlineRenderers) if (r != null) Destroy(r.gameObject);
        if (glowLight != null) Destroy(glowLight.gameObject);
        outlineRenderers = null;
        glowLight = null;
        if (debugLogs) Debug.Log("[PickupHighlight] Removed after first grab.", this);
    }

    private void SetVisible(bool visible)
    {
        if (outlineRenderers != null)
            foreach (var r in outlineRenderers) if (r != null) r.enabled = visible;
        if (glowLight != null) glowLight.enabled = visible;
    }

    private void Build()
    {
        // Glow shells: a copy of each visible mesh, scaled up around its own centre
        if (outlineMaterial != null)
        {
            var filters = GetComponentsInChildren<MeshFilter>(true);
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var mf in filters)
            {
                var src = mf.GetComponent<MeshRenderer>();
                if (mf.sharedMesh == null || src == null || !src.enabled) continue;

                var go = new GameObject("Highlight_Outline");
                go.transform.SetParent(mf.transform, false);
                Vector3 c = mf.sharedMesh.bounds.center;
                go.transform.localScale = Vector3.one * outlineScale;
                go.transform.localPosition = c * (1f - outlineScale); // scale around the mesh centre

                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var mr = go.AddComponent<MeshRenderer>();
                var mats = new Material[mf.sharedMesh.subMeshCount];
                for (int i = 0; i < mats.Length; i++) mats[i] = outlineMaterial;
                mr.sharedMaterials = mats;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                list.Add(mr);
            }
            outlineRenderers = list.ToArray();
            if (debugLogs) Debug.Log($"[PickupHighlight] Built {outlineRenderers.Length} glow shell(s).", this);
        }
        else
        {
            Debug.LogWarning("[PickupHighlight] No outline material assigned; only the light will pulse.", this);
        }

        // Pulsing point light at the object's visual centre
        if (addLight)
        {
            Bounds b = new Bounds(transform.position, Vector3.zero);
            bool first = true;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject.name == "Highlight_Outline" || !r.enabled) continue;
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            var lgo = new GameObject("Highlight_Light");
            lgo.transform.SetParent(transform, true);
            lgo.transform.position = b.center;
            glowLight = lgo.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = lightColor;
            glowLight.range = lightRange;
            glowLight.intensity = lightIntensityRange.x;
            glowLight.shadows = LightShadows.None;
        }
    }
}
