using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Clones only transforms and skin. XR events, affordances and interaction keep their original objects.</summary>
public sealed class PhysicalHandVisual : IDisposable
{
    readonly Transform m_Root;
    readonly SkinnedMeshRenderer m_Source;
    readonly bool m_PreviousForceOff;
    readonly IReadOnlyDictionary<Transform, Rigidbody> m_Bodies;
    readonly List<KeyValuePair<Transform, Transform>> m_Pairs = new List<KeyValuePair<Transform, Transform>>();
    readonly MaterialPropertyBlock m_Properties = new MaterialPropertyBlock();
    public SkinnedMeshRenderer Renderer { get; }

    public PhysicalHandVisual(Transform sourceRoot, SkinnedMeshRenderer source, IReadOnlyDictionary<Transform, Rigidbody> bodies)
    {
        m_Source = source;
        m_Bodies = bodies;
        m_PreviousForceOff = source.forceRenderingOff;
        var map = new Dictionary<Transform, Transform>();
        m_Root = Clone(sourceRoot, sourceRoot.parent);
        Renderer = map[source.transform].gameObject.AddComponent<SkinnedMeshRenderer>();
        Renderer.sharedMesh = source.sharedMesh;
        Renderer.sharedMaterials = source.sharedMaterials;
        Renderer.rootBone = source.rootBone != null ? map[source.rootBone] : null;
        var sourceBones = source.bones;
        var bones = new Transform[sourceBones.Length];
        for (var i = 0; i < bones.Length; i++) bones[i] = sourceBones[i] != null ? map[sourceBones[i]] : null;
        Renderer.bones = bones;
        Renderer.localBounds = source.localBounds;
        Renderer.quality = source.quality;
        Renderer.updateWhenOffscreen = true;
        Renderer.shadowCastingMode = source.shadowCastingMode;
        Renderer.receiveShadows = source.receiveShadows;
        Renderer.enabled = false;
        source.forceRenderingOff = true;

        Transform Clone(Transform original, Transform parent)
        {
            var copy = new GameObject(original.name + " physical visual") { layer = original.gameObject.layer }.transform;
            copy.SetParent(parent, false);
            copy.SetLocalPositionAndRotation(original.localPosition, original.localRotation);
            copy.localScale = original.localScale;
            map.Add(original, copy);
            m_Pairs.Add(new KeyValuePair<Transform, Transform>(original, copy));
            foreach (Transform child in original) Clone(child, copy);
            return copy;
        }
    }

    public void Apply(bool visible)
    {
        Renderer.enabled = visible;
        if (!visible) return;
        // Parent-before-child order preserves the small non-physical metacarpal and tip offsets.
        foreach (var pair in m_Pairs)
        {
            pair.Value.localScale = pair.Key.localScale;
            pair.Value.SetLocalPositionAndRotation(pair.Key.localPosition, pair.Key.localRotation);
            if (m_Bodies.TryGetValue(pair.Key, out var body))
                pair.Value.SetPositionAndRotation(body.transform.position, body.transform.rotation);
        }
        m_Source.GetPropertyBlock(m_Properties);
        Renderer.SetPropertyBlock(m_Properties);
        for (var i = 0; i < m_Source.sharedMaterials.Length; i++)
        {
            m_Source.GetPropertyBlock(m_Properties, i);
            Renderer.SetPropertyBlock(m_Properties, i);
        }
    }

    public void Dispose()
    {
        if (Renderer != null) Renderer.enabled = false;
        if (m_Source != null) m_Source.forceRenderingOff = m_PreviousForceOff;
        if (m_Root == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(m_Root.gameObject);
        else UnityEngine.Object.DestroyImmediate(m_Root.gameObject);
    }
}
