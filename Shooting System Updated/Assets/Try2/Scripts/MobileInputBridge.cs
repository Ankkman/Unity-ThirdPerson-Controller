using UnityEngine;

public class MobileInputBridge : MonoBehaviour
{
    [SerializeField] private PlayerInputController targetPlayer;

    // Call this method directly from the Joystick UI Component
    public void ReceiveMovementInput(Vector2 directionalInput)
    {
        if (targetPlayer == null) return;

        // Stream the input data to your controller
        targetPlayer.MobileUpdateMovement(directionalInput);
        
        // Sprint handling based on joystick push distance
        if (directionalInput.magnitude > 0.85f)
        {
            targetPlayer.MobileSetSprint(true);
        }
        else
        {
            targetPlayer.MobileSetSprint(false);
        }
    }
}
