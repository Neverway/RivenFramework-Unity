//===================== (Neverway 2024) Written by Liz M. =====================
//
// Purpose: If any key is pressed, skip forwards to title screen
// Notes:
//
//=============================================================================

using RivenFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LB_Splash : MonoBehaviour
{
    //=-----------------=
    // Public Variables
    //=-----------------=


    //=-----------------=
    // Private Variables
    //=-----------------=
    private bool acceptingInput = true;


    //=-----------------=
    // Reference Variables
    //=-----------------=


    //=-----------------=
    // Mono Functions
    //=-----------------=
    private void Start()
    {
    
    }

    private void Update()
    {
        if (!acceptingInput) return;
        if (Input.GetKey(KeyCode.F4) || Input.GetKey(KeyCode.JoystickButton0))
        {
            FindObjectOfType<ApplicationSettings>().EraseSettings();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
            acceptingInput = false;
            return;
        }
        if (Input.anyKeyDown)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    //=-----------------=
    // Internal Functions
    //=-----------------=


    //=-----------------=
    // External Functions
    //=-----------------=
}
