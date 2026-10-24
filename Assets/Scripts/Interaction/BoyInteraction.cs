using System.Collections;
using UnityEngine;

public class BoyInteraction : MonoBehaviour
{
    public float interactionDistance = 4f;
    public KeyCode interactionKey = KeyCode.E;

    public float turnSpeed = 8f;

    public float reachAngle = 55f;
    public float elbowBend = 25f;

    public float reachTime = 0.25f;
    public float holdTime = 0.25f;

    private Animator animator;

    private Transform rightUpperArm;
    private Transform rightLowerArm;

    private BoyMovement movement;
    private SimpleLimbMotion limbMotion;

    private bool interacting = false;
    
    public KeyCode upperDoorKey = KeyCode.R;

    void Start()
    {
        animator = GetComponent<Animator>();

        movement = GetComponent<BoyMovement>();
        limbMotion = GetComponent<SimpleLimbMotion>();

        rightUpperArm =
            animator.GetBoneTransform(HumanBodyBones.RightUpperArm);

        rightLowerArm =
            animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
    }

    void Update()
    {
        if (interacting)
            return;

        // E = lower fridge
        if (Input.GetKeyDown(KeyCode.E))
        {
            PlayerDoorInteractable lower =
                FindDoorByType(DoorType.FridgeLower);

            if (lower != null)
            {
                Debug.Log("Lower fridge selected");
                StartCoroutine(Interact(lower));
            }
        }

        // R = upper fridge
        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayerDoorInteractable upper =
                FindDoorByType(DoorType.FridgeUpper);

            if (upper != null)
            {
                Debug.Log("Upper fridge selected");
                StartCoroutine(Interact(upper));
            }
        }

        // C = closest cabinet
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("C pressed");

            PlayerDoorInteractable cabinet =
                FindNearestCabinet();

            if (cabinet != null)
            {
                Debug.Log(
                    "Found cabinet: " + cabinet.name
                );

                StartCoroutine(
                    Interact(cabinet)
                );
            }
            else
            {
                Debug.Log(
                    "No cabinet close enough"
                );
            }
        }
    }

    PlayerDoorInteractable FindNearbyDoor()
    {
        PlayerDoorInteractable[] doors =
            FindObjectsOfType<PlayerDoorInteractable>();

        PlayerDoorInteractable closest = null;
        float bestScore = Mathf.Infinity;

        Vector3 playerPosition = transform.position;

        foreach (PlayerDoorInteractable door in doors)
        {
            Vector3 targetPosition;

            if (door.interactionPoint != null)
            {
                targetPosition =
                    door.interactionPoint.position;
            }
            else
            {
                targetPosition =
                    door.transform.position;
            }

            // Horizontal distance is what determines whether
            // the boy is close enough to interact.
            Vector3 flatTarget = targetPosition;
            flatTarget.y = playerPosition.y;

            float horizontalDistance =
                Vector3.Distance(
                    playerPosition,
                    flatTarget
                );

            if (horizontalDistance > interactionDistance)
                continue;

            /*
             * Give less penalty to height.
             *
             * This allows the upper fridge door to be selected
             * even though its handle is above the boy.
             */
            float heightDifference =
                Mathf.Abs(
                    targetPosition.y -
                    (playerPosition.y + 1.2f)
                );

            float score =
                horizontalDistance +
                heightDifference * 0.25f;

            if (score < bestScore)
            {
                bestScore = score;
                closest = door;
            }
        }

        return closest;
    }
    
    PlayerDoorInteractable FindDoorByName(string doorName)
    {
        PlayerDoorInteractable[] doors =
            FindObjectsOfType<PlayerDoorInteractable>();

        foreach (PlayerDoorInteractable door in doors)
        {
            if (door.gameObject.name == doorName)
            {
                Vector3 flatDoor =
                    door.transform.position;

                flatDoor.y =
                    transform.position.y;

                float distance =
                    Vector3.Distance(
                        transform.position,
                        flatDoor
                    );

                if (distance <= interactionDistance)
                {
                    return door;
                }
            }
        }

        return null;
    }

    IEnumerator Interact(PlayerDoorInteractable target)
    {
        interacting = true;

        if (movement != null)
            movement.canMove = false;

        if (limbMotion != null)
            limbMotion.isInteracting = true;

        Quaternion armStart =
            rightUpperArm.localRotation;

        Quaternion elbowStart =
            rightLowerArm.localRotation;

        // Turn toward fridge/cabinet
        Vector3 direction =
            target.transform.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            while (Quaternion.Angle(
                       transform.rotation,
                       targetRotation) > 2f)
            {
                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        turnSpeed * Time.deltaTime
                    );

                yield return null;
            }
        }

        // Reach with right arm
        Quaternion armReach =
            armStart *
            Quaternion.AngleAxis(
                -reachAngle,
                Vector3.right
            );

        Quaternion elbowReach =
            elbowStart *
            Quaternion.AngleAxis(
                elbowBend,
                Vector3.right
            );

        float timer = 0f;

        while (timer < reachTime)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(timer / reachTime);

            rightUpperArm.localRotation =
                Quaternion.Slerp(
                    armStart,
                    armReach,
                    t
                );

            rightLowerArm.localRotation =
                Quaternion.Slerp(
                    elbowStart,
                    elbowReach,
                    t
                );

            yield return null;
        }

        // Open or close door
        yield return StartCoroutine(
            target.ToggleDoor()
        );

        yield return new WaitForSeconds(holdTime);

        // Return arm
        timer = 0f;

        while (timer < reachTime)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(timer / reachTime);

            rightUpperArm.localRotation =
                Quaternion.Slerp(
                    armReach,
                    armStart,
                    t
                );

            rightLowerArm.localRotation =
                Quaternion.Slerp(
                    elbowReach,
                    elbowStart,
                    t
                );

            yield return null;
        }

        rightUpperArm.localRotation = armStart;
        rightLowerArm.localRotation = elbowStart;

        if (limbMotion != null)
            limbMotion.isInteracting = false;

        if (movement != null)
            movement.canMove = true;

        interacting = false;
    }
    
    PlayerDoorInteractable FindDoorByType(
        DoorType type)
    {
        PlayerDoorInteractable[] doors =
            FindObjectsOfType<PlayerDoorInteractable>();

        foreach (PlayerDoorInteractable door in doors)
        {
            if (door.doorType != type)
                continue;

            Vector3 target =
                door.transform.position;

            target.y = transform.position.y;

            float distance =
                Vector3.Distance(
                    transform.position,
                    target
                );

            if (distance <= interactionDistance)
                return door;
        }

        return null;
    }


    PlayerDoorInteractable FindNearestCabinet()
    {
        PlayerDoorInteractable[] doors =
            FindObjectsOfType<PlayerDoorInteractable>();

        PlayerDoorInteractable bestDoor = null;
        float bestScore = Mathf.Infinity;

        foreach (PlayerDoorInteractable door in doors)
        {
            if (door.doorType != DoorType.Cabinet)
                continue;

            Vector3 targetPosition =
                door.interactionPoint != null
                    ? door.interactionPoint.position
                    : door.transform.position;

            Vector3 direction =
                targetPosition - transform.position;

            // Ignore height
            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance > interactionDistance)
                continue;

            direction.Normalize();

            // How much the boy is facing this door
            float facing =
                Vector3.Dot(
                    transform.forward,
                    direction
                );

            // Prefer doors in front of the boy
            if (facing < 0.2f)
                continue;

            // Smaller is better:
            // close + directly in front
            float score =
                distance + (1f - facing) * 2f;

            if (score < bestScore)
            {
                bestScore = score;
                bestDoor = door;
            }
        }

        return bestDoor;
    }
}