using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DividerAttribute))]
public class DividerAttributeDrawer : DecoratorDrawer
{
    public override float GetHeight() => 100;

    public override void OnGUI(Rect position)
    {
        //position.height = 2;
        //position.y += 4;

        EditorGUI.DrawRect(position, new Color(1, 1, 1, 1f));
    }
}
