//===================== (Neverway 2024) Written by Liz M. =====================
//
// Purpose: Show the title screen widget and unlock the mouse cursor
// Notes: 
//
//=============================================================================

using RivenFramework;
using UnityEngine;

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
    private GI_WidgetManager widgetManager;
    [SerializeField] private GameObject titleWidget;


    //=-----------------=
    // Mono Functions
    //=-----------------=
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        widgetManager = GameInstance.Get<GI_WidgetManager>();
        print(widgetManager);
        print(titleWidget);
        widgetManager.AddWidget(titleWidget);
    }

    private void Update()
    {
        if (widgetManager)
        {
            if (!widgetManager.GetExistingWidget(titleWidget.name))
            {
                widgetManager.AddWidget(titleWidget);
            }
        }
        else
        {
            widgetManager = GameInstance.Get<GI_WidgetManager>();
        }
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