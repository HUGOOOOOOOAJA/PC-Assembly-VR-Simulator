using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

/// <summary>Read-only Dynamic snapshots. Does not replace the skeleton driver or modify XR poses.</summary>
public sealed class PhysicalHandTracking : IDisposable
{
    readonly XRHandSkeletonDriver m_Driver;
    readonly List<XRHandSubsystem> m_Subsystems = new List<XRHandSubsystem>();
    readonly Dictionary<Transform, Pose> m_LocalPoses = new Dictionary<Transform, Pose>();
    readonly Dictionary<Transform, Pose> m_WorldPoses = new Dictionary<Transform, Pose>();
    XRHandSubsystem m_Subsystem;
    float m_NextSearch;
    float m_LastUpdate = float.NegativeInfinity;
    bool m_Valid;
    public bool IsValid => m_Valid && m_Subsystem != null && m_Subsystem.running && m_Driver.isActiveAndEnabled && Time.unscaledTime - m_LastUpdate <= .12f;
    public IReadOnlyDictionary<Transform, Pose> WorldPoses => m_WorldPoses;

    public PhysicalHandTracking(XRHandSkeletonDriver driver) { m_Driver = driver; }

    public void Poll()
    {
        if (m_Subsystem != null && !m_Subsystem.running) Unbind();
        if (m_Subsystem == null && Time.unscaledTime >= m_NextSearch)
        {
            m_NextSearch = Time.unscaledTime + 1f;
            SubsystemManager.GetSubsystems(m_Subsystems);
            foreach (var subsystem in m_Subsystems)
            {
                if (!subsystem.running) continue;
                m_Subsystem = subsystem;
                m_Subsystem.updatedHands += Updated;
                m_Subsystem.trackingLost += Lost;
                break;
            }
        }
        if (!IsValid) return;
        var space = m_Driver.rootTransform.parent;
        foreach (var pair in m_LocalPoses)
            m_WorldPoses[pair.Key] = space != null
                ? new Pose(space.TransformPoint(pair.Value.position), space.rotation * pair.Value.rotation)
                : pair.Value;
    }

    void Updated(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags flags, XRHandSubsystem.UpdateType type)
    {
        if (type != XRHandSubsystem.UpdateType.Dynamic || !ReferenceEquals(subsystem, m_Subsystem)) return;
        var left = m_Driver.handTrackingEvents.handedness == Handedness.Left;
        var hand = left ? subsystem.leftHand : subsystem.rightHand;
        var flag = left ? XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints : XRHandSubsystem.UpdateSuccessFlags.RightHandJoints;
        m_Valid = hand.isTracked && (flags & flag) != 0;
        foreach (var joint in m_Driver.jointTransformReferences)
        {
            if (joint.jointTransform == null) continue;
            if (!hand.GetJoint(joint.xrHandJointID).TryGetPose(out var pose) ||
                !float.IsFinite(pose.position.x) || !float.IsFinite(pose.position.y) || !float.IsFinite(pose.position.z))
            { m_Valid = false; break; }
            m_LocalPoses[joint.jointTransform] = pose;
        }
        m_LastUpdate = Time.unscaledTime;
    }

    void Lost(XRHand hand)
    {
        if (hand.handedness == m_Driver.handTrackingEvents.handedness) m_Valid = false;
    }

    void Unbind()
    {
        if (m_Subsystem != null)
        {
            m_Subsystem.updatedHands -= Updated;
            m_Subsystem.trackingLost -= Lost;
            m_Subsystem = null;
        }
        m_Valid = false;
    }
    public void Dispose() { Unbind(); }
}
