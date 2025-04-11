#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
#endif

[CustomPropertyDrawer(typeof(ShowIfEnumAttribute))]
public class ShowIfEnumDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ShowIfEnumAttribute showIf = (ShowIfEnumAttribute)attribute;
        SerializedProperty enumProp = property.serializedObject.FindProperty(showIf.EnumFieldName);

        if (enumProp != null && enumProp.propertyType == SerializedPropertyType.Enum)
        {
            if (enumProp.enumValueIndex == (int)showIf.EnumValue)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ShowIfEnumAttribute showIf = (ShowIfEnumAttribute)attribute;
        SerializedProperty enumProp = property.serializedObject.FindProperty(showIf.EnumFieldName);

        if (enumProp != null && enumProp.propertyType == SerializedPropertyType.Enum)
        {
            if (enumProp.enumValueIndex == (int)showIf.EnumValue)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
        }

        return 0f;
    }
}