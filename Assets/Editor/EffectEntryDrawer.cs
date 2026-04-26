using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EffectEntry))]
public class EffectEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float y = position.y;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        var typeProp = property.FindPropertyRelative("type");
        var valueProp = property.FindPropertyRelative("value");
        var targetSelfProp = property.FindPropertyRelative("targetSelf");
        var statusTypeProp = property.FindPropertyRelative("statusType");
        var durationProp = property.FindPropertyRelative("duration");

        // TYPE
        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, lineHeight),
            typeProp);
        y += lineHeight + spacing;

        // VALUE
        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, lineHeight),
            valueProp);
        y += lineHeight + spacing;

        // TARGET SELF
        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, lineHeight),
            targetSelfProp);
        y += lineHeight + spacing;

        // 👉 CONDITION
        if ((EffectType)typeProp.enumValueIndex == EffectType.Status)
        {
            EditorGUI.LabelField(
                new Rect(position.x, y, position.width, lineHeight),
                "Status Effect",
                EditorStyles.boldLabel);
            y += lineHeight + spacing;

            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                statusTypeProp);
            y += lineHeight + spacing;

            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                durationProp);
            y += lineHeight + spacing;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        var typeProp = property.FindPropertyRelative("type");

        int lines = 3; // type + value + targetSelf

        if ((EffectType)typeProp.enumValueIndex == EffectType.Status)
        {
            lines += 3; // header + statusType + duration
        }

        return lines * (lineHeight + spacing);
    }
}