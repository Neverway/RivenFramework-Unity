//===================== (Neverway 2024) Written by Connorses =====================
//
// Purpose: Provides crush detection for pawns
// Notes: can be repurposed for other objects
//
//=============================================================================

using UnityEngine;
using UnityEngine.Events;
using Neverway.Framework.PawnManagement;
using Neverway.Framework;

public class CrushDetector : MonoBehaviour
{
    //=-----------------=
    // Public Variables
    //=-----------------=


    //=-----------------=
    // Private Variables
    //=-----------------=

    [SerializeField] private Vector3 rayDistance;
    [SerializeField] private float downDistance;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private float crushDamageAmount = 40f;
    private Pawn pawn;

    public UnityEvent onCrushed {  get; private set; } = new UnityEvent();

    //=-----------------=
    // Reference Variables
    //=-----------------=


    //=-----------------=
    // Mono Functions
    //=-----------------=

    private void Start ()
    {
        pawn = GetComponent<Pawn>();
    }

    private void FixedUpdate ()
    {
    }

    //=-----------------=
    // Internal Functions
    //=-----------------=

    private bool CheckForOverlaps ()
    {
        Collider[] colliders = Physics.OverlapCapsule (transform.position + transform.up * rayDistance.y, transform.position - transform.up * downDistance, rayDistance.x, layerMask);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject == gameObject)
            {
                continue;
            }
            if (collider.isTrigger)
            {
                continue;
            }
            if (collider.attachedRigidbody != null && !collider.attachedRigidbody.isKinematic) {
                continue;
            }
            return true;
        }
        return false;
    }

    //=-----------------=
    // External Functions
    //=-----------------=
}