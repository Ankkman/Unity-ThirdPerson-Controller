using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem; // <-- Added to fix Mouse.current error

public class GunAimConstraintController : MonoBehaviour
{
    public MultiAimConstraint aimConstraint; // <-- Changed from AimConstraint to MultiAimConstraint
    public float aimWeight = 0.3f;
    public float normalWeight = 0f;
    public float transitionSpeed = 10f;

    private bool isAiming;
    private float targetWeight;

    void Update()
    {
        // Use the same aim state as your ThirdPersonAimController
        isAiming = Mouse.current.rightButton.isPressed;
        targetWeight = isAiming ? aimWeight : normalWeight;

        aimConstraint.weight = Mathf.Lerp(aimConstraint.weight, targetWeight, Time.deltaTime * transitionSpeed);
    }
}
