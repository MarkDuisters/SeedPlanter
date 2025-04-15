#if UNITY_EDITOR
using UnityEngine;
#endif

public class ShowIfEnumAttribute : PropertyAttribute
{
    public string EnumFieldName;
    public object EnumValue;

    public ShowIfEnumAttribute(string enumFieldName, object enumValue)
    {
        EnumFieldName = enumFieldName;
        EnumValue = enumValue;
    }
}
