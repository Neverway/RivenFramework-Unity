using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UserEvent = UnityEngine.Event;

// BEWARE: ERRYNEI WITCHCRAFT BELOW!!!!
// Errynei will try to say that the forbidden tongues are 'bullshit',
// they are correct. (Good luck)

[CustomPropertyDrawer(typeof(Char_PortraitEmotionReference))]
public class Char_PortraitEmotionReferenceDrawer : PropertyDrawer
{
    private bool hasEmotions = false;
    public const float Padding = 4f;
    public const float PreviewSize = 80f;
    private string selectedEmotion = "";
    private Sprite[] spriteList;
    private Char_PortraitProfile portraitProfile;
    private int selectedSpriteIndex = 0;
    private string portraitProfileClassRef = nameof(TextFrames.portraitProfile); // <== This needs to be the same as GI_TextboxManager's portraitProfile reference

    [InitializeOnLoadMethod]
    static void Init()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
    }

    static void Update()
    {
        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            //mousePressPos = GUIUtility.GUIToScreenPoint(UserEvent.current?.mousePosition ?? Vector2.zero);
            //hasLeftClicked = true;
        }
    }

    public void GetSpriteList(SerializedProperty property)
    {
        hasEmotions = false;
        try
        {
            var parentObject = PolymorphicDrawer.GetParent(property);
            portraitProfile = YormExtensionMethods.GetFieldValue<Char_PortraitProfile>(parentObject, portraitProfileClassRef);

            spriteList = portraitProfile.portraitEmotes.Select(e => e.portrait).ToArray();
            for (int i = 0; i < portraitProfile.portraitEmotes.Length; i++)
            {
                if (portraitProfile.portraitEmotes[i].emotionName == selectedEmotion)
                    selectedSpriteIndex = i;
            }

            hasEmotions = true;
        }
        catch { }
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Wtf am I wrong with me?! ~Errynei 2026
        EditorGUI.BeginProperty(position, label, property);

        var emotionNameField = property.FindPropertyRelative(nameof(Char_PortraitEmotionReference.emotionName));
        selectedEmotion = emotionNameField.stringValue;
        // The fitnessgram pacer test is a multi-stage aerobic capacity test...
        GetSpriteList(property);

        // Draw label
        Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        if (!hasEmotions)
        {
            EditorGUI.LabelField(labelRect, label, new GUIContent("No emotions found"));
            
            EditorGUI.EndProperty();
            return;
        }
        EditorGUI.LabelField(labelRect, label, new GUIContent(selectedEmotion));

        Rect spriteRect = position;
        spriteRect.x += spriteRect.width - (PreviewSize + Padding);
        spriteRect.width = PreviewSize;
        spriteRect.height = PreviewSize;

        if (DrawSpriteButton(spriteList[selectedSpriteIndex], spriteRect))
        {
            SelectSpriteWindow.ShowWindow(spriteList, selectedSpriteIndex, (newIndex) => 
            {
                selectedSpriteIndex = newIndex;
                selectedEmotion = portraitProfile.portraitEmotes[selectedSpriteIndex].emotionName;
                emotionNameField.stringValue = selectedEmotion;

                property.serializedObject.ApplyModifiedProperties();
            });
        }

        EditorGUI.EndProperty();
    }
    
    public static bool DrawSpriteButton(Sprite sprite, Rect position)
    {
        if (sprite == null) return false;

        Texture2D preview = AssetPreview.GetAssetPreview(sprite);

        GUIContent content = preview != null
            ? new GUIContent(preview)
            : GUIContent.none;

        return GUI.Button(position, content);
        //else if (hasLeftClicked)
        //{
        //    Rect screenSpaceRect = new Rect(GUIUtility.GUIToScreenPoint(rect.position), rect.size);
        //
        //    // Check if the mouse is over the button
        //    if (screenSpaceRect.Contains(mousePressPos))
        //    {
        //        selectedSpriteIndex = i;
        //        positionHistory.Clear();
        //    }
        //    hasLeftClicked = false;
        //}


        // Highlight selected
    }
    public static void DrawMissingSprite(Rect position)
    {
        Handles.color = Color.grey;
        Handles.DrawSolidRectangleWithOutline(position, new Color(0.8f, 0.8f, 0.8f, 0.2f), Color.grey);
        GUIStyle nullLabelStyle = new GUIStyle("label");
        nullLabelStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(position, "null\nsprite", nullLabelStyle);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return PreviewSize + Padding;
    }
}






public class SelectSpriteWindow : EditorWindow
{
    private Sprite[] sprites;
    private int currentIndex;

    public Action<int> onSelectIndex;

    public static void ShowWindow(Sprite[] sprites, Action<int> onSelect) => ShowWindow(sprites, -1, onSelect);
    public static void ShowWindow(Sprite[] sprites, int currentIndex, Action<int> onSelect)
    {
        var window = GetWindow<SelectSpriteWindow>(true, "My Popup", true);
        window.onSelectIndex = onSelect;
        window.sprites = sprites;
        window.currentIndex = currentIndex;
        window.minSize = new Vector2(250, 100);
    }

    private void OnGUI()
    {
        GUILayout.Label("Select a Sprite", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.FlexibleSpace();

        float size = Char_PortraitEmotionReferenceDrawer.PreviewSize * 2;
        float padding = Char_PortraitEmotionReferenceDrawer.Padding;

        int count = sprites.Length;
        int columns = Mathf.Max(1, Mathf.FloorToInt(position.width / (size + padding)));

        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int col = i % columns;

            Rect rect = new Rect(
                col * (size + padding),
                EditorGUIUtility.singleLineHeight + row * (size + padding),
                size,
                size
            );

            if (sprites[i] == null)
            {
                Char_PortraitEmotionReferenceDrawer.DrawMissingSprite(rect);
            }
            else if (Char_PortraitEmotionReferenceDrawer.DrawSpriteButton(sprites[i], rect))
            {
                onSelectIndex?.Invoke(i);
                currentIndex = i;
            }

            if (currentIndex == i)
            {
                Handles.color = Color.green;
                Handles.DrawSolidRectangleWithOutline(rect, new Color(0, 1, 0, 0.2f), Color.green);
            }
        }
    }
}







public static class YormExtensionMethods
{
    public static object GetFieldValue(object obj, string fieldName)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (string.IsNullOrEmpty(fieldName)) throw new ArgumentException("Field name is null or empty");

        Type type = obj.GetType();

        FieldInfo field = type.GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (field == null)
            throw new MissingFieldException(type.FullName, fieldName);

        return field.GetValue(obj);
    }
    public static T GetFieldValue<T>(object obj, string fieldName)
    {
        object value = GetFieldValue(obj, fieldName);

        if (value is T typedValue)
            return typedValue;

        throw new InvalidCastException(
            $"Field '{fieldName}' is not of type {typeof(T)}"
        );
    }
}