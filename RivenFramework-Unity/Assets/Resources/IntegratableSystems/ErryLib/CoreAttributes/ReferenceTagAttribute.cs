using ErryLib.Reflection;
using System;
using System.Reflection;

[ReferenceTag("TheAttributeItself")]
public class ReferenceTagAttribute : Attribute 
{
    public string tagName;
    public ReferenceTagAttribute(string tagName) => this.tagName = tagName;

    public static bool TryGet<T>(string tagToGet, out T memberInfo)
        where T : MemberInfo
    {
        AttributeInfo[] referenceTags =
            ReflectionCache.GetAttributeUsageInfos<ReferenceTagAttribute>();

        foreach (AttributeInfo rt in referenceTags)
            if (rt.As<ReferenceTagAttribute>().tagName == tagToGet)
                if(rt.Member is T castedMemberInfo)
                {
                    memberInfo = castedMemberInfo;
                    return true;
                }

        memberInfo = null;
        return false;
    }
}