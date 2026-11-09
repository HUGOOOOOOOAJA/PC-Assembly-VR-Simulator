using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public enum RamPressState { Empty, Positioned, Seating, Installed }

[DisallowMultipleComponent]
[RequireComponent(typeof(XRSocketInteractor), typeof(RamSlotLatches))]
public sealed class RamPressSlot : MonoBehaviour
{
    [SerializeField] XRSocketInteractor m_Socket;
    [SerializeField] RamSlotLatches m_Latches;
    [SerializeField] Transform m_FinalSeat;
    [SerializeField, Min(0.001f)] float m_PreviewHeight = 0.004f;
    [SerializeField, Min(0.05f)] float m_SeatingDuration = 0.25f;
    [SerializeField, Min(0.001f)] float m_ContactRadius = 0.018f;
    [SerializeField, Min(0.005f)] float m_ApproachHeight = 0.012f;
    [SerializeField, Min(0.001f)] float m_PressDistance = 0.004f;
    Transform m_RuntimeAttach;
    RamPressGesture m_Gesture;
    BoxCollider m_ModuleCollider;
    BoxCollider m_Trigger;
    Vector3 m_OriginalTriggerCenter;
    Vector3 m_OriginalTriggerSize;
    float m_SeatingElapsed;
    float m_PressProgress;
    float m_CurrentHeight;
    float m_SeatingStartHeight;

    public RamPressState State { get; private set; }
    public XRGrabInteractable Module { get; private set; }
    public RamPressFeedback Feedback { get; private set; }

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;
        if (m_Socket == null)
            m_Socket = GetComponent<XRSocketInteractor>();
        if (m_Latches == null)
            m_Latches = GetComponent<RamSlotLatches>();
        if (m_FinalSeat == null)
            m_FinalSeat = m_Socket.attachTransform;
        if (m_FinalSeat == null)
            return;
        m_Gesture = CreateGesture();
        if (m_RuntimeAttach == null)
        {
            m_RuntimeAttach = new GameObject("RAM_PreviewAttach (runtime)").transform;
            m_RuntimeAttach.SetParent(m_FinalSeat.parent, false);
            SetAttachHeight(m_PreviewHeight);
            m_Trigger = m_Socket.GetComponent<BoxCollider>();
            if (m_Trigger != null && m_Trigger.isTrigger)
            {
                m_OriginalTriggerCenter = m_Trigger.center;
                m_OriginalTriggerSize = m_Trigger.size;
                var rise = m_Trigger.transform.InverseTransformVector(m_FinalSeat.forward * (m_PreviewHeight + 0.002f));
                m_Trigger.center = m_OriginalTriggerCenter + rise * 0.5f;
                m_Trigger.size = m_OriginalTriggerSize + new Vector3(Mathf.Abs(rise.x), Mathf.Abs(rise.y), Mathf.Abs(rise.z));
            }
        }
        m_Socket.attachTransform = m_RuntimeAttach;
        m_Socket.selectEntered.AddListener(OnSelected);
        m_Socket.selectExited.AddListener(OnReleased);
        if (m_Socket.firstInteractableSelected is XRGrabInteractable selected)
        {
            if (!ReferenceEquals(Module, selected))
                Capture(selected);
        }
        else
            ClearModule();
    }

    void OnDisable()
    {
        if (m_Socket != null)
        {
            m_Socket.selectEntered.RemoveListener(OnSelected);
            m_Socket.selectExited.RemoveListener(OnReleased);
        }
        ResetPress();
    }

    void OnDestroy()
    {
        if (m_Trigger != null && m_Trigger.isTrigger)
        {
            m_Trigger.center = m_OriginalTriggerCenter;
            m_Trigger.size = m_OriginalTriggerSize;
        }
        if (m_Socket != null && m_Socket.attachTransform == m_RuntimeAttach)
            m_Socket.attachTransform = m_FinalSeat;
        if (m_RuntimeAttach != null)
            Destroy(m_RuntimeAttach.gameObject);
    }

    void OnSelected(SelectEnterEventArgs args)
    {
        if (args.interactableObject is XRGrabInteractable module)
            Capture(module);
    }

    void Capture(XRGrabInteractable module)
    {
        Module = module;
        m_ModuleCollider = module.GetComponent<BoxCollider>();
        m_Gesture = CreateGesture();
        var installation = module.GetComponent<RamInstallationLock>();
        State = installation != null && installation.IsLocked && ReferenceEquals(installation.OwnerSocket, m_Socket)
            ? RamPressState.Installed : RamPressState.Positioned;
        SetAttachHeight(State == RamPressState.Installed ? 0f : m_PreviewHeight);
        if (State == RamPressState.Installed)
            m_Latches.CloseForInstallation(1f);
        ResetPress();
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (!ReferenceEquals(args.interactableObject, Module))
            return;
        ClearModule();
    }

    void ClearModule()
    {
        Module = null;
        m_ModuleCollider = null;
        State = RamPressState.Empty;
        m_SeatingElapsed = 0f;
        SetAttachHeight(m_PreviewHeight);
        ResetPress();
    }

    public void ResetPress(RamPressFeedback feedback = RamPressFeedback.WaitingForThumbs)
    {
        m_Gesture?.Reset();
        m_PressProgress = 0f;
        Feedback = feedback;
    }

    RamPressGesture CreateGesture()
    {
        var radius = m_ContactRadius;
        var longitudinalRadius = radius;
        if (m_ModuleCollider != null)
        {
            var thickness = m_ModuleCollider.transform.TransformVector(Vector3.up * m_ModuleCollider.size.y).magnitude;
            radius = Mathf.Min(radius, thickness * 0.5f + 0.003f);
            var length = m_ModuleCollider.transform.TransformVector(Vector3.right * m_ModuleCollider.size.x).magnitude;
            longitudinalRadius = length * 0.15f + 0.006f;
        }
        return new RamPressGesture(radius, m_ApproachHeight, m_PressDistance, 0.25f, 0.12f, longitudinalRadius, true);
    }

    bool HasExclusiveModule()
    {
        return Module != null && m_Socket != null && m_Socket.IsSelecting(Module) &&
            Module.interactorsSelecting.Count == 1 && ReferenceEquals(Module.firstInteractorSelecting, m_Socket);
    }

    public bool TryGetPressZones(out Vector3 firstZone, out Vector3 secondZone)
    {
        firstZone = secondZone = default;
        if (!isActiveAndEnabled || State == RamPressState.Empty || !HasExclusiveModule() ||
            m_ModuleCollider == null || m_RuntimeAttach == null)
            return false;
        var attach = Module.GetAttachTransform(m_Socket);
        if (Vector3.Distance(attach.position, m_RuntimeAttach.position) > 0.0015f ||
            Quaternion.Angle(attach.rotation, m_RuntimeAttach.rotation) > 2f)
            return false;
        var top = m_ModuleCollider.center + Vector3.forward * m_ModuleCollider.size.z * 0.5f;
        var end = Vector3.right * m_ModuleCollider.size.x * 0.35f;
        // Recognize against the neutral support, not the RAM that is following the thumbs.
        // Quaternion-only conversion retains the collider's world dimensions under model scaling.
        var referencePosition = State == RamPressState.Positioned
            ? m_FinalSeat.position + m_FinalSeat.forward * m_PreviewHeight : m_RuntimeAttach.position;
        var relativeRotation = m_FinalSeat.rotation * Quaternion.Inverse(attach.rotation);
        firstZone = referencePosition + relativeRotation * (m_ModuleCollider.transform.TransformPoint(top - end) - attach.position);
        secondZone = referencePosition + relativeRotation * (m_ModuleCollider.transform.TransformPoint(top + end) - attach.position);
        return true;
    }

    public void ProcessThumbFrame(RamThumbFrame frame)
    {
        if (!isActiveAndEnabled || State != RamPressState.Positioned)
            return;
        if (m_Gesture == null || m_Latches == null || !m_Latches.IsOpen || m_FinalSeat == null ||
            Time.unscaledTime - frame.Time > 0.12f || frame.Time > Time.unscaledTime + 0.01f ||
            !TryGetPressZones(out var first, out var second))
        {
            ResetPress();
            return;
        }
        Feedback = m_Gesture.Update(frame, first, second, m_FinalSeat.forward);
        m_PressProgress = m_Gesture.Progress;
        if (Feedback != RamPressFeedback.Complete || !HasExclusiveModule())
            return;
        TryBeginSeating(Module);
    }

    public bool ClassifyPhysicalContact(Collider collider, Vector3 point, Vector3 normal)
    {
        return isActiveAndEnabled && State != RamPressState.Empty && HasExclusiveModule() && m_FinalSeat != null &&
            collider != null && collider == m_ModuleCollider &&
            RamPhysicalContactSurface.IsTop(m_ModuleCollider, point, normal, m_FinalSeat.forward);
    }

    public bool TryBeginPhysicalSeating(Collider expectedCollider)
    {
        return expectedCollider != null && expectedCollider == m_ModuleCollider && TryBeginSeating(Module);
    }

    bool TryBeginSeating(XRGrabInteractable expectedModule)
    {
        if (!isActiveAndEnabled || State != RamPressState.Positioned || Module == null || Module != expectedModule ||
            m_Latches == null || !m_Latches.IsOpen || !HasExclusiveModule() || !TryGetPressZones(out _, out _)) return false;
        var installation = Module.GetComponent<RamInstallationLock>();
        if (installation == null || !installation.TryLockTo(m_Socket))
        {
            ResetPress();
            return false;
        }
        State = RamPressState.Seating;
        m_SeatingElapsed = 0f;
        m_SeatingStartHeight = m_CurrentHeight;
        Feedback = RamPressFeedback.Complete;
        return true;
    }

    public float DistanceToPressSurface(Vector3 thumb)
    {
        if (!TryGetPressZones(out var first, out var second))
            return float.PositiveInfinity;
        var along = (second - first).normalized;
        var center = (first + second) * 0.5f;
        var halfLength = m_ModuleCollider.transform.TransformVector(Vector3.right * m_ModuleCollider.size.x * 0.5f).magnitude + 0.006f;
        var nearest = center + along * Mathf.Clamp(Vector3.Dot(thumb - center, along), -halfLength, halfLength);
        var offset = thumb - nearest;
        if (offset.sqrMagnitude > 0.085f * 0.085f)
            return float.PositiveInfinity;
        return Vector3.ProjectOnPlane(offset, m_FinalSeat.forward).magnitude;
    }

    void Update()
    {
        if (State == RamPressState.Positioned && HasExclusiveModule())
        {
            var target = m_PreviewHeight * (1f - m_PressProgress * 0.9f);
            SetAttachHeight(Mathf.MoveTowards(m_CurrentHeight, target, Time.unscaledDeltaTime * 0.04f));
            return;
        }
        if (State != RamPressState.Seating || !HasExclusiveModule())
            return;
        m_SeatingElapsed += Time.unscaledDeltaTime;
        var progress = Mathf.Clamp01(m_SeatingElapsed / m_SeatingDuration);
        var eased = Mathf.SmoothStep(0f, 1f, progress);
        SetAttachHeight(m_SeatingStartHeight * (1f - eased));
        m_Latches.CloseForInstallation(eased);
        if (progress >= 1f)
            State = RamPressState.Installed;
    }

    void SetAttachHeight(float height)
    {
        if (m_RuntimeAttach != null && m_FinalSeat != null)
        {
            m_CurrentHeight = height;
            m_RuntimeAttach.SetPositionAndRotation(m_FinalSeat.position + m_FinalSeat.forward * height, m_FinalSeat.rotation);
        }
    }
}
