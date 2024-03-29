//===================== (Neverway 2024) Written by Liz M. =====================
//
// Purpose:
// Notes:
//
//=============================================================================

using System;
using System.Collections;
using UnityEngine;

public class Pawn : MonoBehaviour
{
    //=-----------------=
    // Public Variables
    //=-----------------=
    public PawnController currentController;

    public PlayerState defaultState;
    public PlayerStateData currentState = new PlayerStateData();

    public bool isPossessed;
    public bool isPaused;
    public bool wasPaused; // used to restore a paused state when all pawns isPaused state is modified by the game instance
    public bool isInvulnerable;
    public bool isDead;
    public bool isNearInteractable; // Imported from old system

    public event Action OnPawnHurt;
    public event Action OnPawnHeal;
    public event Action OnPawnDeath;
    
    [Tooltip("The collision layers that will be checked when testing if the entity is grounded")]
    [SerializeField] private LayerMask groundMask;


    //=-----------------=
    // Private Variables
    //=-----------------=


    //=-----------------=
    // Reference Variables
    //=-----------------=
    private GameInstance gameInstance;
    public Quaternion faceDirection; // Imported from old system


    //=-----------------=
    // Mono Functions
    //=-----------------=
    private void Awake()
    {
        // initialize currentState;
        currentState = defaultState.data;
        currentController.PawnAwake(this);
    }

    private void Update()
    {
        gameInstance = FindObjectOfType<GameInstance>();
        CheckCameraState();
        if (isDead) return;
        currentController.PawnUpdate(this);
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        currentController.PawnFixedUpdate(this);
    }
    

    //=-----------------=
    // Internal Functions
    //=-----------------=
    private void CheckCameraState()
    {
        var viewCamera = GetComponentInChildren<Camera>(true);
        if (IsPlayerControlled() && viewCamera)
        {
            viewCamera.gameObject.SetActive(true);
        }
        else if (viewCamera)
        {
            viewCamera.gameObject.SetActive(false);
        }
    }

    private IEnumerator InvulnerabilityCooldown()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(1);
        isInvulnerable = false;
    }


    //=-----------------=
    // External Functions
    //=-----------------=
    public bool IsGrounded3D()
    {
        return Physics.CheckSphere(transform.position - new Vector3(0, 0, 0), 0.4f, groundMask);
    }
    
    public bool IsPlayerControlled()
    {
        if (!gameInstance) gameInstance = FindObjectOfType<GameInstance>();
        return gameInstance.PlayerControllerClasses.Contains(currentController);
    }

    public void Move(Vector3 _movement, string _mode)
    {
        if (_mode == "translate") transform.Translate(_movement * (
            currentState.movementSpeed * Time.deltaTime));
    }
    
    public void Move(Vector3 _movement, string _mode, float _movementSpeed)
    {
        if (_mode == "translate") transform.Translate(_movement * (_movementSpeed * Time.deltaTime));
    }
    
    public void ModifyHealth(float _value)
    {
        if (isInvulnerable) return;
        StartCoroutine(InvulnerabilityCooldown());
        switch (_value)
        {
            case > 0:
                OnPawnHeal?.Invoke();
                isDead = false;
                if (currentState.sounds.heal) GetComponent<AudioSource_PitchVarienceModulator>().PlaySound(currentState.sounds.heal);
                break;
            case < 0:
                if (isDead) return;
                OnPawnHurt?.Invoke();
                if (currentState.sounds.hurt) GetComponent<AudioSource_PitchVarienceModulator>().PlaySound(currentState.sounds.hurt);
                break;
        }

        if (currentState.health + _value <= 0)
        {
            if (isDead) return;
            GetComponent<AudioSource_PitchVarienceModulator>().PlaySound(currentState.sounds.death);
            OnPawnDeath?.Invoke();
            isDead = true;
        }

        if (currentState.health + _value > currentState.health) currentState.health = defaultState.data.health;
        else if (currentState.health + _value < 0) currentState.health = 0;
        else currentState.health += _value;
    }

    public void Kill()
    {
        // Instantly sets the pawns health to zero, firing its onDeath event
    }
    
    public void GetPawnController()
    {
        // Returns the type of controller that is possessing this pawn
        // This can be used to do things like checking if a pawn is possessed by a player
    }

    public void SetPawnController()
    {
        // Sets the type of controller that is possessing this pawn
    }

    public void SetPawnDefaultState(PlayerState _playerState)
    {
        // Sets the type of character
        defaultState = _playerState;
        currentState = defaultState.data;
    }
}
