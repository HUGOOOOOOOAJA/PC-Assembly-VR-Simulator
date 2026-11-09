using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Hands;

/// <summary>A single dynamic hand with compound shapes. Contact cannot bend the tracked skeleton.</summary>
public sealed class PhysicalHandRig : IDisposable
{
    sealed class Bone
    {
        public Transform Source;
        public Transform Pivot;
        public Pose Target;
        public bool HasTarget;
    }
    readonly GameObject m_Root;
    readonly Rigidbody m_Body;
    readonly PhysicsMaterial m_Material;
    readonly List<Bone> m_Bones = new List<Bone>();
    readonly Dictionary<Transform,Bone> m_Lookup = new Dictionary<Transform,Bone>();
    readonly Dictionary<Transform,Rigidbody> m_Bodies = new Dictionary<Transform,Rigidbody>();
    readonly List<Collider> m_Colliders = new List<Collider>();
    readonly Dictionary<Collider,XRHandJointID> m_Joints = new Dictionary<Collider,XRHandJointID>();
    readonly HashSet<Collider> m_Ignored = new HashSet<Collider>();
    readonly Collider[] m_Overlaps = new Collider[128];
    readonly PhysicsScene m_PhysicsScene;
    readonly int m_Layer;
    readonly IReadOnlyDictionary<Transform,Pose> m_InitialPoses;
    public IReadOnlyDictionary<Transform,Rigidbody> Bodies => m_Bodies;
    public IReadOnlyList<Collider> Colliders => m_Colliders;
    public IReadOnlyDictionary<Collider,XRHandJointID> JointByCollider => m_Joints;
    public bool IsActive { get; private set; }

    public PhysicalHandRig(Transform root,IReadOnlyList<JointToTransformReference> joints,Transform owner,int layer)
        : this(root,joints,owner,layer,null) { }
    public PhysicalHandRig(Transform root,IReadOnlyList<JointToTransformReference> joints,Transform owner,int layer,IReadOnlyDictionary<Transform,Pose> initialPoses)
    {
        m_Layer=layer; m_InitialPoses=initialPoses;
        m_Root=new GameObject("Physical "+owner.name+" (runtime)") { layer=layer };
        SceneManager.MoveGameObjectToScene(m_Root,owner.gameObject.scene);
        m_PhysicsScene=owner.gameObject.scene.GetPhysicsScene();
        m_Root.transform.SetPositionAndRotation(Position(root),Rotation(root));
        m_Body=m_Root.AddComponent<Rigidbody>();
        m_Body.mass=.5f; m_Body.useGravity=false; m_Body.isKinematic=true;
        m_Body.interpolation=RigidbodyInterpolation.Interpolate;
        m_Body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        m_Body.solverIterations=12; m_Body.solverVelocityIterations=6;
        m_Body.maxAngularVelocity=20f; m_Body.maxLinearVelocity=3f;
        m_Body.maxDepenetrationVelocity=.5f; m_Body.sleepThreshold=0f;
        // Moving compound fingers must not shift the mass frame every time the user curls a finger.
        m_Body.centerOfMass=Vector3.zero;
        m_Body.inertiaTensor=Vector3.one*.02f;
        m_Body.inertiaTensorRotation=Quaternion.identity;
        m_Bodies.Add(root,m_Body);
        m_Material=new PhysicsMaterial("Physical hand grip (runtime)")
        {
            staticFriction=1f,dynamicFriction=.9f,bounciness=0f,
            frictionCombine=PhysicsMaterialCombine.Maximum,bounceCombine=PhysicsMaterialCombine.Minimum
        };
        var rootBone=new Bone {Source=root,Pivot=m_Root.transform,Target=new Pose(Position(root),Rotation(root)),HasTarget=initialPoses!=null};
        m_Bones.Add(rootBone);m_Lookup.Add(root,rootBone);
        var map=new Dictionary<XRHandJointID,Transform>();
        foreach(var joint in joints) if(joint.jointTransform!=null)map[joint.xrHandJointID]=joint.jointTransform;
        var middle=map[XRHandJointID.MiddleProximal];var index=map[XRHandJointID.IndexProximal];var little=map[XRHandJointID.LittleProximal];
        var forward=(Position(middle)-Position(root)).normalized;
        var normal=Vector3.Cross(forward,Position(index)-Position(little)).normalized;
        var palm=Shape(rootBone,"Palm",(Position(root)+Position(middle))*.5f,Quaternion.LookRotation(forward,normal));
        var box=palm.AddComponent<BoxCollider>();
        box.size=new Vector3(Mathf.Clamp(Vector3.Distance(Position(index),Position(little))+.019f,.04f,.095f),.022f,Mathf.Clamp(Vector3.Distance(Position(root),Position(middle)),.04f,.11f));
        Register(box,XRHandJointID.Palm);
        AddFinger(XRHandJointID.ThumbMetacarpal,XRHandJointID.ThumbProximal,XRHandJointID.ThumbDistal,XRHandJointID.ThumbTip,.009f);
        AddFinger(XRHandJointID.IndexProximal,XRHandJointID.IndexIntermediate,XRHandJointID.IndexDistal,XRHandJointID.IndexTip,.0075f);
        AddFinger(XRHandJointID.MiddleProximal,XRHandJointID.MiddleIntermediate,XRHandJointID.MiddleDistal,XRHandJointID.MiddleTip,.0075f);
        AddFinger(XRHandJointID.RingProximal,XRHandJointID.RingIntermediate,XRHandJointID.RingDistal,XRHandJointID.RingTip,.007f);
        AddFinger(XRHandJointID.LittleProximal,XRHandJointID.LittleIntermediate,XRHandJointID.LittleDistal,XRHandJointID.LittleTip,.0065f);

        void AddFinger(XRHandJointID a,XRHandJointID b,XRHandJointID c,XRHandJointID tip,float radius)
        {
            var ids=new[]{a,b,c,tip};
            for(var i=0;i<3;i++)
            {
                var source=map[ids[i]];var next=map[ids[i+1]];
                var pivot=new GameObject(source.name+" collision pose").transform;
                pivot.SetParent(m_Root.transform,false);
                pivot.SetPositionAndRotation(Position(source),Rotation(source));
                var bone=new Bone{Source=source,Pivot=pivot,Target=new Pose(Position(source),Rotation(source)),HasTarget=initialPoses!=null};
                m_Bones.Add(bone);m_Lookup.Add(source,bone);
                var delta=Position(next)-Position(source);
                var shape=Shape(bone,ids[i].ToString(),(Position(next)+Position(source))*.5f,Quaternion.FromToRotation(Vector3.up,delta.normalized));
                var capsule=shape.AddComponent<CapsuleCollider>();capsule.radius=radius*(i==2?.85f:1f);
                capsule.height=Mathf.Max(capsule.radius*2f,delta.magnitude+capsule.radius);capsule.direction=1;
                Register(capsule,ids[i]);
            }
        }
    }
    Vector3 Position(Transform source)=>m_InitialPoses!=null&&m_InitialPoses.TryGetValue(source,out var pose)?pose.position:source.position;
    Quaternion Rotation(Transform source)=>m_InitialPoses!=null&&m_InitialPoses.TryGetValue(source,out var pose)?pose.rotation:source.rotation;
    GameObject Shape(Bone bone,string name,Vector3 position,Quaternion rotation)
    {
        var go=new GameObject(name+" collider"){layer=m_Layer};go.transform.SetPositionAndRotation(position,rotation);go.transform.SetParent(bone.Pivot,true);return go;
    }
    void Register(Collider collider,XRHandJointID joint)
    {
        collider.contactOffset=.001f;collider.sharedMaterial=m_Material;collider.enabled=false;
        m_Colliders.Add(collider);m_Joints.Add(collider,joint);
    }
    public void SetTargetPose(Transform source,Pose target)
    {
        if(!m_Lookup.TryGetValue(source,out var bone))return;bone.Target=target;bone.HasTarget=true;
    }
    public void SetIgnoredColliders(IReadOnlyCollection<Collider> colliders)
    {
        m_Ignored.Clear();foreach(var collider in colliders)if(collider!=null)m_Ignored.Add(collider);
    }
    public void Step(bool trackingValid,float deltaTime)
    {
        foreach(var bone in m_Bones)
        {
            if(!bone.HasTarget)bone.Target=new Pose(bone.Source.position,bone.Source.rotation);
            trackingValid&=Finite(bone.Target.position)&&Finite(bone.Target.rotation);
        }
        if(!trackingValid){Suspend();return;}
        var target=m_Bones[0].Target;
        var inverse=Quaternion.Inverse(target.rotation);
        // Local articulation comes from the actual hand, never from a collision solving an individual bone.
        for(var i=1;i<m_Bones.Count;i++)
        {
            var bone=m_Bones[i];
            bone.Pivot.SetLocalPositionAndRotation(inverse*(bone.Target.position-target.position),inverse*bone.Target.rotation);
        }
        if(IsActive&&Vector3.Distance(m_Body.position,target.position)>.3f)Suspend();
        if(!IsActive)
        {
            m_Body.position=target.position;m_Body.rotation=target.rotation;
            foreach(var collider in m_Colliders)collider.enabled=true;
            Physics.SyncTransforms();
            if(HasSolidOverlap()){foreach(var collider in m_Colliders)collider.enabled=false;return;}
            m_Body.isKinematic=false;m_Body.linearVelocity=Vector3.zero;m_Body.angularVelocity=Vector3.zero;IsActive=true;
        }
        var dt=Mathf.Clamp(deltaTime,.001f,.05f);
        var acceleration=((target.position-m_Body.position)*2800f-m_Body.linearVelocity*100f)/(1f+100f*dt+2800f*dt*dt);
        m_Body.AddForce(Vector3.ClampMagnitude(acceleration,100f),ForceMode.Acceleration);
        var error=target.rotation*Quaternion.Inverse(m_Body.rotation);
        if(error.w<0f)error=new Quaternion(-error.x,-error.y,-error.z,-error.w);
        error.ToAngleAxis(out var angle,out var axis);
        var torque=(axis*(angle*Mathf.Deg2Rad*4000f)-m_Body.angularVelocity*140f)/(1f+140f*dt+4000f*dt*dt);
        if(Finite(torque))m_Body.AddTorque(Vector3.ClampMagnitude(torque,2500f),ForceMode.Acceleration);
    }
    bool HasSolidOverlap()
    {
        foreach(var collider in m_Colliders)
        {
            var count=m_PhysicsScene.OverlapSphere(collider.transform.position,.16f,m_Overlaps,~0,QueryTriggerInteraction.Ignore);
            if(count==m_Overlaps.Length)return true;
            for(var i=0;i<count;i++)
            {
                var other=m_Overlaps[i];
                if(other==null||m_Colliders.Contains(other)||m_Ignored.Contains(other)||Physics.GetIgnoreLayerCollision(m_Layer,other.gameObject.layer))continue;
                if(Physics.ComputePenetration(collider,collider.transform.position,collider.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out var depth)&&depth>.0015f)return true;
            }
        }
        return false;
    }
    public void Suspend()
    {
        foreach(var collider in m_Colliders)collider.enabled=false;
        if(!m_Body.isKinematic){m_Body.linearVelocity=Vector3.zero;m_Body.angularVelocity=Vector3.zero;m_Body.isKinematic=true;}
        IsActive=false;
    }
    static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
    static bool Finite(Quaternion q)=>float.IsFinite(q.x)&&float.IsFinite(q.y)&&float.IsFinite(q.z)&&float.IsFinite(q.w)&&Quaternion.Dot(q,q)>.1f;
    public void Dispose()
    {
        Suspend();
        if(Application.isPlaying){UnityEngine.Object.Destroy(m_Root);UnityEngine.Object.Destroy(m_Material);}
        else{UnityEngine.Object.DestroyImmediate(m_Root);UnityEngine.Object.DestroyImmediate(m_Material);}
    }
}
