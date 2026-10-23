using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRSocketInteractor))]
public sealed class RamSlotLatches : MonoBehaviour
{
    [SerializeField] RamLatch m_LeftLatch;
    [SerializeField] RamLatch m_RightLatch;
    [SerializeField] XRSocketInteractor m_Socket;

    public bool IsOpen => m_LeftLatch != null && m_RightLatch != null &&
        m_LeftLatch.isActiveAndEnabled && m_RightLatch.isActiveAndEnabled &&
        m_LeftLatch.IsOpen && m_RightLatch.IsOpen &&
        !m_LeftLatch.isSelected && !m_RightLatch.isSelected;
    public bool IsFocused => (m_LeftLatch != null && (m_LeftLatch.isHovered || m_LeftLatch.isSelected)) ||
        (m_RightLatch != null && (m_RightLatch.isHovered || m_RightLatch.isSelected));
    public XRSocketInteractor Socket => m_Socket;
}
