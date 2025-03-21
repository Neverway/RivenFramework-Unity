//===================== (Neverway 2024) Written by Liz M. =====================
//
// Purpose:
// Notes: 
//
//=============================================================================

using UnityEngine;

namespace Neverway.Framework.PawnManagement
{
    public class LB_Title : MonoBehaviour
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
        private GameInstance gameInstance;


        //=-----------------=
        // Mono Functions
        //=-----------------=
        private void Start()
        {
            gameInstance = FindObjectOfType<GameInstance>();
            gameInstance.UI_ShowTitle();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        //=-----------------=
        // Internal Functions
        //=-----------------=


        //=-----------------=
        // External Functions
        //=-----------------=
    }
}