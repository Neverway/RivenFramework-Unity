using ErryLib.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


[CustomPropertyDrawer(typeof(PolymorphicAttribute))]
public class PolymorphicDrawer : PropertyDrawer
{
    public static Dictionary<Type, List<Type>> cachedDerivedTypes = new Dictionary<Type, List<Type>>();
    public static GUIStyle popupStyle;
    public static GUIStyle popupStyleIfNull;
    private bool? _drawContentCached;
    private PolymorphicAttribute polymorphicAttribute;
    private MethodInfo listFilterMethod;
    private object listFilterTargetObject;
    public bool drawContent { 
        get 
        { 
            if (_drawContentCached == null)
            {
                _drawContentCached = true;
                // In the rare case where there is another attribute that rewrites the drawing of a property but you want to
                // allow the use of the polymorphic propertydrawer, skip drawing this content to allow them to use
                // ULElements.Property to draw this dropdown without also redrawing all of the properties fields
                foreach (AttributeInfo attributes in fieldInfo.GetCachedAttributeUsages())
                {
                    if (attributes.Attribute.GetType()
                        .HasAttribute<AttributeNeedsPolymorphicDrawerToIgnoreContentsButDrawDropdownAttribute>()) // <= Errynei, this name is beautiful
                    {
                        _drawContentCached = false; break;
                    }
                }
            }
            return _drawContentCached.Value;
        } 
    }


    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (attribute is PolymorphicAttribute polyAt)
        {
            polymorphicAttribute = polyAt;
            string tag = polymorphicAttribute.filterMethodReferenceTag;

            if (tag != null 
                && ReferenceTagAttribute.TryGet(tag, out MethodInfo filterMethod))
            {
                listFilterMethod = filterMethod;
                if (filterMethod.IsStatic)
                    listFilterTargetObject = null;
                else
                    listFilterTargetObject = GetParent(property);
            }
        }

        //todo: This is bad hotfix to make sure array elements are propertly indented, probably doesnt fully work
        bool doIndent = property.propertyPath.Contains("Array.data[");
        if (doIndent) EditorGUI.indentLevel++; //Also reduce indent at end of property

        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            if (drawContent)
            {
                EditorGUI.PropertyField(position, property, label);
            }
            EditorGUI.EndProperty();
            return;
        }
        bool hasValue = property.managedReferenceValue != null;

        // Get the currently assigned type (if any)
        Type currentType = GetManagedReferenceType(property);
        if (currentType == null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            if (GUILayout.Button("Missing Type : Clear?"))
            {
                Debug.Log(GetParent(property));
                fieldInfo.SetValue(GetParent(property), null);
                //property.serializedObject.ApplyModifiedProperties();
                //GetParent(property);
            }
            GUILayout.EndHorizontal();
            EditorGUI.EndProperty();
            return;
        }

        List<Type> derivedTypes;
        cachedDerivedTypes.TryGetValue(currentType, out derivedTypes);

        if (derivedTypes == null)
        {
            // Get all valid types for the field
            derivedTypes = GetAllDerivedTypes(currentType, fieldInfo.FieldType);
            cachedDerivedTypes.Add(currentType, derivedTypes);
        }    

        //Initialize the popupStyle if null
        if (popupStyle == null || popupStyleIfNull == null)
        {
            popupStyle = new GUIStyle(EditorStyles.popup);
            popupStyle.normal.textColor = new Color(.27f, 0.78f, 0.63f);
            popupStyle.active.textColor = new Color(.37f, 0.88f, 0.73f);
            popupStyle.hover.textColor = new Color(.47f, 0.98f, 0.83f);
            popupStyle.focused.textColor = new Color(.27f, 0.78f, 0.63f);
            popupStyle.fontSize += 2;

            popupStyleIfNull = new GUIStyle(EditorStyles.popup);
            popupStyleIfNull.normal.textColor = new Color(.85f, .9f, 0f);
            popupStyleIfNull.active.textColor = new Color(.85f, .9f, 0f);
            popupStyleIfNull.hover.textColor = new Color(1f, 1f, 0f);
            popupStyleIfNull.focused.textColor = new Color(.85f, .9f, 0f);
            popupStyleIfNull.fontSize += 2;
        }

        //Filter Derived Types by defined filter method, if a filter method exists
        derivedTypes = derivedTypes.Where(t => FilterTypes(t)).ToList();
        

        // Create dropdown options from the derived types
        List<string> typeOptions = new List<string> { "--- No Type Selected ---" };
        typeOptions.AddRange(derivedTypes.Select(t => t.Name));

        // Get the current type's index in the dropdown
        int selectedIndex = hasValue ? typeOptions.IndexOf(currentType.Name) : 0;
        
        Color[] derivedTypeCustomColors =  GetTypeCustomColors(derivedTypes);
        var typeColor = derivedTypeCustomColors[hasValue ? selectedIndex-1 : 0];
        bool hasColor = (typeColor.a != 0);
        var customStyle = new GUIStyle(EditorStyles.popup);
        if (hasColor)
        {
            customStyle.normal.textColor = typeColor;
            customStyle.active.textColor = typeColor;
            customStyle.hover.textColor = typeColor;
            customStyle.focused.textColor = typeColor;
            customStyle.fontSize += 2;
        }

        // Draw the dropdown
        position.height = EditorGUIUtility.singleLineHeight;
        int newSelectedIndex = 0;
        if (drawContent)
        {
            newSelectedIndex = EditorGUI.Popup(position, " ", selectedIndex, typeOptions.ToArray(),
                (hasValue ? (hasColor ? customStyle : popupStyle) : popupStyleIfNull));
        }
        else
        {
            newSelectedIndex = EditorGUI.Popup(position, selectedIndex, typeOptions.ToArray(),
                (hasValue ? (hasColor ? customStyle : popupStyle) : popupStyleIfNull));
        }

        // If the selected index changed, update the property
        if (newSelectedIndex != selectedIndex)
        {
            if (newSelectedIndex == 0)
            {
                property.managedReferenceValue = null;
            }
            else
            {
                Type newType = derivedTypes[newSelectedIndex - 1]; // Adjust for "(None)"
                property.managedReferenceValue = Activator.CreateInstance(newType);
            }
        }

        // Save original indent level and expand child properties
        int originalIndent = EditorGUI.indentLevel;

        

        if (drawContent)
        {
            position.height = EditorGUI.GetPropertyHeight(property, true);
            EditorGUI.PropertyField(position, property, true);
        }

        if (doIndent) EditorGUI.indentLevel--; //Undo added increment from beginning of property
        EditorGUI.EndProperty();
    }

    private List<Type> GetAllDerivedTypes(Type currentType, Type baseType)
    {
        // Get all derived types of the base type or interface
        if (baseType == null)
            return new List<Type>();

        baseType = GetNonArrayType(baseType);

        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();
    }

    private Color[] GetTypeCustomColors(IEnumerable<Type> derivedTypes)
    {
        var colors = new Color[derivedTypes.Count()];
        var colorAttributes = ReflectionCache.GetAttributeUsageInfos<PolymorphicClassColor>();
        var types = derivedTypes.ToArray();
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color(0,0,0,0);
            
            foreach (var colorAttribute in colorAttributes)
            {
                if (colorAttribute.Member.Equals(types[i].GetTypeInfo()))
                {
                    colors[i] = colorAttribute.As<PolymorphicClassColor>().color;
                }
            }
        }
        
        
        return colors;
    }
    
    private bool FilterTypes(Type t)
    {
        if (t.Has_Attribute<HideInPolymorphicListAttribute>(false))
            return false;

        if (listFilterMethod == null)
            return true;
        if (listFilterTargetObject == null)
            return true;

        bool success;
        try
        {
            success = (bool)listFilterMethod.Invoke(listFilterTargetObject, new[] { t });
        }
        catch
        {
            //Debug.Log("Filter Object : " + listFilterTargetObject.ToString());
            //Debug.LogError("Filter Method assigned to Polymorphic Attribute " +
            //    "must accept a Type as its only parameter and return a bool. Check method tagged: " +
            //    $"\"{polymorphicAttribute.filterMethodReferenceTag}\"");
            success = true;
        }

        return success;
    }

    private Type GetManagedReferenceType(SerializedProperty property)
    {
        // If the property contains a valid reference, retrieve its type
        if (!string.IsNullOrEmpty(property.managedReferenceFullTypename))
        {
            string[] typeInfo = property.managedReferenceFullTypename.Split(' ');
            if (typeInfo.Length == 2)
            {
                string assemblyName = typeInfo[0];
                string className = typeInfo[1];
                return Type.GetType($"{className}, {assemblyName}");
            }
        }
        return GetNonArrayType(fieldInfo.FieldType);
    }
    private Type GetNonArrayType(Type fieldType)
    {
        // Check if the type is an array
        if (fieldType.IsArray)
        {
            // Get the type of the elements in the array
            return fieldType.GetElementType();
        }

        // Check if the type is a generic collection (like List<T>)
        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
        {
            // Get the type of the elements in the List<T>
            return fieldType.GetGenericArguments()[0];
        }

        // If it's not an array or a generic type, return the type itself
        return fieldType;
    }

    /// <remarks>Credit to whydoidoit on at 
    /// https://discussions.unity.com/t/get-the-instance-the-serializedproperty-belongs-to-in-a-custompropertydrawer/66954</remarks>
    public static object GetParent(SerializedProperty prop)
    {
        var path = prop.propertyPath.Replace(".Array.data[", "[");
        object obj = prop.serializedObject.targetObject;
        var elements = path.Split('.');
        foreach (var element in elements.Take(elements.Length - 1))
        {
            if (element.Contains("["))
            {
                var elementName = element.Substring(0, element.IndexOf("["));
                var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                obj = GetValue(obj, elementName, index);
            }
            else
            {
                obj = GetValue(obj, element);
            }
        }
        return obj;
    }

    public static object GetValue(object source, string name)
    {
        if (source == null)
            return null;
        var type = source.GetType();
        var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (f == null)
        {
            var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p == null)
                return null;
            return p.GetValue(source, null);
        }
        return f.GetValue(source);
    }

    public static object GetValue(object source, string name, int index)
    {
        var enumerable = GetValue(source, name) as IEnumerable;
        var enm = enumerable.GetEnumerator();
        while (index-- >= 0)
            enm.MoveNext();
        return enm.Current;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!drawContent)
            return EditorGUIUtility.singleLineHeight;
        return EditorGUI.GetPropertyHeight(property, true);
    }

    [InitializeOnLoadMethod]
    public static void OnEditorLoad()
    {
        EditorApplication.delayCall += InternalEditorUtility.RepaintAllViews;
    }
}