using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Hands;

public static class PhysicalHandsSceneBuilder
{
    [MenuItem("Tools/PC Assembly/Configure Physical Hands #F8")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var drivers = Object.FindObjectsByType<XRHandSkeletonDriver>(FindObjectsInactive.Include)
            .Where(d => d.gameObject.scene.IsValid() && d.name.Contains("QuestVisual") && d.GetComponent<XRHandMeshController>() != null).ToArray();
        if (drivers.Length != 2) { Debug.LogError("Expected exactly two Quest hand visuals in the current scene."); return; }
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        var layer = LayerMask.NameToLayer("Physical Hands");
        if (layer < 0)
        {
            for (var i = 8; i < 32; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { layer = i; break; }
            if (layer < 0) { Debug.LogError("No free physics layer for hands."); return; }
            layers.GetArrayElementAtIndex(layer).stringValue = "Physical Hands";
            tags.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (var driver in drivers)
        {
            var handName = driver.handTrackingEvents.handedness == Handedness.Left ? "Left Hand" : "Right Hand";
            var interactionRoot = driver.transform.root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == handName);
            if (interactionRoot == null) { Debug.LogError("Missing interaction root " + handName); return; }
            var controller = driver.GetComponent<PhysicalHandController>();
            if (controller == null) controller = Undo.AddComponent<PhysicalHandController>(driver.gameObject);
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("m_Driver").objectReferenceValue = driver;
            serialized.FindProperty("m_MeshController").objectReferenceValue = driver.GetComponent<XRHandMeshController>();
            serialized.FindProperty("m_InteractionRoot").objectReferenceValue = interactionRoot;
            serialized.FindProperty("m_PhysicsLayer").intValue = layer;
            serialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
            EditorUtility.SetDirty(controller);
        }
        var count = 0;
        foreach (var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            if (component == null || !component.gameObject.scene.IsValid()) continue;
            var serialized = new SerializedObject(component);
            var changed = false;
            foreach (var name in new[] { "m_RaycastMask", "m_PhysicsLayerMask", "m_BlockingMask" })
            {
                var mask = serialized.FindProperty(name);
                if (mask == null || mask.propertyType != SerializedPropertyType.LayerMask || (mask.intValue & (1 << layer)) == 0) continue;
                Undo.RecordObject(component, "Exclude physical hands from interaction queries");
                mask.intValue &= ~(1 << layer);
                changed = true;
                count++;
            }
            if (!changed) continue;
            serialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorUtility.SetDirty(component);
        }
        EditorSceneManager.MarkSceneDirty(drivers[0].gameObject.scene);
        foreach (var coordinator in Object.FindObjectsByType<RamPressCoordinator>(FindObjectsInactive.Include))
        {
            var serialized = new SerializedObject(coordinator);
            serialized.FindProperty("m_UsePhysicalContacts").boolValue = true;
            var hands = serialized.FindProperty("m_PhysicalHands");
            hands.arraySize = drivers.Length;
            for (var i = 0; i < drivers.Length; i++) hands.GetArrayElementAtIndex(i).objectReferenceValue = drivers[i].GetComponent<PhysicalHandController>();
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(coordinator);
        }
        EditorSceneManager.SaveScene(drivers[0].gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Configured two physical hands; excluded their layer from " + count + " interaction masks. Tracking and ray origins preserved.");
    }
}
