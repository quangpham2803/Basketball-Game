using UnityEngine;
using UnityEditor;

/// <summary>
/// Converts all materials in MarpaStudio/Built-In/Materials to URP/Lit shader.
/// Menu: Basketball > Convert Materials to URP
/// </summary>
public class MaterialConverter : Editor
{
    [MenuItem("Basketball/Convert Materials to URP")]
    public static void ConvertAllMaterials()
    {
        string folder = "Assets/MarpaStudio/Built-In/Materials";
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("URP Lit shader not found! Make sure URP package is installed.");
            return;
        }

        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Skip if already URP/Lit
            if (mat.shader == urpLit) continue;

            // Save old property values before switching shader
            Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            float bumpScale = mat.HasProperty("_BumpScale") ? mat.GetFloat("_BumpScale") : 1f;
            Texture metallicMap = mat.HasProperty("_MetallicGlossMap") ? mat.GetTexture("_MetallicGlossMap") : null;
            float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
            float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
            Texture occlusionMap = mat.HasProperty("_OcclusionMap") ? mat.GetTexture("_OcclusionMap") : null;
            float occlusionStrength = mat.HasProperty("_OcclusionStrength") ? mat.GetFloat("_OcclusionStrength") : 1f;
            Texture emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
            Color emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;

            // Switch shader
            mat.shader = urpLit;

            // Remap properties to URP names
            if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
            mat.SetColor("_BaseColor", color);

            if (bumpMap != null)
            {
                mat.SetTexture("_BumpMap", bumpMap);
                mat.SetFloat("_BumpScale", bumpScale);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (metallicMap != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallicMap);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            if (occlusionMap != null)
            {
                mat.SetTexture("_OcclusionMap", occlusionMap);
                mat.SetFloat("_OcclusionStrength", occlusionStrength);
            }

            if (emissionMap != null || emissionColor != Color.black)
            {
                mat.SetTexture("_EmissionMap", emissionMap);
                mat.SetColor("_EmissionColor", emissionColor);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            EditorUtility.SetDirty(mat);
            count++;
            Debug.Log($"Converted: {path}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"<b>Done!</b> Converted {count} materials to URP/Lit.");
    }
}
