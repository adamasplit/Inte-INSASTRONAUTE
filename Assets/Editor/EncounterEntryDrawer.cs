using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EncounterEntry))]
public class EncounterEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var nameProp = property.FindPropertyRelative("displayName");
        var enemiesProp = property.FindPropertyRelative("enemies");

        string labelText = nameProp.stringValue;

        // fallback si pas de nom
        if (string.IsNullOrEmpty(labelText))
        {
            labelText = $"Encounter ({enemiesProp.arraySize} enemies)";
        }

        EditorGUI.PropertyField(position, property, new GUIContent(labelText), true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}