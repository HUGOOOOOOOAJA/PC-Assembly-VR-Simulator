using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class RamLatchSceneBuilder
{
    const string AssetFolder = "Assets/Generated/RamLatches";
    const string GroupName = "RAM_Latches";
    const string UndoName = "Build RAM Latches";
    const float DeckHeight = 0.097981f;
    const float LeftEnd = -0.0958f;
    const float RightEnd = 1.6974f;
    const float LeftPivot = -0.140457f;
    const float RightPivot = 1.73997f;

    static readonly float[] SlotCenters = { 1.11508f, 0.991155f, 0.866493f, 0.742568f };

    [MenuItem("Tools/PC Assembly/Build RAM Latches")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build RAM latches outside Play mode.");

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
            throw new InvalidOperationException("Open the saved motherboard scene before building RAM latches.");

        var boards = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(item => item.name == "MSI_Z790")
            .ToArray();
        if (boards.Length != 1)
            throw new InvalidOperationException("The active scene must contain exactly one MSI_Z790 motherboard.");

        var motherboard = boards[0];
        if (motherboard.GetComponentsInChildren<RamLatch>(true).Length != 0 ||
            motherboard.GetComponentsInChildren<RamSlotLatches>(true).Length != 0 ||
            motherboard.Find(GroupName) != null)
        {
            Debug.LogWarning("RAM latches already exist. No objects or assets were changed.", motherboard);
            return;
        }

        var sockets = FindSockets(motherboard);
        var meshFilters = motherboard.GetComponentsInChildren<MeshFilter>(true)
            .Where(item => item.sharedMesh != null && FindDdr5Submesh(item) >= 0)
            .ToArray();
        if (meshFilters.Length != 1)
            throw new InvalidOperationException("Expected one motherboard mesh with the original DDR5 material.");

        var meshFilter = meshFilters[0];
        var sourcePath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
        if (!sourcePath.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The motherboard must still reference its imported OBJ mesh.");

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("The URP Lit shader is required for the latch material.");

        var boardMesh = CreateBoardMesh(motherboard, meshFilter, FindDdr5Submesh(meshFilter));
        var handleMesh = CreateHandleMesh();
        var material = new Material(shader) { name = "RAM Latch Dark Polymer" };
        material.SetColor("_BaseColor", new Color(0.12f, 0.13f, 0.145f, 1f));
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.22f);

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);
        var createdAssets = new List<string>();
        GameObject latchGroup = null;
        var groupRegistered = false;

        try
        {
            EnsureAssetFolder();
            SaveAsset(boardMesh, "MSI_Z790_RamLatchSlots.asset", createdAssets);
            SaveAsset(handleMesh, "RamLatchHandle.asset", createdAssets);
            SaveAsset(material, "RamLatchDarkPolymer.mat", createdAssets);

            latchGroup = new GameObject(GroupName);
            latchGroup.transform.SetParent(motherboard, false);

            for (var slotIndex = 0; slotIndex < sockets.Length; slotIndex++)
            {
                var socket = sockets[slotIndex];
                var leftLatch = CreateLatch(latchGroup.transform, socket, slotIndex, -1f, handleMesh, material);
                var rightLatch = CreateLatch(latchGroup.transform, socket, slotIndex, 1f, handleMesh, material);
                var slotLatches = Undo.AddComponent<RamSlotLatches>(socket.gameObject);
                var serializedSlot = new SerializedObject(slotLatches);
                SetReference(serializedSlot, "m_LeftLatch", leftLatch);
                SetReference(serializedSlot, "m_RightLatch", rightLatch);
                SetReference(serializedSlot, "m_Socket", socket);
                serializedSlot.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(slotLatches);
            }

            Undo.RegisterCreatedObjectUndo(latchGroup, UndoName);
            groupRegistered = true;
            Undo.RecordObject(meshFilter, UndoName);
            meshFilter.sharedMesh = boardMesh;
            PrefabUtility.RecordPrefabInstancePropertyModifications(meshFilter);

            ValidateBuild(latchGroup, sockets);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("The scene could not be saved after creating the RAM latches.");

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = motherboard.gameObject;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();
            Debug.Log("Created and saved eight hinged RAM latches for the four existing RAM sockets.", motherboard);
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);
            if (!groupRegistered && latchGroup != null)
                UnityEngine.Object.DestroyImmediate(latchGroup);
            foreach (var assetPath in createdAssets)
                AssetDatabase.DeleteAsset(assetPath);
            if (boardMesh != null && !AssetDatabase.Contains(boardMesh))
                UnityEngine.Object.DestroyImmediate(boardMesh);
            if (handleMesh != null && !AssetDatabase.Contains(handleMesh))
                UnityEngine.Object.DestroyImmediate(handleMesh);
            if (material != null && !AssetDatabase.Contains(material))
                UnityEngine.Object.DestroyImmediate(material);
            throw;
        }
    }

    static XRSocketInteractor[] FindSockets(Transform motherboard)
    {
        var sockets = motherboard.GetComponentsInChildren<XRSocketInteractor>(true)
            .Where(item => item.name.StartsWith("RAM_SLOT_", StringComparison.Ordinal))
            .ToArray();
        if (sockets.Length != SlotCenters.Length)
            throw new InvalidOperationException("The motherboard must contain its four original RAM sockets.");

        var ordered = new XRSocketInteractor[SlotCenters.Length];
        for (var slotIndex = 0; slotIndex < ordered.Length; slotIndex++)
        {
            var matches = sockets.Where(item => item.name == $"RAM_SLOT_{slotIndex + 1}").ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException($"Expected one RAM_SLOT_{slotIndex + 1} socket.");
            var socket = matches[0];
            var localPosition = motherboard.InverseTransformPoint(socket.transform.position);
            if (Mathf.Abs(localPosition.y - SlotCenters[slotIndex]) > 0.002f ||
                Mathf.Abs(localPosition.x - 0.82f) > 0.002f ||
                Mathf.Abs(localPosition.z - 0.098f) > 0.002f || socket.attachTransform == null)
                throw new InvalidOperationException($"{socket.name} no longer matches the measured motherboard slot.");
            ordered[slotIndex] = socket;
        }
        return ordered;
    }

    static int FindDdr5Submesh(MeshFilter meshFilter)
    {
        var renderer = meshFilter.GetComponent<MeshRenderer>();
        if (renderer == null)
            return -1;
        var materials = renderer.sharedMaterials;
        if (materials.Length != meshFilter.sharedMesh.subMeshCount)
            return -1;
        var matches = Enumerable.Range(0, materials.Length)
            .Where(index => materials[index] != null && materials[index].name == "DDR5")
            .ToArray();
        return matches.Length == 1 ? matches[0] : -1;
    }

    static Mesh CreateBoardMesh(Transform motherboard, MeshFilter meshFilter, int ddr5Submesh)
    {
        var source = meshFilter.sharedMesh;
        if (source.GetTopology(ddr5Submesh) != MeshTopology.Triangles)
            throw new InvalidOperationException("The DDR5 submesh must use triangle geometry.");

        var vertices = source.vertices;
        var normals = source.normals;
        var triangles = source.GetTriangles(ddr5Submesh);
        var toBoard = motherboard.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
        var toMesh = meshFilter.transform.worldToLocalMatrix * motherboard.localToWorldMatrix;
        var changed = new HashSet<int>();
        var foundEnds = new bool[SlotCenters.Length, 2];

        foreach (var vertexIndex in triangles)
        {
            var position = toBoard.MultiplyPoint3x4(vertices[vertexIndex]);
            if (position.z <= DeckHeight || position.x >= LeftEnd && position.x <= RightEnd)
                continue;
            var endIndex = position.x < LeftEnd ? 0 : 1;
            var slotIndex = Array.FindIndex(SlotCenters, center => Mathf.Abs(center - position.y) < 0.05f);
            if (slotIndex < 0)
                throw new InvalidOperationException("DDR5 end geometry does not match the four measured slots.");
            foundEnds[slotIndex, endIndex] = true;
            position.z = DeckHeight;
            vertices[vertexIndex] = toMesh.MultiplyPoint3x4(position);
            changed.Add(vertexIndex);
        }

        for (var slotIndex = 0; slotIndex < SlotCenters.Length; slotIndex++)
        {
            if (!foundEnds[slotIndex, 0] || !foundEnds[slotIndex, 1])
                throw new InvalidOperationException($"Could not isolate both original latch caps for RAM_SLOT_{slotIndex + 1}.");
        }
        for (var submeshIndex = 0; submeshIndex < source.subMeshCount; submeshIndex++)
        {
            if (submeshIndex != ddr5Submesh && source.GetIndices(submeshIndex).Any(changed.Contains))
                throw new InvalidOperationException("DDR5 caps share vertices with other motherboard parts.");
        }

        var normalSums = new Dictionary<int, Vector3>();
        for (var triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex += 3)
        {
            var first = triangles[triangleIndex];
            var second = triangles[triangleIndex + 1];
            var third = triangles[triangleIndex + 2];
            var normal = Vector3.Cross(vertices[second] - vertices[first], vertices[third] - vertices[first]);
            foreach (var vertexIndex in new[] { first, second, third })
            {
                normalSums.TryGetValue(vertexIndex, out var sum);
                normalSums[vertexIndex] = sum + normal;
            }
        }
        if (normals.Length == vertices.Length)
        {
            foreach (var vertexIndex in changed)
            {
                if (normalSums[vertexIndex].sqrMagnitude > 0.0000000001f)
                    normals[vertexIndex] = normalSums[vertexIndex].normalized;
            }
        }

        var mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = "MSI_Z790 with RAM latch bases";
        mesh.vertices = vertices;
        if (normals.Length == vertices.Length)
            mesh.normals = normals;
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh CreateHandleMesh()
    {
        var profile = new[]
        {
            new Vector2(-0.0425f, -0.008f),
            new Vector2(0.0425f, -0.008f),
            new Vector2(0.0425f, 0.092f),
            new Vector2(0.032f, 0.152f),
            new Vector2(-0.032f, 0.152f),
            new Vector2(-0.0425f, 0.092f)
        };
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var front = profile.Select(point => new Vector3(point.x, -0.0375f, point.y)).ToArray();
        var back = profile.Select(point => new Vector3(point.x, 0.0375f, point.y)).ToArray();
        AddFace(vertices, triangles, front);
        AddFace(vertices, triangles, back.Reverse().ToArray());
        for (var profileIndex = 0; profileIndex < profile.Length; profileIndex++)
        {
            var nextIndex = (profileIndex + 1) % profile.Length;
            AddFace(vertices, triangles, new[] { front[profileIndex], back[profileIndex], back[nextIndex], front[nextIndex] });
        }
        var mesh = new Mesh { name = "RAM Latch Tapered Handle" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void AddFace(List<Vector3> vertices, List<int> triangles, Vector3[] face)
    {
        var startIndex = vertices.Count;
        vertices.AddRange(face);
        for (var cornerIndex = 1; cornerIndex < face.Length - 1; cornerIndex++)
        {
            triangles.Add(startIndex);
            triangles.Add(startIndex + cornerIndex);
            triangles.Add(startIndex + cornerIndex + 1);
        }
    }

    static RamLatch CreateLatch(Transform parent, XRSocketInteractor socket, int slotIndex,
        float direction, Mesh mesh, Material material)
    {
        var side = direction < 0f ? "Left" : "Right";
        var pivot = new GameObject($"RAM_Latch_{slotIndex + 1}_{side}");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = new Vector3(direction < 0f ? LeftPivot : RightPivot,
            SlotCenters[slotIndex], DeckHeight);

        var movingPart = new GameObject("Handle");
        movingPart.transform.SetParent(pivot.transform, false);
        movingPart.AddComponent<MeshFilter>().sharedMesh = mesh;
        movingPart.AddComponent<MeshRenderer>().sharedMaterial = material;
        var collider = movingPart.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0f, 0.125f);
        collider.size = new Vector3(0.085f, 0.075f, 0.054f);

        var grip = new GameObject("Grip");
        grip.transform.SetParent(movingPart.transform, false);
        grip.transform.localPosition = new Vector3(0f, 0f, 0.13f);

        var latch = pivot.AddComponent<RamLatch>();
        latch.interactionManager = socket.interactionManager;
        latch.interactionLayers = 1;
        latch.selectMode = InteractableSelectMode.Single;
        latch.colliders.Clear();
        latch.colliders.Add(collider);
        var serializedLatch = new SerializedObject(latch);
        SetReference(serializedLatch, "m_MovingPart", movingPart.transform);
        SetReference(serializedLatch, "m_Grip", grip.transform);
        SetReference(serializedLatch, "m_Socket", socket);
        serializedLatch.FindProperty("m_OpenDirection").floatValue = direction;
        serializedLatch.FindProperty("m_OpenAngle").floatValue = 55f;
        serializedLatch.FindProperty("m_Angle").floatValue = 0f;
        serializedLatch.ApplyModifiedPropertiesWithoutUndo();
        return latch;
    }

    static void SetReference(SerializedObject target, string field, UnityEngine.Object value)
    {
        var property = target.FindProperty(field);
        if (property == null)
            throw new InvalidOperationException($"{target.targetObject.GetType().Name} has no serialized {field} field.");
        property.objectReferenceValue = value;
    }

    static void EnsureAssetFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
            AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(AssetFolder))
            AssetDatabase.CreateFolder("Assets/Generated", "RamLatches");
    }

    static void SaveAsset(UnityEngine.Object asset, string fileName, List<string> createdAssets)
    {
        var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{AssetFolder}/{fileName}");
        AssetDatabase.CreateAsset(asset, assetPath);
        createdAssets.Add(assetPath);
    }

    static void ValidateBuild(GameObject latchGroup, XRSocketInteractor[] sockets)
    {
        var latches = latchGroup.GetComponentsInChildren<RamLatch>(true);
        if (latches.Length != 8 || latchGroup.GetComponentsInChildren<MeshRenderer>(true).Length != 8 ||
            latchGroup.GetComponentsInChildren<BoxCollider>(true).Length != 8)
            throw new InvalidOperationException("The latch build did not create eight complete handles.");

        foreach (var socket in sockets)
        {
            var slotLatches = socket.GetComponent<RamSlotLatches>();
            var serializedSlot = new SerializedObject(slotLatches);
            if (serializedSlot.FindProperty("m_LeftLatch").objectReferenceValue == null ||
                serializedSlot.FindProperty("m_RightLatch").objectReferenceValue == null ||
                serializedSlot.FindProperty("m_Socket").objectReferenceValue != socket)
                throw new InvalidOperationException($"{socket.name} is missing its latch connections.");
        }
    }
}
