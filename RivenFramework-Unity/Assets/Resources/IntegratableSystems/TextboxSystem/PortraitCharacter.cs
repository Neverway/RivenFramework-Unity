using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Characters/PortraitProfile",  fileName = "CharPor_")]
public class Char_PortraitProfile : ScriptableObject
{
    public string characterName;
    public Char_ChatterVoice chatterVoice;
    public Char_PortraitEmotion[] portraitEmotes;
}

[Serializable]
public class Char_PortraitEmotion
{
    public string emotionName;
    public Sprite portrait;
}

[Serializable]
public class Char_PortraitEmotionReference
{
    public string emotionName;
}
