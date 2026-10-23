using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRSocketInteractor))]
public sealed class RamSocketFilter : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
{
    [SerializeField] XRGrabInteractable[] m_Modules = Array.Empty<XRGrabInteractable>();
    [SerializeField, Range(0f, 45f)] float m_MaxAngle = 12f;
    [SerializeField] Vector3 m_PositionTolerance = new Vector3(0.02f, 0.0045f, 0.018f);
    [SerializeField, Min(0f)] float m_MaxSpeed = 0.4f;
    [SerializeField, Min(0f)] float m_MaxAngularSpeed = 2f;

    readonly Dictionary<IXRSelectInteractable, int> m_ReleaseFrames = new Dictionary<IXRSelectInteractable, int>();
    XRSocketInteractor m_Socket;

    public bool canProcess => isActiveAndEnabled;

    void OnEnable()
    {
        m_Socket = GetComponent<XRSocketInteractor>();
        m_Socket.hoverFilters.Add(this);
        m_Socket.selectFilters.Add(this);
        foreach (var module in m_Modules)
        {
            if (module != null)
                module.selectExited.AddListener(OnReleased);
        }
    }

    void OnDisable()
    {
        if (m_Socket != null)
        {
            m_Socket.hoverFilters.Remove(this);
            m_Socket.selectFilters.Remove(this);
        }
        foreach (var module in m_Modules)
        {
            if (module != null)
                module.selectExited.RemoveListener(OnReleased);
        }
        m_ReleaseFrames.Clear();
    }

    void OnReleased(SelectExitEventArgs args)
    {
        m_ReleaseFrames[args.interactableObject] = Time.frameCount;
    }

    public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
    {
        return interactable is XRGrabInteractable module && HasValidPose(module);
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (!(interactable is XRGrabInteractable module) || !HasValidPose(module))
            return false;
        if (m_Socket.IsSelecting(module))
            return true;
        if (m_ReleaseFrames.TryGetValue(module, out var releaseFrame) && Time.frameCount <= releaseFrame)
            return false;
        var body = module.GetComponent<Rigidbody>();
        return body != null && body.linearVelocity.sqrMagnitude <= m_MaxSpeed * m_MaxSpeed &&
            body.angularVelocity.sqrMagnitude <= m_MaxAngularSpeed * m_MaxAngularSpeed;
    }

    bool HasValidPose(XRGrabInteractable module)
    {
        if (Array.IndexOf(m_Modules, module) < 0)
            return false;
        if (m_Socket.IsSelecting(module))
            return true;
        var target = m_Socket.GetAttachTransform(module);
        var source = module.GetAttachTransform(m_Socket);
        var offset = Quaternion.Inverse(target.rotation) * (source.position - target.position);
        return Mathf.Abs(offset.x) <= m_PositionTolerance.x &&
            Mathf.Abs(offset.y) <= m_PositionTolerance.y &&
            Mathf.Abs(offset.z) <= m_PositionTolerance.z &&
            Quaternion.Angle(source.rotation, target.rotation) <= m_MaxAngle;
    }
}
