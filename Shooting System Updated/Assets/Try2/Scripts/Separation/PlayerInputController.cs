using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterMotor))]
public class PlayerInputController : MonoBehaviour
{
    [Header("Core References")]
    public CharacterMotor motor;
    public Camera mainCamera;
    public GameObject cameraTarget;
    public Transform worldAimTarget;
    public GameObject aimVirtualCamera;
    public GameObject crosshairUI;

    [Header("Camera Settings")]
    public float lookSensitivity = 0.2f;
    public float topClamp = 70.0f;
    public float bottomClamp = -30.0f;
    public LayerMask aimColliderMask;
    public float maxAimDistance = 100f;

    [Header("Mobile UI Configuration")]
    [SerializeField] private bool useMobileControls = true;
    public TouchField cameraTouchField; 
    public float mobileLookSensitivity = 0.5f;

    // Internal Input State
    private PlayerControls inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector2 mobileMoveInput;
    private bool isAiming;
    private bool isFiring;
    private bool isSprinting;
    
    private float cinemachineTargetYaw;
    private float cinemachineTargetPitch;

    private void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        inputActions = new PlayerControls();

        // PC Input Bindings
        inputActions.Player.Aim.performed += ctx => isAiming = true;
        inputActions.Player.Aim.canceled += ctx => isAiming = false;
        inputActions.Player.Fire.performed += ctx => isFiring = true;
        inputActions.Player.Fire.canceled += ctx => isFiring = false;
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
        inputActions.Player.Sprint.performed += ctx => isSprinting = true;
        inputActions.Player.Sprint.canceled += ctx => isSprinting = false;
        
        inputActions.Player.Jump.performed += ctx => motor.Jump();
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
        HandleInputGathering();
        HandleRaycastTarget();
        HandleCameraToggle();
        
        CommandMotor(); // This is where the Brain tells the Body what to do!
    }

    private void LateUpdate()
    {
        HandleCameraRotation();
    }

    // ==========================================
    // BRAIN LOGIC (Calculation & Vision)
    // ==========================================

    private void HandleInputGathering()
    {
        if (useMobileControls)
        {
            float h = Input.GetAxisRaw("Horizontal") + mobileMoveInput.x;
            float v = Input.GetAxisRaw("Vertical") + mobileMoveInput.y;
            moveInput = new Vector2(h, v);
            
            if (Input.GetKeyDown(KeyCode.LeftShift)) isSprinting = true;
            if (Input.GetKeyUp(KeyCode.LeftShift)) isSprinting = false;


            if (cameraTouchField != null && cameraTouchField.Pressed)
            {
                lookInput = cameraTouchField.TouchDist * mobileLookSensitivity; 
            }
            else
            {
                lookInput = Vector2.zero;
            }
        }
        else
        {
            moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            isSprinting = Input.GetKey(KeyCode.LeftShift);
            
            if (Input.GetKeyDown(KeyCode.H)) motor.TriggerHeal();
            if (Input.GetKeyDown(KeyCode.R)) motor.TriggerReload();
            if (Input.GetKeyDown(KeyCode.Alpha1)) motor.ToggleWeapon();
        }
    }

    private void HandleCameraRotation()
    {
        cinemachineTargetYaw += lookInput.x * lookSensitivity;
        cinemachineTargetPitch -= lookInput.y * lookSensitivity;
        cinemachineTargetPitch = Mathf.Clamp(cinemachineTargetPitch, bottomClamp, topClamp);

        cameraTarget.transform.rotation = Quaternion.Euler(cinemachineTargetPitch, cinemachineTargetYaw, 0.0f);
    }

    private void HandleRaycastTarget()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = mainCamera.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimColliderMask))
        {
            worldAimTarget.position = hit.point;
        }
        else
        {
            worldAimTarget.position = ray.GetPoint(maxAimDistance);
        }

        // --- THE RESTORED DEBUG LINES ---
        Debug.DrawLine(mainCamera.transform.position, worldAimTarget.position, Color.blue);
        
        if (motor != null && motor.equippedWeapon != null && motor.equippedWeapon.muzzlePoint != null)
        {
            Transform muzzle = motor.equippedWeapon.muzzlePoint;
            Debug.DrawLine(muzzle.position, worldAimTarget.position, Color.red);
            Debug.DrawRay(muzzle.position, muzzle.forward * maxAimDistance, Color.green);
        }
    }

    private void HandleCameraToggle()
    {
        if (aimVirtualCamera != null) aimVirtualCamera.SetActive(isAiming);
        if (crosshairUI != null) 
        {
            crosshairUI.SetActive(true);
            Image crosshairImg = crosshairUI.GetComponent<Image>();
            if (crosshairImg != null)
            {
                Color c = crosshairImg.color;
                c.a = (isAiming || isFiring) ? 1f : 0.7f; 
                crosshairImg.color = c;
            }
        }
    }

    // ==========================================
    // ISSUING COMMANDS TO THE MOTOR
    // ==========================================

    private void CommandMotor()
    {
        // 1. Calculate World-Space Move Direction based on Camera
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();
        
        Vector3 worldMoveDir = (forward * moveInput.y + right * moveInput.x).normalized;

        // 2. Send commands to the dumb puppet (CharacterMotor)
        motor.Move(worldMoveDir, isSprinting);
        motor.SetAimTarget(worldAimTarget.position);
        motor.SetCombatState(isAiming, isFiring);
    }

    // ==========================================
    // PUBLIC MOBILE UI GATEWAYS
    // ==========================================
    
    public void MobileUpdateMovement(Vector2 inputDirection) { mobileMoveInput = inputDirection; }
    public void MobileSetSprint(bool sprintState) { isSprinting = sprintState; }
    public void MobileTriggerHeal() { motor.TriggerHeal(); }
    public void MobileTriggerReload() { motor.TriggerReload(); }
    public void MobileToggleWeapon() { motor.ToggleWeapon(); }
    public void MobileSetAim(bool state) { isAiming = state; }
    public void MobileSetFire(bool state) { isFiring = state; }
}