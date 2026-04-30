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
using RivenFramework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GI_TransitionManager : MonoBehaviour
{
    #region========================================( Variables )======================================================//
    /*-----[ Inspector Variables ]------------------------------------------------------------------------------------*/


    /*-----[ External Variables ]-------------------------------------------------------------------------------------*/
    public bool transitionInProgress;


    /*-----[ Internal Variables ]-------------------------------------------------------------------------------------*/


    /*-----[ Reference Variables ]------------------------------------------------------------------------------------*/
    public Image fadescreen;
    private GI_WidgetManager widgetManager;


    #endregion


    #region=======================================( Functions )=======================================================//
    /*-----[ Mono Functions ]-----------------------------------------------------------------------------------------*/
    private void Start()
    {
        ReferenceCheck();
    }

    /*-----[ Internal Functions ]-------------------------------------------------------------------------------------*/
    private void ReferenceCheck()
    {
        widgetManager = GameInstance.Get<GI_WidgetManager>();
        widgetManager.AddWidget("WB_Transition");

        if (!fadescreen)
        {
            fadescreen = widgetManager.GetExistingWidget("WB_Transition").GetComponentInChildren<Image>();
        }
    }
    
    private IEnumerator FadeCoroutine(float _duration, Color _startColor, Color _targetColor, float _delay = 0)
    {
        transitionInProgress = true;
        yield return new WaitForSeconds(_delay);
        SetDrawInFront();
        
        fadescreen.color = _startColor;
        var elapsedTime = 0f;
        
        while (elapsedTime < _duration)
        {
            elapsedTime += Time.deltaTime;
            fadescreen.color = Color.Lerp(_startColor, _targetColor, elapsedTime / _duration);
            yield return null;
        }
        transitionInProgress = false;
    }

    private IEnumerator FadecrossCoroutine(float _duration, float _holdDuration)
    {
        SetDrawInFront();
        StartCoroutine(FadeCoroutine(_duration/2, new Color(0,0,0,0), new Color(0,0,0,1)));
        yield return new WaitForSeconds(_holdDuration);
        StartCoroutine(FadeCoroutine(_duration/2, new Color(0,0,0,1), new Color(0,0,0,0), _duration / 2));
    }

    private void SetDrawInFront()
    {
        fadescreen.transform.parent.SetAsLastSibling();
    }


    /*-----[ External Functions ]-------------------------------------------------------------------------------------*/
    public void Fadeout(float _duration = 0.5f)
    {
        ReferenceCheck();
        StartCoroutine(FadeCoroutine(_duration, new Color(0,0,0,0), new Color(0,0,0,1)));
    }

    public void Fadein(float _duration = 0.5f)
    {
        ReferenceCheck();
        StartCoroutine(FadeCoroutine(_duration, new Color(0,0,0,1), new Color(0,0,0,0)));
    }

    public void CutOut()
    {
        fadescreen.color = new Color(0,0,0,1);
    }

    public void Fadecross(float _duration = 1, float _holdDuration = 0.5f)
    {
        ReferenceCheck();
        StartCoroutine(FadecrossCoroutine(_duration, _holdDuration));
    }


    #endregion
}
