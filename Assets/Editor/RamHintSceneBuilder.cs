using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class RamHintSceneBuilder
{
    [MenuItem("Tools/PC Assembly/Build RAM Hint _F10")]
    public static void Build()
    {
        if (Application.isPlaying || Object.FindAnyObjectByType<RamAssemblyHint>() != null)
            return;
        var slots = Object.FindObjectsByType<RamSlotLatches>().OrderBy(slot => slot.name).ToArray();
        if (slots.Length != 4)
            throw new System.InvalidOperationException("Configure all four RAM latch pairs first.");
        var modules = Object.FindObjectsByType<XRGrabInteractable>()
            .Where(module => (module.interactionLayers.value & 2) != 0).ToArray();
        var hintObject = new GameObject("RAM - Orientação de montagem");
        hintObject.transform.position = slots[0].transform.position + new Vector3(-0.03f, 0.25f, 0f);
        hintObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        hintObject.transform.localScale = Vector3.one * 0.1f;
        var labelObject = new GameObject("Instrução", typeof(TextMeshPro));
        labelObject.transform.SetParent(hintObject.transform, false);
        var label = labelObject.GetComponent<TextMeshPro>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        label.fontSize = 2.6f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.93f, 0.97f, 1f);
        label.rectTransform.sizeDelta = new Vector2(5f, 1.2f);
        label.text = "<b>PREPARAR A MEMÓRIA RAM</b>\nAbra as duas travas do slot escolhido.\nFaça pinça nas travas e mova-as para fora.";
        var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(background.GetComponent<Collider>());
        background.name = "Fundo";
        background.transform.SetParent(hintObject.transform, false);
        background.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        background.transform.localScale = new Vector3(5.3f, 1.5f, 1f);
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor", new Color(0.015f, 0.027f, 0.045f));
        const string materialPath = "Assets/Generated/RamLatches/Hint Background.mat";
        var existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (existingMaterial == null)
            AssetDatabase.CreateAsset(material, materialPath);
        else
        {
            Object.DestroyImmediate(material);
            material = existingMaterial;
        }
        background.GetComponent<Renderer>().sharedMaterial = material;
        var hint = hintObject.AddComponent<RamAssemblyHint>();
        var serialized = new SerializedObject(hint);
        serialized.FindProperty("m_Label").objectReferenceValue = label;
        SetReferences(serialized.FindProperty("m_Slots"), slots);
        SetReferences(serialized.FindProperty("m_Modules"), modules);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(hintObject.scene);
        EditorSceneManager.SaveScene(hintObject.scene);
    }

    static void SetReferences(SerializedProperty property, Object[] references)
    {
        property.arraySize = references.Length;
        for (var index = 0; index < references.Length; index++)
            property.GetArrayElementAtIndex(index).objectReferenceValue = references[index];
    }
}
