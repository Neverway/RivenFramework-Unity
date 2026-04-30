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
using TMPro;
using UnityEngine;

public class GI_WidgetManager : MonoBehaviour
{
    #region========================================( Variables )======================================================//
    /*-----[ Inspector Variables ]------------------------------------------------------------------------------------*/


    /*-----[ External Variables ]-------------------------------------------------------------------------------------*/


    /*-----[ Internal Variables ]-------------------------------------------------------------------------------------*/


    /*-----[ Reference Variables ]------------------------------------------------------------------------------------*/
    private const string CANVAS_GAMEOBJECT_TAG = "UserInterface";
    private GameObject _canvas;
    private GameObject Canvas
    {
        get
        {
            if (_canvas == null)
                _canvas = GameObject.FindWithTag(CANVAS_GAMEOBJECT_TAG);
            return _canvas;
        }
    }

    public List<GameObject> widgets;
    public GameObject effectText;
    public Action OnNewWidgetCreated;
    public GameObject lastCreatedWidget;


    #endregion


    #region=======================================( Functions )======================================================= //

    /*-----[ Mono Functions ]-----------------------------------------------------------------------------------------*/


    /*-----[ Internal Functions ]-------------------------------------------------------------------------------------*/

    /// <summary>
    /// Find the widget prefab, on the widget manager, with the specified name
    /// </summary>
    private GameObject GetWidgetPrefab(string _widgetName)
    {
        foreach (var widget in widgets)
            if (widget.name == _widgetName)
                return widget;

        throw new Exception($"No widget named \"{_widgetName}\" exists. " +
        $"(check if widget is added to {nameof(GI_WidgetManager)} on {name})");
    }
    /// <summary>
    /// Find the widget prefab, on the widget manager, using the widget's unique WB script
    /// </summary>
    private GameObject GetWidgetPrefab<T>() where T : WidgetBlueprint
    {
        foreach (var widget in widgets)
            if (widget.GetComponent<T>() != null)
                return widget;

        throw new Exception($"No widget of type \"{nameof(T)}\" exists. " +
        $"(check if widget is added to {nameof(GI_WidgetManager)} on {name})");
    }

    /*-----[ External Functions ]-------------------------------------------------------------------------------------*/

    /// <summary>
    /// Add the widget from the widget manager, with the specified name, to the user interface
    /// </summary>
    /// <param name="_allowDuplicates">If enabled, multiple of the same widget can be added to the UI</param>
    /// <returns>True if adding the widget was successful
    /// <p>False if the widget couldn't be found on the widget manager</p>
    /// <p>(The result will also be False if the widget was already present on the UI and allowDuplicates is False)</p> </returns>
    public bool AddWidget(string _widgetName, bool _allowDuplicates = false) =>
    AddWidget(GetWidgetPrefab(_widgetName), _allowDuplicates);

    /// <summary>
    /// Add the widget from the widget manager, with the specified WB script, to the user interface
    /// </summary>
    /// <param name="_allowDuplicates">If enabled, multiple of the same widget can be added to the UI</param>
    /// <returns>True if adding the widget was successful
    /// <p>False if the widget couldn't be found on the widget manager</p>
    /// <p>(The result will also be False if the widget was already present on the UI and allowDuplicates is False)</p> </returns>
    public bool AddWidget<T>(bool _allowDuplicates = false) where T : WidgetBlueprint =>
    AddWidget(GetWidgetPrefab<T>(), _allowDuplicates);
    /// <summary>
    /// Add the widget from the widget manager, with the same name as the specified object, to the user interface
    /// </summary>
    /// <param name="_allowDuplicates">If enabled, multiple of the same widget can be added to the UI</param>
    /// <returns>True if adding the widget was successful
    /// <p>False if the widget couldn't be found on the widget manager</p>
    /// <p>(The result will also be False if the widget was already present on the UI and allowDuplicates is False)</p> </returns>
    public bool AddWidget(GameObject _widgetObject, bool _allowDuplicates = false)
    {
        // Do not add widget if no canvas exists
        if (Canvas == null) return false;

        // Do not allow adding a new widget if it already exists (unless duplicates are allowed)
        if (_allowDuplicates is false)
            if (GetExistingWidget(_widgetObject.name) != null)
                return false;

        var newWidget = Instantiate(_widgetObject, Canvas.transform, false);
        newWidget.transform.localScale = Vector3.one;
        newWidget.name = _widgetObject.name;
        lastCreatedWidget = newWidget;
        OnNewWidgetCreated.Invoke();
        return true;
    }

    /// <summary>Gets the widget of the specified type (or creates a new one if one does not exist)
    /// <br/>Returns false if widget could not be created or retrieved (like if the Canvas was null)</summary>
    public bool TryAddOrGetWidget<T>(out T addedWidget) where T : MonoBehaviour
    {
        addedWidget = null; //Initialize with default value
        //Do not add widget if no canvas exists
        if (Canvas == null) return false;

        //Try finding an existing widget of that type
        foreach (Transform child in Canvas.transform)
        {
            addedWidget = child.GetComponent<T>();
            if (addedWidget != null) return true;
        }

        //Try to get the widget prefab
        T widgetPrefab = null;
        foreach (var widget in widgets)
            if (widget.TryGetComponent(out widgetPrefab))
                break;
        if (widgetPrefab == null)
        {
            Debug.LogError($"No Widget with component of type {typeof(T)} was found. Maybe you forgot to" +
            $"add it to {nameof(GI_WidgetManager)}?");
            return false;
        }

        GameObject widgetObj = Instantiate(widgetPrefab.gameObject, Canvas.transform, false);
        T newWidget = widgetObj.GetComponent<T>();
        newWidget.transform.localScale = Vector3.one;
        newWidget.name = widgetPrefab.name;
        addedWidget = newWidget;
        return true;
    }
    public bool RemoveWidget<T>() where T : MonoBehaviour
    {
        if (Canvas == null) return false;

        foreach (Transform child in Canvas.transform)
        {
            if (child.TryGetComponent(out T component))
            {
                Destroy(child.gameObject);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Add the widget from the UI, with the specified name, or remove it if it's already present
    /// </summary>
    /// <returns>Returns true if the widget was added and false if it was removed</returns>
    public bool ToggleWidget(string _widgetName)
    {
        // If the widget already exists, destroy it
        GameObject existingWidget = GetExistingWidget(_widgetName);
        if (existingWidget != null)
        {
            Destroy(existingWidget);
            return false;
        }
        // If it does not exist, create it
        AddWidget(_widgetName);
        return true;
    }
    /// <summary>
    /// Add the widget from the UI, with the specified WB script, or remove it if it's already present
    /// </summary>
    /// <returns>Returns true if the widget was added and false if it was removed</returns>
    public bool ToggleWidget<T>() where T : WidgetBlueprint =>
        ToggleWidget(GetWidgetPrefab<T>().name);

    /// <summary>
    /// Get the specified widget object if it's present on the user interface
    /// </summary>
    /// <returns>Returns the widget object from the UI</returns>
    public GameObject GetExistingWidget(string _widgetName)
    {
        if (Canvas == null) return null;

        foreach (Transform child in Canvas.transform)
            if (child.name == _widgetName) return child.gameObject;

            return null;
    }
    /// <summary>
    /// Get the specified widget object if it's present on the user interface
    /// </summary>
    /// <returns>Returns the widget object from the UI</returns>
    public T GetExistingWidget<T>() where T : WidgetBlueprint
    {
        if (Canvas == null) return null;

        foreach (Transform child in Canvas.transform)
        {
            T widget = child.GetComponent<T>();
            if (widget != null) return widget;
        }
        return null;
    }

    public bool TryGetExistingWidget(string _widgetName, out GameObject _result)
    {
        _result = GetExistingWidget(_widgetName);
        return _result != null;
    }


    /// <summary>Gets the widget of the specified type (or creates a new one if one does not exist)
    /// <br/>Returns false if widget could not be created or retrieved (like if the Canvas was null)</summary>
    public bool AddOrGetExistingWidget<T>(out T addedWidget) where T : MonoBehaviour
    {
        addedWidget = null; //Initialize with default value
        //Do not add widget if no canvas exists
        if (Canvas == null) return false;

        //Try finding an existing widget of that type
        foreach (Transform child in Canvas.transform)
        {
            addedWidget = child.GetComponent<T>();
            if (addedWidget != null) return true;
        }

        //Try to get the widget prefab
        T widgetPrefab = null;
        foreach (var widget in widgets)
            if (widget.TryGetComponent(out widgetPrefab))
                break;
        if (widgetPrefab == null)
        {
            Debug.LogError($"No Widget with component of type {typeof(T)} was found. Maybe you forgot to" +
            $"add it to {nameof(GI_WidgetManager)}?");
            return false;
        }

        GameObject widgetObj = Instantiate(widgetPrefab.gameObject, Canvas.transform, false);
        T newWidget = widgetObj.GetComponent<T>();
        newWidget.transform.localScale = Vector3.one;
        newWidget.name = widgetPrefab.name;
        addedWidget = newWidget;
        return true;
    }

    private IEnumerator CoSpawnEffectText(string _amount, Vector3 _position, int _mode, float _delay)
    {
        yield return new WaitForSeconds(_delay);
        var newText = Instantiate(effectText, _canvas.transform);
        var viewCam = FindObjectOfType<Camera>();
        newText.transform.position = viewCam.WorldToScreenPoint(_position);
        var textComponent = newText.transform.GetChild(0).GetComponent<TMP_Text>();
        Destroy(newText.gameObject, 1);

        switch (_mode)
        {
            case 0: //Damage
                textComponent.color = Color.red;
                textComponent.text =
                $"{_amount}";
                break;
            case 1: //Heal
                textComponent.color = Color.green;
                textComponent.text =
                $"{_amount}";
                break;
            case 2: // Defense damage
                textComponent.color = Color.white;
                textComponent.text =
                $"<sprite index=8> {_amount}";
                break;
            case 3: // ???
                textComponent.color = Color.white;
                textComponent.text =
                $"<sprite index=3> {_amount}";
                break;
            case 4: // Corruption
                textComponent.color = new Color(0.25f,0.05f,0.75f);
                textComponent.text =
                $"{_amount}";
                break;
            case 5: // ???
                textComponent.color = Color.yellow;
                textComponent.text =
                $"{_amount}";
                break;
            case 6: //Money
                textComponent.color = new Color(1f, .9f, .33f);
                textComponent.text =
                $"+{_amount}$";
                break;
        }
    }

    public void SpawnEffectText(string _amount, Vector3 _position, int _mode, float _delay=0)
    {
        if (string.IsNullOrWhiteSpace(_amount)) return;
        if (_amount == "0") return;

        StartCoroutine(CoSpawnEffectText(_amount, _position, _mode, _delay));
    }


    #endregion
}
