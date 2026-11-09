using UnityEngine;

[DisallowMultipleComponent]
public sealed class RamPressCoordinator : MonoBehaviour
{
    [SerializeField] RamThumbTracking m_Tracking;
    [SerializeField] RamPressSlot[] m_Slots = System.Array.Empty<RamPressSlot>();
    [SerializeField] bool m_UsePhysicalContacts;
    [SerializeField] PhysicalHandController[] m_PhysicalHands = System.Array.Empty<PhysicalHandController>();
    readonly System.Collections.Generic.List<PhysicalHandContact> m_Contacts = new System.Collections.Generic.List<PhysicalHandContact>();
    RamPhysicalPressGesture m_PhysicalGesture;
    public RamPressSlot FocusedSlot { get; private set; }

    void OnEnable()
    {
        m_PhysicalGesture = new RamPhysicalPressGesture();
        foreach (var hand in m_PhysicalHands)
            if (hand != null) hand.Contact += OnPhysicalContact;
        if (m_Tracking != null)
            m_Tracking.Updated += OnThumbFrame;
    }

    void OnDisable()
    {
        foreach (var hand in m_PhysicalHands)
            if (hand != null) hand.Contact -= OnPhysicalContact;
        m_Contacts.Clear();
        m_PhysicalGesture?.Reset();
        if (m_Tracking != null)
            m_Tracking.Updated -= OnThumbFrame;
        FocusedSlot = null;
        foreach (var slot in m_Slots)
            if (slot != null)
                slot.ResetPress();
    }

    void OnThumbFrame(RamThumbFrame frame)
    {
        if (m_UsePhysicalContacts) return;
        var leftSlot = frame.LeftValid ? ClosestSlot(frame.LeftPosition) : null;
        var rightSlot = frame.RightValid ? ClosestSlot(frame.RightPosition) : null;
        FocusedSlot = leftSlot != null && ReferenceEquals(leftSlot, rightSlot) ? leftSlot : null;
        foreach (var slot in m_Slots)
        {
            if (slot == null || slot.State != RamPressState.Positioned)
                continue;
            if (ReferenceEquals(slot, FocusedSlot))
                slot.ProcessThumbFrame(frame);
            else
                slot.ResetPress(leftSlot != null || rightSlot != null
                    ? (frame.LeftValid && frame.RightValid ? RamPressFeedback.Misaligned : RamPressFeedback.OneThumb)
                    : RamPressFeedback.WaitingForThumbs);
        }
    }

    void OnPhysicalContact(PhysicalHandContact contact)
    {
        if (!m_UsePhysicalContacts || contact.Source == null || !contact.Source.IsPhysicalActive ||
            System.Array.IndexOf(m_PhysicalHands, contact.Source) < 0) return;
        for (var i = 0; i < m_Contacts.Count; i++)
        {
            var previous = m_Contacts[i];
            if (previous.Source == contact.Source && previous.Collision.HandCollider == contact.Collision.HandCollider &&
                previous.Collision.OtherCollider == contact.Collision.OtherCollider)
            { m_Contacts[i] = contact; return; }
        }
        m_Contacts.Add(contact);
    }

    void FixedUpdate()
    {
        if (!m_UsePhysicalContacts) return;
        var now = Time.fixedTime;
        var maximumAge = Time.fixedDeltaTime * 1.6f;
        RamPressSlot left = null, right = null;
        Vector3 leftPoint = default, rightPoint = default;
        var leftAmbiguous = false; var rightAmbiguous = false;
        var leftThumb = false; var rightThumb = false;
        var leftPriority = -1; var rightPriority = -1;
        for (var i = m_Contacts.Count - 1; i >= 0; i--)
        {
            var entry = m_Contacts[i]; var touch = entry.Collision;
            if (entry.Source == null || !entry.Source.IsPhysicalActive || touch.HandCollider == null || touch.OtherCollider == null ||
                !touch.HandCollider.enabled || !touch.OtherCollider.enabled || now - touch.Time > maximumAge || touch.Time > now + .001f)
            { m_Contacts.RemoveAt(i); continue; }
            if (!touch.IsThumb) continue;
            RamPressSlot touchedSlot = null;
            foreach (var slot in m_Slots)
                if (slot != null && slot.ClassifyPhysicalContact(touch.OtherCollider, touch.Point, touch.Normal))
                { touchedSlot = slot; break; }
            if (touchedSlot == null) continue;
            var priority = touch.IsThumb ? (int)touch.Joint : 0;
            if (entry.Source.Hand == UnityEngine.XR.Hands.Handedness.Left)
            {
                if (left != null && left != touchedSlot) leftAmbiguous = true;
                left = touchedSlot;
                if (priority >= leftPriority) { leftPoint = touch.Point; leftThumb = touch.IsThumb; leftPriority = priority; }
            }
            else if (entry.Source.Hand == UnityEngine.XR.Hands.Handedness.Right)
            {
                if (right != null && right != touchedSlot) rightAmbiguous = true;
                right = touchedSlot;
                if (priority >= rightPriority) { rightPoint = touch.Point; rightThumb = touch.IsThumb; rightPriority = priority; }
            }
        }
        if (leftAmbiguous || rightAmbiguous)
        { left = right = null; }
        FocusedSlot = left != null && left == right ? left : null;
        var completed = m_PhysicalGesture.Update(left != null ? left.Module.GetComponent<BoxCollider>() : null,
            right != null ? right.Module.GetComponent<BoxCollider>() : null, leftPoint, rightPoint,
            left != null, right != null, leftThumb, rightThumb, now);
        foreach (var slot in m_Slots)
            if (slot != null && slot.State == RamPressState.Positioned && slot != FocusedSlot) slot.ResetPress();
        TryCompletePhysicalPress(completed);
    }

    void TryCompletePhysicalPress(Collider completed)
    {
        if (completed != null && FocusedSlot != null && !FocusedSlot.TryBeginPhysicalSeating(completed))
            m_PhysicalGesture.Reset();
    }

    RamPressSlot ClosestSlot(Vector3 thumb)
    {
        RamPressSlot closest = null;
        var best = float.PositiveInfinity;
        var next = float.PositiveInfinity;
        foreach (var slot in m_Slots)
        {
            if (slot == null)
                continue;
            var distance = slot.DistanceToPressSurface(thumb);
            if (float.IsNaN(distance) || float.IsInfinity(distance))
                continue;
            if (distance < best)
            {
                next = best;
                best = distance;
                closest = slot;
            }
            else if (distance < next)
                next = distance;
        }
        return next - best < 0.0015f ? null : closest;
    }
}
