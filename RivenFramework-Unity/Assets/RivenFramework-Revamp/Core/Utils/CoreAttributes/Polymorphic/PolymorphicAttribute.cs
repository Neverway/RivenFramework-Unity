using System;
using UnityEngine;

public class PolymorphicAttribute : PropertyAttribute 
{
    //Static method name
    public string filterMethodReferenceTag;
    public PolymorphicAttribute() 
    {
        filterMethodReferenceTag = null;
    }
    public PolymorphicAttribute(string filterMethodReferenceTag) 
    {
        this.filterMethodReferenceTag = filterMethodReferenceTag;
    }
}



[AttributeUsage(AttributeTargets.Class)]
public class HideInPolymorphicListAttribute : Attribute { }

public class PolymorphicClassColor : Attribute
{
    public Color color;
    public PolymorphicClassColor(float r, float g, float b)
    {
        this.color = new Color(r,g,b);
    }
}

// In the rare case where there is another attribute that rewrites the drawing of a property, but you want to
// allow the use of the polymorphic propertydrawer, skip drawing the content after the polymorphic dropdown to
// allow them to use ULElements.Property to draw this dropdown without also redrawing all of the properties fields
public class AttributeNeedsPolymorphicDrawerToIgnoreContentsButDrawDropdownAttribute : Attribute { }