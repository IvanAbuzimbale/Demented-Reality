using UnityEngine;
using UnityEngine.UI;

namespace DementedReality.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    public sealed class DR_BlurredBackground : MonoBehaviour
    {
        [Header("Output")]
        [Tooltip("RawImage that displays the blurred capture. Usually a full-screen child of the popup panel.")]
        [SerializeField] private RawImage targetImage;

        [Header("Source")]
        [Tooltip("World camera captured. Auto = Camera.main.")]
        [SerializeField] private Camera sourceCamera;

        [Header("Blur")]
        [Tooltip("Material using Hidden/DR/KawaseBlur (or any compatible blur shader).")]
        [SerializeField] private Material blurMaterial;
        [SerializeField, Range(1, 16)] private int iterations = 4;
        [SerializeField, Range(1, 8)] private int downsample = 2;
        [SerializeField, Range(0.1f, 4f)] private float baseOffset = 0.5f;
        [SerializeField, Range(0f, 1f)] private float offsetGrowthPerIteration = 0.5f;

        private RenderTexture finalRT;

        private void Awake()
        {
            if (sourceCamera == null) sourceCamera = Camera.main;
        }

        public void Refresh()
        {
            Camera cam = sourceCamera != null ? sourceCamera : Camera.main;
            if (cam == null)
            {
                Debug.LogError("[DR_BlurredBackground] No source camera assigned and Camera.main is null.", this);
                return;
            }
            if (blurMaterial == null)
            {
                Debug.LogError("[DR_BlurredBackground] Blur material not assigned.", this);
                return;
            }

            int w = Mathf.Max(1, Screen.width / downsample);
            int h = Mathf.Max(1, Screen.height / downsample);

            RenderTexture cap = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.Default);
            RenderTexture a = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.Default);
            RenderTexture b = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.Default);

            RenderTexture prev = cam.targetTexture;
            cam.targetTexture = cap;
            cam.Render();
            cam.targetTexture = prev;

            Graphics.Blit(cap, a);

            for (int i = 0; i < iterations; i++)
            {
                blurMaterial.SetFloat("_Offset", baseOffset + i * offsetGrowthPerIteration);
                Graphics.Blit(a, b, blurMaterial);
                (a, b) = (b, a);
            }

            if (finalRT == null || finalRT.width != w || finalRT.height != h)
            {
                ReleaseFinalRT();
                finalRT = new RenderTexture(w, h, 0, RenderTextureFormat.Default)
                {
                    name = "DR_BlurredBackgroundRT",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                finalRT.Create();
            }
            Graphics.Blit(a, finalRT);

            if (targetImage != null)
            {
                targetImage.texture = finalRT;
                targetImage.enabled = true;
            }

            RenderTexture.ReleaseTemporary(cap);
            RenderTexture.ReleaseTemporary(a);
            RenderTexture.ReleaseTemporary(b);
        }

        public void Clear()
        {
            if (targetImage != null)
            {
                targetImage.texture = null;
                targetImage.enabled = false;
            }
        }

        private void OnDestroy()
        {
            ReleaseFinalRT();
        }

        private void ReleaseFinalRT()
        {
            if (finalRT == null) return;
            finalRT.Release();
            Destroy(finalRT);
            finalRT = null;
        }
    }
}
