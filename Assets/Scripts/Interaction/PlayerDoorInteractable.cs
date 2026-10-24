using System.Collections;
using UnityEngine;

public enum DoorType
{
    FridgeLower,
    FridgeUpper,
    Cabinet
}

public class PlayerDoorInteractable : MonoBehaviour
{
    [Header("Type")]
    public DoorType doorType;

    [Header("Door")]
    public Transform door;

    [Header("Interaction")]
    public Transform interactionPoint;

    [Header("Opening")]
    public float openAngle = 90f;
    public float openSpeed = 3f;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool isOpen = false;
    private bool moving = false;

    void Start()
    {
        if (door == null)
            door = transform;

        closedRotation = door.localRotation;

        openRotation =
            closedRotation *
            Quaternion.Euler(0f, openAngle, 0f);
    }

    public IEnumerator ToggleDoor()
    {
        if (moving)
            yield break;

        moving = true;

        Quaternion target =
            isOpen ? closedRotation : openRotation;

        while (
            Quaternion.Angle(
                door.localRotation,
                target
            ) > 0.5f
        )
        {
            door.localRotation =
                Quaternion.Slerp(
                    door.localRotation,
                    target,
                    openSpeed * Time.deltaTime
                );

            yield return null;
        }

        door.localRotation = target;

        isOpen = !isOpen;
        moving = false;
    }
}