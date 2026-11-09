using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Owns only the IgnoreCollision pairs it changes, including the release separation grace.</summary>
public sealed class PhysicalHandGrabCollision : IDisposable
{
    sealed class Entry
    {
        public bool[] Previous;
        public float ReleaseTime = float.PositiveInfinity;
    }
    readonly IReadOnlyList<Collider> m_Hand;
    readonly Dictionary<Collider, Entry> m_Entries = new Dictionary<Collider, Entry>();
    readonly HashSet<Collider> m_Selected = new HashSet<Collider>();
    readonly List<Collider> m_Remove = new List<Collider>();
    public IReadOnlyCollection<Collider> Ignored => m_Entries.Keys;

    public PhysicalHandGrabCollision(IReadOnlyList<Collider> hand) { m_Hand = hand; }

    public void Update(IReadOnlyCollection<Collider> selected, float now)
    {
        m_Selected.Clear();
        foreach (var collider in selected)
        {
            if (collider == null || !m_Selected.Add(collider)) continue;
            if (!m_Entries.TryGetValue(collider, out var entry))
            {
                entry = new Entry { Previous = new bool[m_Hand.Count] };
                for (var i = 0; i < m_Hand.Count; i++)
                {
                    entry.Previous[i] = Physics.GetIgnoreCollision(m_Hand[i], collider);
                    Physics.IgnoreCollision(m_Hand[i], collider, true);
                }
                m_Entries.Add(collider, entry);
            }
            entry.ReleaseTime = float.PositiveInfinity;
        }
        m_Remove.Clear();
        foreach (var pair in m_Entries)
        {
            if (pair.Key == null) { m_Remove.Add(pair.Key); continue; }
            if (m_Selected.Contains(pair.Key)) continue;
            if (float.IsPositiveInfinity(pair.Value.ReleaseTime)) pair.Value.ReleaseTime = now;
            if (now - pair.Value.ReleaseTime < .12f || Overlaps(pair.Key)) continue;
            Restore(pair.Key, pair.Value);
            m_Remove.Add(pair.Key);
        }
        foreach (var collider in m_Remove) m_Entries.Remove(collider);
    }

    bool Overlaps(Collider other)
    {
        foreach (var hand in m_Hand)
            if (hand != null && Physics.ComputePenetration(hand, hand.transform.position, hand.transform.rotation,
                    other, other.transform.position, other.transform.rotation, out _, out _)) return true;
        return false;
    }

    void Restore(Collider other, Entry entry)
    {
        if (other == null) return;
        for (var i = 0; i < m_Hand.Count; i++)
            if (m_Hand[i] != null) Physics.IgnoreCollision(m_Hand[i], other, entry.Previous[i]);
    }

    public void Dispose()
    {
        foreach (var pair in m_Entries) Restore(pair.Key, pair.Value);
        m_Entries.Clear();
    }
}
