using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>Connects the separate physics and skin to one existing tracked hand.</summary>
[DisallowMultipleComponent]
public sealed class PhysicalHandController : MonoBehaviour
{
    [SerializeField] XRHandSkeletonDriver m_Driver;
    [SerializeField] XRHandMeshController m_MeshController;
    [SerializeField] Transform m_InteractionRoot;
    [SerializeField] int m_PhysicsLayer = 8;
    PhysicalHandTracking m_Tracking;
    PhysicalHandRig m_Rig;
    PhysicalHandVisual m_Visual;
    PhysicalHandGrabCollision m_Grab;
    XRBaseInteractor[] m_Selectors;
    PhysicalHandCollisionSource m_CollisionSource;
    public event System.Action<PhysicalHandContact> Contact;
    public Handedness Hand => m_Driver.handTrackingEvents.handedness;
    public bool IsPhysicalActive => m_Rig != null && m_Rig.IsActive && CanSimulate;
    readonly HashSet<Collider> m_Selected = new HashSet<Collider>();
    bool CanSimulate => m_Tracking != null && m_Tracking.IsValid && m_Driver != null && m_Driver.isActiveAndEnabled &&
        m_MeshController != null && m_MeshController.isActiveAndEnabled && m_MeshController.handIsTracked &&
        m_MeshController.handMeshRenderer != null && m_MeshController.handMeshRenderer.enabled;

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (m_Driver == null) m_Driver = GetComponent<XRHandSkeletonDriver>();
        if (m_MeshController == null) m_MeshController = GetComponent<XRHandMeshController>();
        if (m_Driver == null || m_MeshController == null || m_InteractionRoot == null || LayerMask.LayerToName(m_PhysicsLayer) != "Physical Hands")
        { Debug.LogError("Physical hand configuration is incomplete.", this); enabled = false; return; }
        m_Selectors = m_InteractionRoot.GetComponentsInChildren<XRBaseInteractor>(true);
        m_Tracking = new PhysicalHandTracking(m_Driver);
        Application.onBeforeRender += Render;
    }

    void Update()
    {
        if (m_Tracking == null) return;
        m_Tracking.Poll();
        if (m_Rig == null && CanSimulate)
        {
            m_Rig = new PhysicalHandRig(m_Driver.rootTransform, m_Driver.jointTransformReferences, transform, m_PhysicsLayer, m_Tracking.WorldPoses);
            m_Visual = new PhysicalHandVisual(transform, (SkinnedMeshRenderer)m_MeshController.handMeshRenderer, m_Rig.Bodies);
            m_Grab = new PhysicalHandGrabCollision(m_Rig.Colliders);
            m_CollisionSource = m_Rig.Bodies[m_Driver.rootTransform].gameObject.AddComponent<PhysicalHandCollisionSource>();
            m_CollisionSource.Configure(m_Rig.JointByCollider);
            m_CollisionSource.Contact += OnPhysicalCollision;
        }
        if (m_Rig == null) return;
        UpdateGrabs();
        if (!CanSimulate) m_Rig.Suspend();
        Render();
    }

    void FixedUpdate()
    {
        if (m_Rig == null) return;
        m_Tracking.Poll();
        foreach (var pair in m_Tracking.WorldPoses) m_Rig.SetTargetPose(pair.Key, pair.Value);
        UpdateGrabs();
        m_Rig.Step(CanSimulate, Time.fixedDeltaTime);
    }

    void UpdateGrabs()
    {
        m_Selected.Clear();
        foreach (var selector in m_Selectors)
        {
            if (selector == null) continue;
            foreach (var item in selector.interactablesSelected)
                foreach (var collider in item.colliders)
                    if (collider != null) m_Selected.Add(collider);
        }
        m_Grab.Update(m_Selected, Time.unscaledTime);
        m_Rig.SetIgnoredColliders(m_Grab.Ignored);
    }

    void LateUpdate() { Render(); }

    void OnPhysicalCollision(PhysicalHandCollision collision)
    {
        if (IsPhysicalActive) Contact?.Invoke(new PhysicalHandContact(this, collision));
    }

    [BeforeRenderOrder(10000)]
    void Render()
    {
        m_Visual?.Apply(m_Rig.IsActive && CanSimulate);
    }

    void OnDisable()
    {
        Application.onBeforeRender -= Render;
        if (m_CollisionSource != null) m_CollisionSource.Contact -= OnPhysicalCollision;
        m_CollisionSource = null;
        m_Tracking?.Dispose(); m_Tracking = null;
        m_Grab?.Dispose(); m_Grab = null;
        m_Visual?.Dispose(); m_Visual = null;
        m_Rig?.Dispose(); m_Rig = null;
    }
}
