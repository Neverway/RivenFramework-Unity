//==========================================( Neverway 2025 )=========================================================//
// Author
//  Liz M.
// 
// Contributors: 
//  Connorses, Errynei, Soulex
//
//====================================================================================================================//

using RivenFramework;
using UnityEngine;

public class FPPawn_Player : FPPawn
{
    #region========================================( Variables )======================================================//
    /*-----[ Inspector Variables ]------------------------------------------------------------------------------------*/
    /*----------------------------------------------------------------------------------------------------------------*/
    
    
    /*-----[ External Variables ]-------------------------------------------------------------------------------------*/
    /*----------------------------------------------------------------------------------------------------------------*/

    
    /*-----[ Internal Variables ]-------------------------------------------------------------------------------------*/
    /*----------------------------------------------------------------------------------------------------------------*/
    private Vector3 moveDirection;
    private Vector2 lookRotation;
    
    /*-----[ Reference Variables ]------------------------------------------------------------------------------------*/
    /*----------------------------------------------------------------------------------------------------------------*/
    private GI_WidgetManager widgetManager;
    private new FPPawnActions action = new FPPawnActions();
    private InputActions.FirstPersonActions inputActions;
    [SerializeField] private GameObject DeathScreenWidget;
    
    #endregion


    #region=======================================( Functions )=======================================================//
    /*-----[ Mono Functions ]-----------------------------------------------------------------------------------------*/
    /*----------------------------------------------------------------------------------------------------------------*/
    private void UpdatePauseMenu()
    {
        if (!widgetManager)
        {
            widgetManager = FindObjectOfType<GI_WidgetManager>();
            if (!widgetManager) return;
        }
        isPaused = widgetManager.GetExistingWidget("WB_Pause");
        
        // Pause Game
        if (inputActions.Menu.WasPressedThisFrame())
        {
            widgetManager.ToggleWidget("WB_Pause");
        }
        
        // Lock mouse when unpaused, unlock when paused
        if (isPaused)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public new void Awake()
    {
        base.Awake();
        
        // Subscribe to events
        OnPawnDeath += () => { OnDeath(); };
        
        // Setup inputs
        inputActions = new InputActions().FirstPerson;
        inputActions.Enable();
        
        // Enable the view camera
        action.EnableViewCamera(this, true);
        
        // Disable the mouse cursor
        
    }

    public void Update()
    {
        // Pausing
        UpdatePauseMenu();
        
        
        if (isPaused || isDead) return;
        UpdateMovement();
        UpdateRotation();
        
        // Kill bind
        if (Input.GetKeyDown(KeyCode.Delete)) Kill();
        
        // Jumping
        if (inputActions.Jump.WasPressedThisFrame()) action.Jump(this);
        
        // Crouching
        if (inputActions.Crouch.IsPressed())
        {
            action.Crouch(this, true);
        }
        else
        {
            action.Crouch(this, false);
        }
        
        // Interact 
        if (inputActions.Interact.WasPressedThisFrame()) action.Interact(this, interactionPrefab, viewPoint.transform);
        
        // Throw held object
        if (inputActions.ItemAction1.WasPressedThisFrame() && physObjectAttachmentPoint.attachedObject)
        {
            action.ThrowPhysProp(this);
        }
        
        // Switch item
        if (inputActions.ItemSwapNext.WasPressedThisFrame()) action.ItemSwapNext(this);
        if (inputActions.ItemSwapPrevious.WasPressedThisFrame()) action.ItemSwapPrevious(this);
        
        // Use Item
        var inventory = GetComponentInChildren<Pawn_Inventory>();
        if (inputActions.ItemAction1.WasPressedThisFrame()) action.ItemUseAction(inventory, 0);
        if (inputActions.ItemAction2.WasPressedThisFrame()) action.ItemUseAction(inventory, 1);
        if (inputActions.ItemAction3.WasPressedThisFrame()) action.ItemUseAction(inventory, 2);
        if (inputActions.ItemAction1.WasReleasedThisFrame()) action.ItemUseAction(inventory, 0, "release");
        if (inputActions.ItemAction2.WasReleasedThisFrame()) action.ItemUseAction(inventory, 1, "release");
        if (inputActions.ItemAction3.WasReleasedThisFrame()) action.ItemUseAction(inventory, 2, "release");
    }

    public void FixedUpdate()
    {
        if (isPaused || isDead) return;
        ApplyMovement();
        ApplyRotation();
    }

    /*-----[ Internal Functions ]-------------------------------------------------------------------------------------*/
    /*----------------------------------------------------------------------------------------------------------------*/

    
    private void UpdateMovement()
    {
        moveDirection = new Vector3(inputActions.Move.ReadValue<Vector2>().x, 0, inputActions.Move.ReadValue<Vector2>().y);
    }
    private void ApplyMovement()
    {
        action.Move(this, moveDirection);
    }

    private void UpdateRotation()
    {
        // Get the look speed
        float horizontalLookSpeed = 3; // = applicationSettings.currentSettingsData.horizontalLookSpeed
        float verticalLookSpeed = 2; // = applicationSettings.currentSettingsData.verticalLookSpeed
        
        // Separate multipliers for mouse and joystick
        float mouseMultiplier = 0.01f;
        float joystickMultiplier = 0.2f;

        // Determine the input method (mouse or joystick)
        bool isUsingMouse = false;
        if (inputActions.LookAxis.IsInProgress())
        {
            if (inputActions.LookAxis.activeControl.device.name == "Mouse")
            {
                isUsingMouse = true;
            }
        }

        // Apply the appropriate multiplier
        var multiplier = isUsingMouse ? mouseMultiplier : joystickMultiplier;
        
        // Store the rotation values
        lookRotation.x -= inputActions.LookAxis.ReadValue<Vector2>().y * (20 * verticalLookSpeed) * multiplier;
        lookRotation.y += inputActions.LookAxis.ReadValue<Vector2>().x * (20 * horizontalLookSpeed) * multiplier;
        lookRotation.x = Mathf.Clamp(lookRotation.x, -90f, 90f);
    }
    private void ApplyRotation()
    {
        action.FaceTowardsDirection(this, viewPoint, lookRotation);
    }


    private void OnDeath()
    {
        // Drop held props
        if (physObjectAttachmentPoint)
        {
            if (physObjectAttachmentPoint.attachedObject)
            {
                if (physObjectAttachmentPoint.attachedObject.GetComponent<Object_PhysPickup>())
                {
                    physObjectAttachmentPoint.attachedObject.GetComponent<Object_PhysPickup>().ToggleHeld();
                }
            }
        }

        // Remove the HUD
        Destroy(widgetManager.GetExistingWidget("WB_HUD"));
        // Add the respawn HUD
        widgetManager.AddWidget(DeathScreenWidget);

        // Play the death animation
        if (GetComponent<Animator>()) GetComponent<Animator>().Play("Death");
    }

    /*-----[ External Functions ]-------------------------------------------------------------------------------------*/
    /*----------------------------------------------------------------------------------------------------------------*/


    #endregion
}
