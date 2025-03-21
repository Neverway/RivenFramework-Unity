//===================== (Neverway 2024) Written by Liz M. =====================
//
// Purpose:
// Notes: 
//
//=============================================================================

using System.Collections;
using UnityEngine;
using Neverway.Framework.PawnManagement;

namespace Neverway
{
    public class LB_World : MonoBehaviour
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
            gameInstance.UI_ShowHUD();
        }

        private void Update()
        {
        }


        //=-----------------=
        // Internal Functions
        //=-----------------=
        

        //=-----------------=
        // External Functions
        //=-----------------=
    }
}