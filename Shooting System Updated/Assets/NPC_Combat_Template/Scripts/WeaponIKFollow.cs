using UnityEngine;

public class WeaponIKFollow : MonoBehaviour
{
    [Header("Gun Sockets")]
    public Transform rightHandSocket;
    public Transform leftHandSocket;

    [Header("Rig IK Targets")]
    public Transform rightHandIKTarget;
    public Transform leftHandIKTarget;

    void LateUpdate()
    {
        // Match the position of the Rig targets to the gun sockets every single frame
        if (rightHandSocket != null && rightHandIKTarget != null)
        {
            rightHandIKTarget.position = rightHandSocket.position;
            rightHandIKTarget.rotation = rightHandSocket.rotation;
        }

        if (leftHandSocket != null && leftHandIKTarget != null)
        {
            leftHandIKTarget.position = leftHandSocket.position;
            leftHandIKTarget.rotation = leftHandSocket.rotation;
        }
    }
}
