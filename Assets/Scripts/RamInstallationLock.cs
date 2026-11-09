using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public sealed class RamInstallationLock : MonoBehaviour, IXRSelectFilter
{
    XRGrabInteractable m_Module;
    public bool IsLocked { get; private set; }
    public XRSocketInteractor OwnerSocket { get; private set; }
    public bool canProcess => isActiveAndEnabled;

    void OnEnable()
    {
        m_Module = GetComponent<XRGrabInteractable>();
        m_Module.selectFilters.Add(this);
    }

    void OnDisable()
    {
        if (m_Module != null)
            m_Module.selectFilters.Remove(this);
    }

    public bool TryLockTo(XRSocketInteractor socket)
    {
        if (!isActiveAndEnabled || socket == null || m_Module == null || !socket.IsSelecting(m_Module) ||
            m_Module.interactorsSelecting.Count != 1 || !ReferenceEquals(m_Module.firstInteractorSelecting, socket))
            return false;
        if (IsLocked)
            return ReferenceEquals(OwnerSocket, socket);
        OwnerSocket = socket;
        IsLocked = true;
        return true;
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        return !IsLocked || ReferenceEquals(interactor, OwnerSocket);
    }
}
