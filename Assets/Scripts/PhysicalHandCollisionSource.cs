using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public readonly struct PhysicalHandCollision
{
    public readonly Collider HandCollider;
    public readonly Collider OtherCollider;
    public readonly XRHandJointID Joint;
    public readonly Vector3 Point;
    public readonly Vector3 Normal;
    public readonly float Time;
    public bool IsThumb => Joint == XRHandJointID.ThumbMetacarpal || Joint == XRHandJointID.ThumbProximal || Joint == XRHandJointID.ThumbDistal;
    public PhysicalHandCollision(Collider hand, Collider other, XRHandJointID joint, Vector3 point, Vector3 normal, float time)
    { HandCollider = hand; OtherCollider = other; Joint = joint; Point = point; Normal = normal; Time = time; }
}

public readonly struct PhysicalHandContact
{
    public readonly PhysicalHandController Source;
    public readonly PhysicalHandCollision Collision;
    public PhysicalHandContact(PhysicalHandController source, PhysicalHandCollision collision) { Source = source; Collision = collision; }
}

/// <summary>Receives actual compound-shape contacts on the hand Rigidbody.</summary>
public sealed class PhysicalHandCollisionSource : MonoBehaviour
{
    IReadOnlyDictionary<Collider, XRHandJointID> m_Shapes;
    public event Action<PhysicalHandCollision> Contact;
    public void Configure(IReadOnlyDictionary<Collider, XRHandJointID> shapes) { m_Shapes = shapes; }
    void OnCollisionEnter(Collision collision) { Report(collision); }
    void OnCollisionStay(Collision collision) { Report(collision); }
    void Report(Collision collision)
    {
        if (m_Shapes == null) return;
        for (var i = 0; i < collision.contactCount; i++)
        {
            var point = collision.GetContact(i);
            if (m_Shapes.TryGetValue(point.thisCollider, out var joint))
                Contact?.Invoke(new PhysicalHandCollision(point.thisCollider, point.otherCollider, joint, point.point, point.normal, Time.fixedTime));
            else if (m_Shapes.TryGetValue(point.otherCollider, out joint))
                Contact?.Invoke(new PhysicalHandCollision(point.otherCollider, point.thisCollider, joint, point.point, -point.normal, Time.fixedTime));
        }
    }
}
