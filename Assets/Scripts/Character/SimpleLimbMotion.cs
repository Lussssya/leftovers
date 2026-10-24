using UnityEngine;

public class SimpleLimbMotion : MonoBehaviour
{
    private Animator animator;

    private Transform hips;
    private Transform spine;

    private Transform leftUpperArm;
    private Transform rightUpperArm;
    private Transform leftLowerArm;
    private Transform rightLowerArm;

    private Transform leftUpperLeg;
    private Transform rightUpperLeg;
    private Transform leftLowerLeg;
    private Transform rightLowerLeg;

    private Transform leftFoot;
    private Transform rightFoot;

    private Quaternion hipsStartRotation;
    private Vector3 hipsStartPosition;
    private Quaternion spineStart;

    // Imported/original rotations
    private Quaternion leftUpperArmStart;
    private Quaternion rightUpperArmStart;
    private Quaternion leftLowerArmStart;
    private Quaternion rightLowerArmStart;

    private Quaternion leftUpperLegStart;
    private Quaternion rightUpperLegStart;
    private Quaternion leftLowerLegStart;
    private Quaternion rightLowerLegStart;

    private Quaternion leftFootStart;
    private Quaternion rightFootStart;

    // CUSTOM IDLE / WALK BASE POSE
    private Quaternion leftUpperArmNeutral;
    private Quaternion rightUpperArmNeutral;
    private Quaternion leftLowerArmNeutral;
    private Quaternion rightLowerArmNeutral;

    [Header("Walking")]
    public float walkSpeed = 7f;

    [Header("Legs")]
    public float legSwing = 28f;
    public float kneeBend = 20f;
    public float footLift = 10f;

    [Header("Arms")]
    public float armSwing = 18f;
    public float elbowBend = 12f;

    [Header("Idle Arm Pose")]
    public float armDownAngle = 92f;
    public float armBackAngle = 0f;
    public float idleElbowBend = 0f;

    [Header("Body")]
    public float bodyBob = 0.02f;
    public float hipRotation = 3f;
    public float bodyLean = 2f;

    private float cycle = 0f;
    
    public bool isInteracting = false;

    void Start()
    {
        animator = GetComponent<Animator>();

        hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);

        leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);

        leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);

        leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

        hipsStartPosition = hips.localPosition;
        hipsStartRotation = hips.localRotation;
        spineStart = spine.localRotation;

        leftUpperArmStart = leftUpperArm.localRotation;
        rightUpperArmStart = rightUpperArm.localRotation;
        leftLowerArmStart = leftLowerArm.localRotation;
        rightLowerArmStart = rightLowerArm.localRotation;

        leftUpperLegStart = leftUpperLeg.localRotation;
        rightUpperLegStart = rightUpperLeg.localRotation;
        leftLowerLegStart = leftLowerLeg.localRotation;
        rightLowerLegStart = rightLowerLeg.localRotation;

        leftFootStart = leftFoot.localRotation;
        rightFootStart = rightFoot.localRotation;

        BuildNeutralPose();

        // Apply neutral pose immediately so the boy does not stay in T-pose
        ApplyNeutralIdlePose();
    }

    void Update()
    {
        if (isInteracting)
        {
            return;
        }
        
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        bool walking =
            Mathf.Abs(horizontal) > 0.01f ||
            Mathf.Abs(vertical) > 0.01f;

        if (walking)
        {
            AnimateWalk();
        }
        else
        {
            ReturnToIdle();
        }
    }

    void BuildNeutralPose()
    {
        // 1) bring arms down
        // 2) rotate them back toward the sides of the body

        leftUpperArmNeutral =
            leftUpperArmStart *
            Quaternion.AngleAxis(-65f, Vector3.left) *
            Quaternion.AngleAxis(30f, Vector3.down);

        rightUpperArmNeutral =
            rightUpperArmStart *
            Quaternion.AngleAxis(65f, Vector3.right) *
            Quaternion.AngleAxis(-30f, Vector3.down);

        // Relaxed lower arms
        leftLowerArmNeutral =
            leftLowerArmStart *
            Quaternion.AngleAxis(5f, Vector3.right);

        rightLowerArmNeutral =
            rightLowerArmStart *
            Quaternion.AngleAxis(5f, Vector3.right);
    }

    void ApplyNeutralIdlePose()
    {
        leftUpperArm.localRotation = leftUpperArmNeutral;
        rightUpperArm.localRotation = rightUpperArmNeutral;
        leftLowerArm.localRotation = leftLowerArmNeutral;
        rightLowerArm.localRotation = rightLowerArmNeutral;
    }

    void AnimateWalk()
    {
        cycle += Time.deltaTime * walkSpeed;

        float wave = Mathf.Sin(cycle);
        float oppositeWave = -wave;

        // LEGS
        leftUpperLeg.localRotation =
            leftUpperLegStart *
            Quaternion.AngleAxis(wave * legSwing, Vector3.right);

        rightUpperLeg.localRotation =
            rightUpperLegStart *
            Quaternion.AngleAxis(oppositeWave * legSwing, Vector3.right);

        float leftKneeAmount = Mathf.Max(0f, -wave);
        float rightKneeAmount = Mathf.Max(0f, wave);

        leftLowerLeg.localRotation =
            leftLowerLegStart *
            Quaternion.AngleAxis(leftKneeAmount * kneeBend, Vector3.right);

        rightLowerLeg.localRotation =
            rightLowerLegStart *
            Quaternion.AngleAxis(rightKneeAmount * kneeBend, Vector3.right);

        leftFoot.localRotation =
            leftFootStart *
            Quaternion.AngleAxis(-wave * footLift, Vector3.right);

        rightFoot.localRotation =
            rightFootStart *
            Quaternion.AngleAxis(wave * footLift, Vector3.right);

        // ARMS - move from neutral pose, not T-pose
        leftUpperArm.localRotation =
            leftUpperArmNeutral *
            Quaternion.AngleAxis(-wave * armSwing, Vector3.right);

        rightUpperArm.localRotation =
            rightUpperArmNeutral *
            Quaternion.AngleAxis(wave * armSwing, Vector3.right);

        float leftElbow =
            idleElbowBend + Mathf.Max(0f, wave) * elbowBend;

        float rightElbow =
            idleElbowBend + Mathf.Max(0f, -wave) * elbowBend;

        leftLowerArm.localRotation =
            leftLowerArmNeutral *
            Quaternion.AngleAxis(leftElbow, Vector3.right);

        rightLowerArm.localRotation =
            rightLowerArmNeutral *
            Quaternion.AngleAxis(rightElbow, Vector3.right);

        // BODY
        float bob = Mathf.Abs(Mathf.Sin(cycle)) * bodyBob;

        hips.localPosition =
            hipsStartPosition + Vector3.up * bob;

        hips.localRotation =
            hipsStartRotation *
            Quaternion.AngleAxis(wave * hipRotation, Vector3.up);

        spine.localRotation =
            spineStart *
            Quaternion.AngleAxis(bodyLean, Vector3.right);
    }

    void ReturnToIdle()
    {
        cycle = 0f;

        float smooth = 8f * Time.deltaTime;

        hips.localPosition =
            Vector3.Lerp(
                hips.localPosition,
                hipsStartPosition,
                smooth
            );

        hips.localRotation =
            Quaternion.Slerp(
                hips.localRotation,
                hipsStartRotation,
                smooth
            );

        spine.localRotation =
            Quaternion.Slerp(
                spine.localRotation,
                spineStart,
                smooth
            );

        leftUpperArm.localRotation =
            Quaternion.Slerp(
                leftUpperArm.localRotation,
                leftUpperArmNeutral,
                smooth
            );

        rightUpperArm.localRotation =
            Quaternion.Slerp(
                rightUpperArm.localRotation,
                rightUpperArmNeutral,
                smooth
            );

        leftLowerArm.localRotation =
            Quaternion.Slerp(
                leftLowerArm.localRotation,
                leftLowerArmNeutral,
                smooth
            );

        rightLowerArm.localRotation =
            Quaternion.Slerp(
                rightLowerArm.localRotation,
                rightLowerArmNeutral,
                smooth
            );

        leftUpperLeg.localRotation =
            Quaternion.Slerp(
                leftUpperLeg.localRotation,
                leftUpperLegStart,
                smooth
            );

        rightUpperLeg.localRotation =
            Quaternion.Slerp(
                rightUpperLeg.localRotation,
                rightUpperLegStart,
                smooth
            );

        leftLowerLeg.localRotation =
            Quaternion.Slerp(
                leftLowerLeg.localRotation,
                leftLowerLegStart,
                smooth
            );

        rightLowerLeg.localRotation =
            Quaternion.Slerp(
                rightLowerLeg.localRotation,
                rightLowerLegStart,
                smooth
            );

        leftFoot.localRotation =
            Quaternion.Slerp(
                leftFoot.localRotation,
                leftFootStart,
                smooth
            );

        rightFoot.localRotation =
            Quaternion.Slerp(
                rightFoot.localRotation,
                rightFootStart,
                smooth
            );
    }
}