// using UnityEngine;
// using UnityEngine.InputSystem;
// using Cinemachine; // Fixes the first error (removes "Unity.")

// public class ThirdPersonAimController : MonoBehaviour
// {
//     [Header("References")]
//     public CinemachineVirtualCamera aimVirtualCamera; // Fixes the second error
//     public Transform aimTarget;
//     public Animator animator;

//     [Header("Aim Settings")]
//     public float maxAimDistance = 100f;

//     [Header("Camera Zoom")]
//     public float normalFOV = 40f;
//     public float aimFOV = 30f;
//     public float fovSpeed = 10f;

//     private bool isAiming;

//     public static Transform CurrentAimTarget;

//     [Header("Character Rotation")]
//     public float rotationSpeed = 10f;

//     private void Awake()
//     {
//         CurrentAimTarget = aimTarget;
//     }


//     private void Update()
//     {
//         isAiming = Mouse.current.rightButton.isPressed;

//         animator.SetBool("IsAiming", isAiming);

//         UpdateAimTarget();

//         if (aimVirtualCamera != null)
//         {
//             float targetFOV = isAiming ? aimFOV : normalFOV;

//             // Accesses the FOV variable structure used in traditional Cinemachine
//             aimVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(
//                 aimVirtualCamera.m_Lens.FieldOfView,
//                 targetFOV,
//                 Time.deltaTime * fovSpeed);
//         }

//         if (isAiming)
//         {
//             RotateTowardsCamera();
//         }

//     }

//     private void RotateTowardsCamera()
//     {
//         Vector3 direction = Camera.main.transform.forward;

//         direction.y = 0f;

//         if (direction.sqrMagnitude < 0.01f)
//             return;

//         Quaternion targetRotation =
//             Quaternion.LookRotation(direction);

//         transform.rotation = Quaternion.Slerp(
//             transform.rotation,
//             targetRotation,
//             rotationSpeed * Time.deltaTime);
//     }

//     void UpdateAimTarget()
//     {
//         Vector2 screenCenter = new Vector2(
//             Screen.width * 0.5f,
//             Screen.height * 0.5f);

//         Ray ray = Camera.main.ScreenPointToRay(screenCenter);

//         Debug.DrawRay(
//             ray.origin,
//             ray.direction * maxAimDistance,
//             Color.red);

//         if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance))
//         {
//             aimTarget.position = hit.point;

//             Debug.Log("Hit: " + hit.collider.name);
//         }
//         else
//         {
//             aimTarget.position =
//                 ray.origin + ray.direction * maxAimDistance;
//         }
//     }
// }



using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine; // Fixes the first error (removes "Unity.")

public class ThirdPersonAimController : MonoBehaviour
{
    [Header("References")]
    public CinemachineVirtualCamera aimVirtualCamera; // Fixes the second error
    public Transform aimTarget;
    public Animator animator;

    [Header("Aim Settings")]
    public float maxAimDistance = 100f;

    [Header("Camera Zoom")]
    public float normalFOV = 40f;
    public float aimFOV = 30f;
    public float fovSpeed = 10f;

    private bool isAiming;

    public static Transform CurrentAimTarget;

    [Header("Character Rotation")]
    public float rotationSpeed = 10f;

    // Added new variables to control rotation manually via Inspector
    [Header("Manual Adjustments")]
    [Tooltip("Check this to add a custom offset angle to the player's aiming orientation.")]
    public bool useManualOffset = false;
    [Tooltip("Manually twist the player's target direction in degrees (e.g., 90 to face right, -45 to angle slightly left).")]
    [Range(-180f, 180f)]
    public float manualRotationOffset = 0f;

    private void Awake()
    {
        CurrentAimTarget = aimTarget;
    }

    private void Update()
    {
        isAiming = Mouse.current.rightButton.isPressed;

        animator.SetBool("IsAiming", isAiming);

        UpdateAimTarget();

        if (aimVirtualCamera != null)
        {
            float targetFOV = isAiming ? aimFOV : normalFOV;

            // Accesses the FOV variable structure used in traditional Cinemachine
            aimVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(
                aimVirtualCamera.m_Lens.FieldOfView,
                targetFOV,
                Time.deltaTime * fovSpeed);
        }

        if (isAiming)
        {
            RotateTowardsCamera();
        }
    }

    private void RotateTowardsCamera()
    {
        Vector3 direction = Camera.main.transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        // Create the base look rotation from the camera direction
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // If enabled, manually offset the angle by multiplying it with an Euler rotation
        if (useManualOffset)
        {
            targetRotation *= Quaternion.Euler(0f, manualRotationOffset, 0f);
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    void UpdateAimTarget()
    {
        Vector2 screenCenter = new Vector2(
            Screen.width * 0.5f,
            Screen.height * 0.5f);

        Ray ray = Camera.main.ScreenPointToRay(screenCenter);

        Debug.DrawRay(
            ray.origin,
            ray.direction * maxAimDistance,
            Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance))
        {
            aimTarget.position = hit.point;

            Debug.Log("Hit: " + hit.collider.name);
        }
        else
        {
            aimTarget.position =
                ray.origin + ray.direction * maxAimDistance;
        }
    }
}
