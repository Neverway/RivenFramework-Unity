//==========================================( Neverway 2025 )=========================================================//
// Author
//  Liz M.
//
// Contributors
//
//
//====================================================================================================================//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Volume_Warp : MonoBehaviour
{
    #region========================================( Variables )======================================================//
    /*-----[ Inspector Variables ]------------------------------------------------------------------------------------*/
    public Transform warpExit;
    public Vector3 exitOffset;


    /*-----[ External Variables ]-------------------------------------------------------------------------------------*/


    /*-----[ Internal Variables ]-------------------------------------------------------------------------------------*/


    /*-----[ Reference Variables ]------------------------------------------------------------------------------------*/


    #endregion


    #region=======================================( Functions )======================================================= //

    /*-----[ Mono Functions ]-----------------------------------------------------------------------------------------*/
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(Warp(other.transform.root.gameObject));
        }
    }

    private void OnDrawGizmos()
    {
        if (!warpExit) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(warpExit.position+exitOffset, transform.localScale);
        Gizmos.DrawLine(warpExit.position+exitOffset, transform.position);
    }


    /*-----[ Internal Functions ]-------------------------------------------------------------------------------------*/
    private IEnumerator Warp(GameObject _target)
    {
        if (!warpExit) yield return null;
        GameInstance.Get<GI_TransitionManager>().Fadecross(1f, 0.25f);
        yield return new WaitForSeconds(0.7f);
        _target.transform.position = warpExit.position + exitOffset;
    }


    /*-----[ External Functions ]-------------------------------------------------------------------------------------*/


    #endregion
}
