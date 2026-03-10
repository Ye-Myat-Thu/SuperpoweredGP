using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class MoveIcon : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 0.35f;

    [Header("Scale")]
    [SerializeField] private Vector3 startScale = Vector3.one;
    [SerializeField] private Vector3 endScale = Vector3.zero;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Fade")]
    [SerializeField] private bool fadeOut = false;
    [SerializeField] private MeshRenderer[] meshRenderers;

    private float timer;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        if (meshRenderers == null || meshRenderers.Length == 0)
            meshRenderers = GetComponentsInChildren<MeshRenderer>();

        mpb = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        timer = 0f;
        transform.localScale = startScale;
        SetAlpha(1f);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / Mathf.Max(lifeTime, 0.0001f));

        float curveT = scaleCurve.Evaluate(t);
        transform.localScale = Vector3.LerpUnclamped(startScale, endScale, curveT);

        if (fadeOut)
        {
            SetAlpha(1f - t);
        }

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void SetAlpha(float alpha)
    {
        if (meshRenderers == null) return;

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            var mr = meshRenderers[i];
            if (mr == null) continue;

            mr.GetPropertyBlock(mpb);

            // Works if shader uses _Color
            if (mr.sharedMaterial != null && mr.sharedMaterial.HasProperty("_Color"))
            {
                Color baseColor = mr.sharedMaterial.color;
                baseColor.a = alpha;
                mpb.SetColor("_Color", baseColor);
            }

            // Works if shader uses _BaseColor (common in URP/HDRP)
            if (mr.sharedMaterial != null && mr.sharedMaterial.HasProperty("_BaseColor"))
            {
                Color baseColor = mr.sharedMaterial.GetColor("_BaseColor");
                baseColor.a = alpha;
                mpb.SetColor("_BaseColor", baseColor);
            }

            mr.SetPropertyBlock(mpb);
        }
    }
}
