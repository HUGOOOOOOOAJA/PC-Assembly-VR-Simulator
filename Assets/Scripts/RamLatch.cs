using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
public sealed class RamLatch : XRBaseInteractable
{
    [SerializeField] Transform m_MovingPart;
    [SerializeField] Transform m_Grip;
    [SerializeField] XRSocketInteractor m_Socket;
    [SerializeField] float m_OpenDirection = 1f;
    [SerializeField, Range(1f, 90f)] float m_OpenAngle = 55f;
    [SerializeField, Min(0f)] float m_Angle;

    readonly List<Collider> m_NearTargets = new List<Collider>();
    IXRSelectInteractor m_Hand;
    float m_GrabOffset;
    float m_HandAngle;
    bool m_HasGrabAngle;

    public bool IsOpen => m_Angle >= Mathf.Min(45f, m_OpenAngle);
    public float Angle => m_Angle;

    protected override void Awake()
    {
        base.Awake();
        selectMode = InteractableSelectMode.Single;
        SetAngle(m_Angle);
    }

    void OnValidate()
    {
        m_OpenDirection = m_OpenDirection < 0f ? -1f : 1f;
        m_OpenAngle = Mathf.Clamp(m_OpenAngle, 1f, 90f);
        SetAngle(m_Angle);
    }

    public override Transform GetAttachTransform(IXRInteractor interactor)
    {
        return m_Grip != null ? m_Grip : base.GetAttachTransform(interactor);
    }

    public override bool IsHoverableBy(IXRHoverInteractor interactor)
    {
        return base.IsHoverableBy(interactor) && HasMovingPart() &&
            (m_Socket == null || !m_Socket.hasSelection) && IsNearHand(interactor);
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        if (!base.IsSelectableBy(interactor) || !HasMovingPart())
            return false;
        if (IsSelected(interactor))
            return true;
        return !isSelected && (m_Socket == null || !m_Socket.hasSelection) && IsNearHand(interactor);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        m_Hand = args.interactorObject;
        m_HasGrabAngle = false;
        CaptureGrabAngle();
        base.OnSelectEntered(args);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        if (ReferenceEquals(m_Hand, args.interactorObject))
        {
            var open = args.isCanceled ? IsOpen : m_Angle >= m_OpenAngle * 0.5f;
            SetAngle(open ? m_OpenAngle : 0f);
            m_Hand = null;
            m_HasGrabAngle = false;
        }
        base.OnSelectExited(args);
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);
        if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic || m_Hand == null ||
            (m_Socket != null && m_Socket.hasSelection))
            return;
        if (!m_HasGrabAngle)
        {
            CaptureGrabAngle();
            return;
        }
        if (!TryGetHandAngle(out var handAngle))
            return;
        m_HandAngle += Mathf.DeltaAngle(m_HandAngle, handAngle);
        SetAngle(m_GrabOffset + m_HandAngle * Mathf.Sign(m_OpenDirection));
    }

    bool HasMovingPart()
    {
        return m_MovingPart != null && m_MovingPart != transform && m_MovingPart.IsChildOf(transform);
    }

    bool IsNearHand(IXRInteractor interactor)
    {
        if (interactor is XRDirectInteractor)
            return true;
        if (!(interactor is NearFarInteractor nearFar) || !nearFar.enableNearCasting)
            return false;
        if (nearFar.hasSelection && nearFar.selectionRegion.Value == NearFarInteractor.Region.Far)
            return false;
        var caster = nearFar.nearInteractionCaster;
        if (caster == null || !caster.TryGetColliderTargets(nearFar.interactionManager, m_NearTargets))
            return false;
        foreach (var target in m_NearTargets)
        {
            if (colliders.Contains(target))
                return true;
        }
        return false;
    }

    void CaptureGrabAngle()
    {
        if (!TryGetHandAngle(out var handAngle))
            return;
        m_HandAngle = handAngle;
        m_GrabOffset = m_Angle - handAngle * Mathf.Sign(m_OpenDirection);
        m_HasGrabAngle = true;
    }

    bool TryGetHandAngle(out float handAngle)
    {
        handAngle = 0f;
        if (m_Hand == null)
            return false;
        var attach = m_Hand.GetAttachTransform(this);
        if (attach == null)
            return false;
        var direction = transform.InverseTransformPoint(attach.position);
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.000001f)
            return false;
        handAngle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);
        return true;
    }

    void SetAngle(float angle)
    {
        m_Angle = Mathf.Clamp(angle, 0f, m_OpenAngle);
        if (HasMovingPart())
            m_MovingPart.localRotation = Quaternion.AngleAxis(m_Angle * Mathf.Sign(m_OpenDirection), Vector3.up);
    }
}
