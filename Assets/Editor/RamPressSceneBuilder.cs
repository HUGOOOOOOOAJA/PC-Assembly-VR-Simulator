using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.XR.CoreUtils;
using TMPro;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class RamPressSceneBuilder
{
    const string RootName = "RAM - Instalação por pressão";

    [MenuItem("Tools/PC Assembly/Configure RAM Press _F5")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Configure RAM pressure outside Play.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
            throw new InvalidOperationException("Open the saved assembly scene first.");
        var sockets = Object.FindObjectsByType<XRSocketInteractor>().Where(item => item.name.StartsWith("RAM_SLOT_")).OrderBy(item => item.name).ToArray();
        var modules = Object.FindObjectsByType<XRGrabInteractable>().Where(item => (item.interactionLayers.value & 2) != 0).OrderBy(item => item.name).ToArray();
        var origins = Object.FindObjectsByType<XROrigin>();
        if (sockets.Length != 4 || modules.Length != 4 || origins.Length != 1 || origins[0].CameraFloorOffsetObject == null ||
            sockets.Any(item => item.attachTransform == null || item.GetComponent<RamSlotLatches>() == null))
            throw new InvalidOperationException("Expected four configured RAM slots/modules and one XR tracking origin.");
        if (Object.FindAnyObjectByType<RamPressCoordinator>() != null)
        {
            if (sockets.Any(item => item.GetComponent<RamPressSlot>() == null) || modules.Any(item => item.GetComponent<RamInstallationLock>() == null))
                throw new InvalidOperationException("Existing RAM pressure setup is incomplete; no objects changed.");
            Debug.Log("RAM pressure setup already exists; no duplicate objects created.");
            return;
        }
        Undo.IncrementCurrentGroup();
        var group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure guided RAM pressure");
        try
        {
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create RAM pressure controller");
            var tracking = Undo.AddComponent<RamThumbTracking>(root);
            SetReference(tracking, "m_TrackingSpace", origins[0].CameraFloorOffsetObject.transform);
            var slots = new RamPressSlot[4];
            for (var index = 0; index < sockets.Length; index++)
            {
                slots[index] = Undo.AddComponent<RamPressSlot>(sockets[index].gameObject);
                SetReference(slots[index], "m_Socket", sockets[index]);
                SetReference(slots[index], "m_Latches", sockets[index].GetComponent<RamSlotLatches>());
                SetReference(slots[index], "m_FinalSeat", sockets[index].attachTransform);
                Undo.AddComponent<RamInstallationLock>(modules[index].gameObject);
            }
            var coordinator = Undo.AddComponent<RamPressCoordinator>(root);
            SetReference(coordinator, "m_Tracking", tracking);
            var serialized = new SerializedObject(coordinator);
            var references = serialized.FindProperty("m_Slots");
            references.arraySize = slots.Length;
            for (var index = 0; index < slots.Length; index++)
                references.GetArrayElementAtIndex(index).objectReferenceValue = slots[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("Configured guided RAM pressure for four slots, preserving original seat transforms.");
        }
        catch
        {
            Undo.RevertAllDownToGroup(group);
            throw;
        }
    }

    static void SetReference(Object target, string property, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    static void ConfigureVisuals(RamPressCoordinator coordinator, Vector3 slotPosition)
    {
        var hint = Object.FindAnyObjectByType<RamAssemblyHint>();
        if (hint != null)
        {
            var current = new SerializedObject(hint).FindProperty("m_PressCoordinator").objectReferenceValue;
            if (current != coordinator)
            {
                Undo.RecordObject(hint, "Connect RAM pressure instruction");
                SetReference(hint, "m_PressCoordinator", coordinator);
            }
        }
        if (Object.FindAnyObjectByType<RamPressDemonstration>() != null)
            return;
        if (!AssetDatabase.IsValidFolder("Assets/Generated/RamPressDemo"))
            AssetDatabase.CreateFolder("Assets/Generated", "RamPressDemo");
        var ghost = Material("Thumb Ghost", new Color(0.2f, 0.85f, 1f, 0.55f));
        var nail = Material("Thumb Nail", new Color(0.8f, 0.97f, 1f, 0.8f));
        var dark = Material("Dark Polymer", new Color(0.08f, 0.11f, 0.14f));
        var green = Material("PCB Green", new Color(0.08f, 0.35f, 0.23f));
        var gold = Material("Gold Contacts", new Color(1f, 0.72f, 0.18f));
        var backdrop = Material("Instruction Background", new Color(0.012f, 0.025f, 0.045f));
        var root = new GameObject("RAM - Demonstração dos polegares");
        Undo.RegisterCreatedObjectUndo(root, "Create RAM pressure demonstration");
        root.transform.position = slotPosition + new Vector3(-0.38f, 0.58f, -0.05f);
        var demo = root.AddComponent<RamPressDemonstration>();
        Primitive("Painel", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.025f, 0.06f), new Vector3(0.35f, 0.44f, 0.008f), backdrop);
        Label(root.transform, "Demonstração", "<b>DEMONSTRAÇÃO — MEMÓRIA RAM</b>", new Vector3(0f, 0.205f, -0.02f), new Vector2(0.33f, 0.045f), 0.19f);
        var step = Label(root.transform, "Etapa", "1. UM POLEGAR EM CADA PONTA", new Vector3(0f, 0.16f, -0.02f), new Vector2(0.33f, 0.045f), 0.16f);
        Label(root.transform, "Atenção", "Pressione <b>firme, gradual e uniformemente</b>.\nSe estiver torta ou não encaixar, pare e reposicione. <b>Não force.</b>",
            new Vector3(0f, -0.11f, -0.02f), new Vector2(0.32f, 0.13f), 0.145f);
        var visuals = new GameObject("Exemplo visual — sem interação");
        visuals.transform.SetParent(root.transform, false);
        visuals.transform.localPosition = new Vector3(0f, -0.025f, 0f);
        Primitive("Slot de exemplo", PrimitiveType.Cube, visuals.transform, new Vector3(0f, -0.012f, 0f), new Vector3(0.215f, 0.024f, 0.018f), dark);
        var module = new GameObject("RAM de exemplo").transform;
        module.SetParent(visuals.transform, false);
        module.localPosition = new Vector3(0f, 0.026f, 0f);
        Primitive("Placa", PrimitiveType.Cube, module, Vector3.zero, new Vector3(0.18f, 0.034f, 0.009f), green);
        Primitive("Contatos dourados", PrimitiveType.Cube, module, new Vector3(0f, -0.016f, -0.001f), new Vector3(0.172f, 0.003f, 0.01f), gold);
        for (var index = 0; index < 8; index++)
            Primitive("Chip", PrimitiveType.Cube, module, new Vector3(-0.07f + index * 0.02f, 0.003f, -0.006f), new Vector3(0.016f, 0.016f, 0.003f), dark);
        var thumbs = new Transform[2];
        var latches = new Transform[2];
        for (var index = 0; index < 2; index++)
        {
            var side = index == 0 ? -1f : 1f;
            thumbs[index] = new GameObject(index == 0 ? "Polegar esquerdo" : "Polegar direito").transform;
            thumbs[index].SetParent(visuals.transform, false);
            thumbs[index].localPosition = new Vector3(side * 0.068f, 0.11f, 0f);
            Primitive("Base do polegar", PrimitiveType.Capsule, thumbs[index], new Vector3(0f, 0.025f, 0.002f), new Vector3(0.022f, 0.018f, 0.022f), ghost);
            Primitive("Ponta", PrimitiveType.Sphere, thumbs[index], Vector3.zero, new Vector3(0.021f, 0.028f, 0.022f), ghost);
            Primitive("Unha", PrimitiveType.Sphere, thumbs[index], new Vector3(0f, 0.006f, -0.01f), new Vector3(0.014f, 0.016f, 0.003f), nail);
            Primitive("Base da mão", PrimitiveType.Sphere, thumbs[index], new Vector3(side * 0.018f, 0.055f, 0.006f), new Vector3(0.045f, 0.035f, 0.038f), ghost);
            latches[index] = new GameObject("Trava de exemplo").transform;
            latches[index].SetParent(visuals.transform, false);
            latches[index].localPosition = new Vector3(side * 0.106f, -0.012f, 0f);
            latches[index].localRotation = Quaternion.Euler(0f, 0f, -side * 55f);
            Primitive("Alavanca", PrimitiveType.Cube, latches[index], new Vector3(0f, 0.015f, 0f), new Vector3(0.012f, 0.03f, 0.022f), dark);
        }
        SetReference(demo, "m_Module", module);
        SetReference(demo, "m_Visuals", visuals);
        SetReference(demo, "m_StepLabel", step);
        SetArray(demo, "m_Thumbs", thumbs);
        SetArray(demo, "m_Latches", latches);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
    }

    static void SetArray(Object target, string property, Transform[] values)
    {
        var serialized = new SerializedObject(target);
        var array = serialized.FindProperty(property);
        array.arraySize = values.Length;
        for (var index = 0; index < values.Length; index++)
            array.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var shape = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(shape.GetComponent<Collider>());
        shape.name = name;
        shape.transform.SetParent(parent, false);
        shape.transform.localPosition = position;
        shape.transform.localScale = scale;
        var renderer = shape.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return shape;
    }

    static TMP_Text Label(Transform parent, string name, string text, Vector3 position, Vector2 size, float fontSize)
    {
        var label = new GameObject(name, typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        label.transform.SetParent(parent, false);
        label.transform.localPosition = position;
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = size;
        label.color = new Color(0.91f, 0.97f, 1f);
        label.text = text;
        return label;
    }

    static Material Material(string name, Color color)
    {
        var path = "Assets/Generated/RamPressDemo/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            throw new InvalidOperationException("URP Unlit shader missing.");
        var material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        if (color.a < 1f)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
