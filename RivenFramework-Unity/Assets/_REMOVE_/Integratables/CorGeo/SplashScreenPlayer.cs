//===================== (Neverway 2024) Written by Liz M. =====================
//
// Purpose:
// Notes:
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SplashScreenPlayer : MonoBehaviour
{
    //=-----------------=
    // Public Variables
    //=-----------------=


    //=-----------------=
    // Private Variables
    //=-----------------=


    //=-----------------=
    // Reference Variables
    //=-----------------=
    public Animator Animator;
    public AudioSource AudioSource;
    public UnityEvent OnPlayAudio;


    //=-----------------=
    // Mono Functions
    //=-----------------=
    private void Start()
    {
        StartCoroutine(PlaySplash());
    }

    private IEnumerator PlaySplash()
    {
        yield return new WaitForSeconds(0.25f);
        Animator.Play("test");
        if (AudioSource) AudioSource.Play();
        OnPlayAudio.Invoke();
        StartCoroutine(WaitForSplash());
    }

    private IEnumerator WaitForSplash()
    {
        yield return new WaitForSeconds(9.5f);
        SceneManager.LoadScene("_Title");
    }

    //=-----------------=
    // Internal Functions
    //=-----------------=


    //=-----------------=
    // External Functions
    //=-----------------=
}
