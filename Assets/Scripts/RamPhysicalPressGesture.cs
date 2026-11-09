using UnityEngine;

/// <summary>Continuous actual contact, independent of raw thumb motion or inferred force.</summary>
public sealed class RamPhysicalPressGesture
{
    Collider m_Module;
    float m_Start;
    float m_Last = float.NegativeInfinity;
    bool m_Fired;

    public Collider Update(Collider leftModule, Collider rightModule, Vector3 leftPoint, Vector3 rightPoint,
        bool leftValid, bool rightValid, bool leftThumb, bool rightThumb, float time)
    {
        if (!float.IsFinite(time) || time < 0f || time <= m_Last)
        { Reset(); return null; }
        leftValid &= leftThumb;
        rightValid &= rightThumb;
        leftValid &= leftModule != null;
        rightValid &= rightModule != null;
        Collider module = null;
        if (leftValid && rightValid && leftModule == rightModule && Vector3.Distance(leftPoint, rightPoint) >= .015f)
            module = leftModule;
        if (module == null)
        { Reset(); return null; }
        if (m_Module != module || time - m_Last > .05f)
        { m_Module = module; m_Start = time; m_Fired = false; }
        m_Last = time;
        if (m_Fired || time - m_Start < .12f) return null;
        m_Fired = true;
        return module;
    }

    public void Reset()
    { m_Module = null; m_Start = 0f; m_Last = float.NegativeInfinity; m_Fired = false; }
}

public static class RamPhysicalContactSurface
{
    public static bool IsTop(BoxCollider box, Vector3 point, Vector3 normal, Vector3 up)
    {
        if (box == null || box.isTrigger || !Finite(point) || !Finite(normal) || normal.sqrMagnitude < .5f ||
            Vector3.Dot(normal.normalized, up.normalized) < .65f) return false;
        var local = box.transform.InverseTransformPoint(point) - box.center;
        var scaleX = box.transform.TransformVector(Vector3.right).magnitude;
        var scaleY = box.transform.TransformVector(Vector3.up).magnitude;
        if (scaleX < .0001f || scaleY < .0001f) return false;
        var top = box.transform.TransformPoint(box.center + Vector3.forward * box.size.z * .5f);
        return Mathf.Abs(Vector3.Dot(point - top, up.normalized)) <= .003f &&
            Mathf.Abs(local.x) <= box.size.x * .5f + .002f / scaleX &&
            Mathf.Abs(local.y) <= box.size.y * .5f + .002f / scaleY;
    }
    static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
}
