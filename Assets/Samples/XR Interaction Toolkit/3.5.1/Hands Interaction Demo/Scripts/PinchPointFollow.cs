#if XR_HANDS_1_2_OR_NEWER
using Unity.XR.CoreUtils.Bindings;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Tweenables.Primitives;
#endif
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.Hands
{
    /// <summary>
    /// A class that follows the pinch point between the thumb and index finger using XR Hand Tracking.
    /// It updates its position to the midpoint between the thumb and index tip while optionally adjusting its rotation
    /// to look at a specified target. The rotation towards the target can also be smoothly interpolated over time.
    /// </summary>
    public class PinchPointFollow : MonoBehaviour
    {
        [Header("Events")]
        [SerializeField]
        [Tooltip("The XR Hand Tracking Events component that will be used to subscribe to hand tracking events.")]
#if XR_HANDS_1_2_OR_NEWER
        XRHandTrackingEvents m_XRHandTrackingEvents;
#else
        Object m_XRHandTrackingEvents;
#endif

        [Header("Interactor reference (Pick one)")]
        [SerializeField]
        [Tooltip("The transform will use the XRRayInteractor endpoint position to calculate the transform rotation.")]
        XRRayInteractor m_RayInteractor;

        [SerializeField]
        [Tooltip("The transform will use the NearFarInteractor endpoint position to calculate the transform rotation.")]
        NearFarInteractor m_NearFarInteractor;

        [Header("Rotation Config")]
        [SerializeField]
        [Tooltip("The transform to match the rotation of.")]
        Transform m_TargetRotation;

        [SerializeField]
        [Tooltip("How fast to match rotation (0 means no rotation smoothing.)")]
        [Range(0f, 32f)]
#pragma warning disable CS0414 // Field assigned but its value is never used -- Keep to retain serialized value when XR Hands is not installed
        float m_RotationSmoothingSpeed = 12f;
#pragma warning restore CS0414

#if XR_HANDS_1_2_OR_NEWER
        bool m_HasTargetRotationTransform;
        IXRRayProvider m_RayProvider;
        bool m_HasRayProvider;
        OneEuroFilterVector3 m_OneEuroFilterVector3;
#if XR_HANDS_1_5_OR_NEWER
        InputSystem.XR.TrackedPoseDriver m_AimPoseDriver;
        bool m_DrivesAimPose;
#endif

#pragma warning disable CS0618 // Type or member is obsolete
        readonly QuaternionTweenableVariable m_QuaternionTweenableVariable = new QuaternionTweenableVariable();
#pragma warning restore CS0618 // Type or member is obsolete
        readonly BindingsGroup m_BindingsGroup = new BindingsGroup();
#endif

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void OnEnable()
        {
#if XR_HANDS_1_2_OR_NEWER
            if (m_XRHandTrackingEvents != null)
                m_XRHandTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);

            m_OneEuroFilterVector3 = new OneEuroFilterVector3(transform.localPosition);
            if (m_RayInteractor != null)
            {
                m_RayProvider = m_RayInteractor;
                m_HasRayProvider = true;
            }
            if (m_NearFarInteractor != null)
            {
                m_RayProvider = m_NearFarInteractor;
                m_HasRayProvider = true;
            }
            m_HasTargetRotationTransform = m_TargetRotation != null;
#if XR_HANDS_1_5_OR_NEWER
            m_AimPoseDriver = m_HasTargetRotationTransform
                ? m_TargetRotation.GetComponent<InputSystem.XR.TrackedPoseDriver>()
                : null;
#endif
            m_BindingsGroup.AddBinding(m_QuaternionTweenableVariable.Subscribe(newValue => transform.rotation = newValue));
#else
            Debug.LogWarning("PinchPointFollow requires XR Hands (com.unity.xr.hands) 1.2.0 or newer. Disabling component.", this);
            enabled = false;
#endif
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void OnDisable()
        {
#if XR_HANDS_1_2_OR_NEWER
            m_BindingsGroup.Clear();
            if (m_XRHandTrackingEvents != null)
                m_XRHandTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);
#if XR_HANDS_1_5_OR_NEWER
            if (m_DrivesAimPose && m_AimPoseDriver != null)
                m_AimPoseDriver.enabled = true;
            m_DrivesAimPose = false;
#endif
#endif
        }

#if XR_HANDS_1_2_OR_NEWER
        static bool TryGetPinchPosition(XRHandJointsUpdatedEventArgs args, out Vector3 position)
        {
#if XR_HANDS_1_5_OR_NEWER
            if (args.subsystem != null)
            {
                var commonHandGestures = args.hand.handedness == Handedness.Left
                    ? args.subsystem.leftHandCommonGestures
                    : args.hand.handedness == Handedness.Right
                        ? args.subsystem.rightHandCommonGestures
                        : null;
                if (commonHandGestures != null && commonHandGestures.TryGetPinchPose(out var pinchPose))
                {
                    // Protect against platforms returning bad data like (NaN, NaN, NaN)
                    if (IsFinite(pinchPose.position))
                    {
                        position = pinchPose.position;
                        return true;
                    }
                }
            }
#endif

            var thumbTip = args.hand.GetJoint(XRHandJointID.ThumbTip);
            if (!thumbTip.TryGetPose(out var thumbTipPose))
            {
                position = Vector3.zero;
                return false;
            }

            var indexTip = args.hand.GetJoint(XRHandJointID.IndexTip);
            if (!indexTip.TryGetPose(out var indexTipPose))
            {
                position = Vector3.zero;
                return false;
            }

            position = Vector3.Lerp(thumbTipPose.position, indexTipPose.position, 0.5f);
            return IsFinite(position);
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

#if XR_HANDS_1_5_OR_NEWER
        static bool TryGetHandAim(XRHandJointsUpdatedEventArgs args, out Pose aim)
        {
            aim = Pose.identity;
            var hand = args.hand;
            if (!hand.isTracked || (hand.handedness != Handedness.Left && hand.handedness != Handedness.Right))
                return false;

            var hasWrist = hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out var wrist) && IsFinite(wrist.position);
            var hasIndex = hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out var index) && IsFinite(index.position);
            var direction = hasWrist && hasIndex ? index.position - wrist.position : Vector3.zero;
            if (direction.sqrMagnitude > 0.000001f)
            {
                // The native gesture ray can point across the hand even while its aim flags are valid.
                // Use the same tracked hand axis across frames so crossing that angle cannot flip the ray.
                var up = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.99f
                    ? Vector3.forward
                    : Vector3.up;
                aim = new Pose(index.position, Quaternion.LookRotation(direction, up));
                return true;
            }

            var gestures = hand.handedness == Handedness.Left
                ? args.subsystem?.leftHandCommonGestures
                : args.subsystem?.rightHandCommonGestures;
            if (gestures != null && gestures.TryGetAimPose(out aim) && UsableAim(aim, hasWrist, wrist.position))
                return true;

            var device = hand.handedness == Handedness.Left ? MetaAimHand.left : MetaAimHand.right;
            if (device != null && device.isTracked.ReadValue() > 0f &&
                ((MetaAimFlags)device.aimFlags.ReadValue() & MetaAimFlags.Valid) != 0)
            {
                aim = new Pose(device.devicePosition.ReadValue(), device.deviceRotation.ReadValue());
                if (UsableAim(aim, hasWrist, wrist.position))
                    return true;
            }

            return false;
        }

        static bool UsableAim(Pose aim, bool hasWrist, Vector3 wrist)
        {
            var norm = Quaternion.Dot(aim.rotation, aim.rotation);
            if (!IsFinite(aim.position) || !float.IsFinite(norm) || Mathf.Abs(norm - 1f) > 0.05f)
                return false;
            if (aim.position.sqrMagnitude < 0.000001f && Quaternion.Angle(aim.rotation, Quaternion.identity) < 0.01f)
                return false;
            return !hasWrist || (aim.position - wrist).sqrMagnitude <= 0.25f * 0.25f;
        }
#endif

        public void OnJointsUpdated(XRHandJointsUpdatedEventArgs args)
        {
            if (!isActiveAndEnabled)
                return;

            var hasPinchPosition = TryGetPinchPosition(args, out var targetPos);
            var deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);

#if XR_HANDS_1_5_OR_NEWER
            if (m_HasTargetRotationTransform && TryGetHandAim(args, out var aimPose))
            {
                if (m_AimPoseDriver != null && m_AimPoseDriver.enabled)
                {
                    m_AimPoseDriver.enabled = false;
                    m_DrivesAimPose = true;
                }

                // The curve and its visible origin share the same tracked pinch and direction.
                transform.localPosition = m_OneEuroFilterVector3.Filter(hasPinchPosition ? targetPos : aimPose.position, deltaTime);
                var trackingOrigin = transform.parent;
                var worldRotation = trackingOrigin != null ? trackingOrigin.rotation * aimPose.rotation : aimPose.rotation;
                m_TargetRotation.SetPositionAndRotation(transform.position, worldRotation);
                transform.rotation = worldRotation;
                return;
            }
#endif

            if (!hasPinchPosition)
                return;
            transform.localPosition = m_OneEuroFilterVector3.Filter(targetPos, deltaTime);

            if (m_HasTargetRotationTransform && m_HasRayProvider)
            {
                // Given that the ray endpoint is in world space, we need to use the world space transform of this point to determine the target rotation.
                // This allows us to keep orientation consistent when moving the XR Origin for locomotion.
                var targetDir = (m_RayProvider.rayEndPoint - transform.position).normalized;
                if (targetDir != Vector3.zero)
                {
                    // Use the parent Transform's up vector if available, otherwise use the world up vector.
                    // The assumption is the parent Transform matches the XR Origin rotation.
                    // This allows the XR Origin to teleport to angled surfaces or upside down surfaces
                    // and the visual will still be correct relative to the application's ground.
                    var upwards = Vector3.up;
                    var parentTransform = transform.parent;
                    if (!(parentTransform is null))
                        upwards = parentTransform.up;

                    var targetRot = Quaternion.LookRotation(targetDir, upwards);

                    // If there aren't any major swings in rotation, follow the target rotation.
                    if (Vector3.Dot(m_TargetRotation.forward, targetDir) > 0.5f)
                        m_QuaternionTweenableVariable.target = targetRot;
                }
                else
                {
                    m_QuaternionTweenableVariable.target = m_TargetRotation.rotation;
                }

                var tweenTarget = m_RotationSmoothingSpeed > 0f ? m_RotationSmoothingSpeed * Time.deltaTime : 1f;
                m_QuaternionTweenableVariable.HandleTween(tweenTarget);
            }
        }
#endif
    }
}
