using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public readonly struct RamThumbFrame
{
    public readonly Vector3 LeftPosition;
    public readonly Vector3 RightPosition;
    public readonly bool LeftValid;
    public readonly bool RightValid;
    public readonly float Time;

    public RamThumbFrame(Vector3 leftPosition, Vector3 rightPosition, bool leftValid, bool rightValid, float time)
    {
        LeftPosition = leftPosition;
        RightPosition = rightPosition;
        LeftValid = leftValid;
        RightValid = rightValid;
        Time = time;
    }
}

[DisallowMultipleComponent]
public sealed class RamThumbTracking : MonoBehaviour
{
    [SerializeField] Transform m_TrackingSpace;
    readonly List<XRHandSubsystem> m_Subsystems = new List<XRHandSubsystem>();
    XRHandSubsystem m_Subsystem;
    float m_LastUpdate = float.NegativeInfinity;
    float m_NextSearch;
    bool m_Invalidated;

    public event Action<RamThumbFrame> Updated;

    void OnEnable()
    {
        m_NextSearch = 0f;
        m_LastUpdate = float.NegativeInfinity;
        m_Invalidated = false;
        if (Application.isPlaying)
            Discover();
    }

    void OnDisable()
    {
        Unbind();
        Invalidate();
    }

    void Update()
    {
        if (m_Subsystem != null && !m_Subsystem.running)
        {
            Unbind();
            Invalidate();
        }
        if (m_Subsystem == null && Time.unscaledTime >= m_NextSearch)
            Discover();
        if (!m_Invalidated && Time.unscaledTime - m_LastUpdate > 0.12f)
            Invalidate();
    }

    void Discover()
    {
        m_NextSearch = Time.unscaledTime + 1f;
        SubsystemManager.GetSubsystems(m_Subsystems);
        foreach (var subsystem in m_Subsystems)
        {
            if (!subsystem.running)
                continue;
            m_Subsystem = subsystem;
            m_Subsystem.updatedHands += OnHandsUpdated;
            m_Subsystem.trackingLost += OnTrackingLost;
            m_LastUpdate = float.NegativeInfinity;
            Invalidate();
            break;
        }
    }

    void Unbind()
    {
        if (m_Subsystem == null)
            return;
        m_Subsystem.updatedHands -= OnHandsUpdated;
        m_Subsystem.trackingLost -= OnTrackingLost;
        m_Subsystem = null;
    }

    void OnTrackingLost(XRHand hand)
    {
        Invalidate();
    }

    void OnHandsUpdated(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags flags, XRHandSubsystem.UpdateType updateType)
    {
        if (!isActiveAndEnabled || !ReferenceEquals(subsystem, m_Subsystem) || !subsystem.running ||
            updateType != XRHandSubsystem.UpdateType.Dynamic)
            return;
        var leftValid = TryReadThumb(subsystem.leftHand, (flags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) != 0, m_TrackingSpace, out var left);
        var rightValid = TryReadThumb(subsystem.rightHand, (flags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints) != 0, m_TrackingSpace, out var right);
        m_LastUpdate = Time.unscaledTime;
        m_Invalidated = false;
        Updated?.Invoke(new RamThumbFrame(left, right, leftValid, rightValid, m_LastUpdate));
    }

    static bool TryReadThumb(XRHand hand, bool jointsUpdated, Transform trackingSpace, out Vector3 position)
    {
        position = default;
        if (!jointsUpdated || !hand.isTracked || trackingSpace == null ||
            !hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out var pose))
            return false;
        position = trackingSpace.TransformPoint(pose.position);
        return !float.IsNaN(position.sqrMagnitude) && !float.IsInfinity(position.sqrMagnitude);
    }

    void Invalidate()
    {
        m_Invalidated = true;
        Updated?.Invoke(new RamThumbFrame(default, default, false, false, Time.unscaledTime));
    }
}
