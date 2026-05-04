using TMPro;
using UnityEditor;
using UnityEngine;

namespace LotteryMachine.EditorTools
{
    public static partial class LotteryMachineSampleBuilder
    {
        private static Material CreateMaterial(string name, Color color, Shader shader, float metallic = 0f, float smoothness = 0.35f)
        {
            var path = MaterialsRoot + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (color.a < 1f)
            {
                if (material.HasProperty("_Surface"))
                {
                    material.SetFloat("_Surface", 1f);
                }

                if (material.HasProperty("_AlphaClip"))
                {
                    material.SetFloat("_AlphaClip", 0f);
                }

                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateParticleMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            var path = MaterialsRoot + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateTextMeshProMaterial(string name, Color faceColor, Color outlineColor, float outlineWidth)
        {
            var defaultFontAsset = TMP_Settings.defaultFontAsset;
            if (defaultFontAsset == null || defaultFontAsset.material == null)
            {
                return null;
            }

            var path = MaterialsRoot + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(defaultFontAsset.material);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = defaultFontAsset.material.shader;
            material.EnableKeyword("OUTLINE_ON");

            if (material.HasProperty("_MainTex") && defaultFontAsset.atlasTexture != null)
            {
                material.SetTexture("_MainTex", defaultFontAsset.atlasTexture);
            }

            if (material.HasProperty("_FaceColor"))
            {
                material.SetColor("_FaceColor", faceColor);
            }

            if (material.HasProperty("_OutlineColor"))
            {
                material.SetColor("_OutlineColor", outlineColor);
            }

            if (material.HasProperty("_OutlineWidth"))
            {
                material.SetFloat("_OutlineWidth", outlineWidth);
            }

            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
