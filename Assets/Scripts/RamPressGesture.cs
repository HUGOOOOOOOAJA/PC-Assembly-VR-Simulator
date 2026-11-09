using UnityEngine;

public enum RamPressFeedback { WaitingForThumbs, OneThumb, Misaligned, Pressing, Complete }

public sealed class RamPressGesture
{
    struct ThumbProgress
    {
        public bool Armed;
        public bool Contact;
        public bool Finished;
        public float StartHeight;
        public float ContactTime;
        public float FinishedTime;
        public Vector3 ContactPosition;
    }

    readonly float m_Radius;
    readonly float m_ApproachHeight;
    readonly float m_PressDistance;
    readonly float m_MaxSkew;
    readonly float m_MaxSampleGap;
    readonly float m_LongitudinalRadius;
    readonly bool m_WholeSurface;
    ThumbProgress m_Left;
    ThumbProgress m_Right;
    bool m_HasTime;
    bool m_HasAssignment;
    bool m_LeftAtFirst;
    bool m_Complete;
    float m_LastTime;

    public float Progress { get; private set; }

    public RamPressGesture(float contactRadius, float approachHeight, float pressDistance, float maxSkew, float maxSampleGap)
        : this(contactRadius, approachHeight, pressDistance, maxSkew, maxSampleGap, contactRadius)
    {
    }

    public RamPressGesture(float contactRadius, float approachHeight, float pressDistance, float maxSkew, float maxSampleGap, float longitudinalRadius)
        : this(contactRadius, approachHeight, pressDistance, maxSkew, maxSampleGap, longitudinalRadius, false)
    {
    }

    public RamPressGesture(float contactRadius, float approachHeight, float pressDistance, float maxSkew, float maxSampleGap, float longitudinalRadius, bool wholeSurface)
    {
        m_Radius = Mathf.Max(0.001f, contactRadius);
        m_LongitudinalRadius = Mathf.Max(m_Radius, longitudinalRadius);
        m_WholeSurface = wholeSurface;
        m_ApproachHeight = Mathf.Max(0.005f, approachHeight);
        m_PressDistance = Mathf.Max(0.001f, pressDistance);
        m_MaxSkew = Mathf.Max(0.02f, maxSkew);
        m_MaxSampleGap = Mathf.Max(0.02f, maxSampleGap);
    }

    public void Reset()
    {
        m_Left = default;
        m_Right = default;
        m_HasTime = false;
        m_HasAssignment = false;
        m_Complete = false;
        Progress = 0f;
    }

    public RamPressFeedback Update(RamThumbFrame frame, Vector3 firstZone, Vector3 secondZone, Vector3 up)
    {
        if (!frame.LeftValid || !frame.RightValid || !IsFinite(frame.LeftPosition) || !IsFinite(frame.RightPosition) ||
            float.IsNaN(frame.Time) || float.IsInfinity(frame.Time) || up.sqrMagnitude < 0.5f)
        {
            Reset();
            return frame.LeftValid != frame.RightValid ? RamPressFeedback.OneThumb : RamPressFeedback.WaitingForThumbs;
        }
        if (m_HasTime && (frame.Time <= m_LastTime || frame.Time - m_LastTime > m_MaxSampleGap))
        {
            Reset();
            return RamPressFeedback.WaitingForThumbs;
        }
        m_HasTime = true;
        m_LastTime = frame.Time;
        if (m_Complete)
            return RamPressFeedback.Complete;
        up.Normalize();
        var leftAtFirst = Vector3.ProjectOnPlane(frame.LeftPosition - firstZone, up).sqrMagnitude <
            Vector3.ProjectOnPlane(frame.LeftPosition - secondZone, up).sqrMagnitude;
        var rightAtFirst = Vector3.ProjectOnPlane(frame.RightPosition - firstZone, up).sqrMagnitude <
            Vector3.ProjectOnPlane(frame.RightPosition - secondZone, up).sqrMagnitude;
        var along = Vector3.ProjectOnPlane(secondZone - firstZone, up).normalized;
        if (m_WholeSurface ? Mathf.Abs(Vector3.Dot(frame.LeftPosition - frame.RightPosition, along)) < 0.015f :
            leftAtFirst == rightAtFirst || (m_HasAssignment && leftAtFirst != m_LeftAtFirst))
        {
            Reset();
            return RamPressFeedback.Misaligned;
        }
        m_HasAssignment = true;
        m_LeftAtFirst = leftAtFirst;
        var leftZone = m_WholeSurface ? (firstZone + secondZone) * 0.5f : leftAtFirst ? firstZone : secondZone;
        var rightZone = m_WholeSurface ? leftZone : rightAtFirst ? firstZone : secondZone;
        var longRadius = m_LongitudinalRadius + (m_WholeSurface ? Vector3.Distance(firstZone, secondZone) * 0.5f : 0f);
        if (!Advance(ref m_Left, frame.LeftPosition, leftZone, up, along, longRadius, frame.Time) ||
            !Advance(ref m_Right, frame.RightPosition, rightZone, up, along, longRadius, frame.Time))
        {
            Reset();
            return RamPressFeedback.Misaligned;
        }
        if (m_Left.Contact && m_Right.Contact && Mathf.Abs(m_Left.ContactTime - m_Right.ContactTime) > m_MaxSkew)
        {
            Reset();
            return RamPressFeedback.Misaligned;
        }
        if (m_Left.Finished && !m_Right.Finished && frame.Time - m_Left.FinishedTime > m_MaxSkew ||
            m_Right.Finished && !m_Left.Finished && frame.Time - m_Right.FinishedTime > m_MaxSkew)
        {
            Reset();
            return RamPressFeedback.Misaligned;
        }
        if (m_Left.Finished && m_Right.Finished && Mathf.Abs(m_Left.FinishedTime - m_Right.FinishedTime) <= m_MaxSkew &&
            frame.Time - Mathf.Max(m_Left.ContactTime, m_Right.ContactTime) >= 0.06f)
        {
            m_Complete = true;
            Progress = 1f;
            return RamPressFeedback.Complete;
        }
        if (m_Left.Contact && m_Right.Contact)
        {
            var leftHeight = Vector3.Dot(frame.LeftPosition - leftZone, up);
            var rightHeight = Vector3.Dot(frame.RightPosition - rightZone, up);
            Progress = Mathf.Clamp01(Mathf.Min(m_Left.StartHeight - leftHeight, m_Right.StartHeight - rightHeight) / m_PressDistance);
            return RamPressFeedback.Pressing;
        }
        Progress = 0f;
        return m_Left.Contact || m_Right.Contact ? RamPressFeedback.OneThumb : RamPressFeedback.WaitingForThumbs;
    }

    bool Advance(ref ThumbProgress progress, Vector3 position, Vector3 zone, Vector3 up, Vector3 along, float longRadius, float time)
    {
        var offset = position - zone;
        var height = Vector3.Dot(offset, up);
        var planar = Vector3.ProjectOnPlane(offset, up);
        var longitudinal = Vector3.Dot(planar, along);
        var cross = planar - along * longitudinal;
        var penetration = progress.Armed ? Mathf.Max(m_Radius, m_PressDistance + 0.02f) : m_Radius;
        if (Mathf.Abs(longitudinal) > longRadius || cross.sqrMagnitude > m_Radius * m_Radius || height < -penetration || height > 0.08f)
            return false;
        if (height >= m_ApproachHeight)
            progress.Armed = true;
        if (!progress.Armed)
            return true;
        if (progress.Contact && height > 0.008f)
            return false;
        if (!progress.Contact && height <= 0.004f)
        {
            progress.Contact = true;
            progress.ContactPosition = position;
            progress.StartHeight = 0f;
            progress.ContactTime = time;
        }
        if (!progress.Contact)
            return true;
        if (progress.Finished && progress.StartHeight - height < m_PressDistance - 0.0015f)
            return false;
        var drift = Vector3.ProjectOnPlane(position - progress.ContactPosition, up);
        var longDrift = Vector3.Dot(drift, along);
        if (Mathf.Abs(longDrift) > Mathf.Min(m_LongitudinalRadius, 0.012f) ||
            (drift - along * longDrift).magnitude > m_Radius * 0.8f)
            return false;
        if (!progress.Finished && progress.StartHeight - height >= m_PressDistance - 0.000001f)
        {
            progress.Finished = true;
            progress.FinishedTime = time;
        }
        return true;
    }

    static bool IsFinite(Vector3 position)
    {
        return !float.IsNaN(position.sqrMagnitude) && !float.IsInfinity(position.sqrMagnitude);
    }
}
