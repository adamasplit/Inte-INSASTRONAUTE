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
        var descriptionProp = property.FindPropertyRelative("description");
        var targetSelfProp = property.FindPropertyRelative("targetSelf");
        var targetOthersProp = property.FindPropertyRelative("targetOthers");
        var statusTypeProp = property.FindPropertyRelative("statusType");
        var durationProp = property.FindPropertyRelative("duration");
        var conditionalProp = property.FindPropertyRelative("conditional");
        var conditionTypeProp = property.FindPropertyRelative("conditionType");
        var conditionValueProp = property.FindPropertyRelative("conditionValue");
        var trueEffectProp = property.FindPropertyRelative("trueEffect");
        var cardIDProp = property.FindPropertyRelative("cardID");

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

        // DESCRIPTION
        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, lineHeight),
            descriptionProp);
        y += lineHeight + spacing;

        // TARGET SELF
        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, lineHeight),
            targetSelfProp);
        y += lineHeight + spacing;

        // TARGET OTHERS
        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, lineHeight),
            targetOthersProp);
        y += lineHeight + spacing;

        // CONDITIONAL 
        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, lineHeight),
            conditionalProp);
        y += lineHeight + spacing;

        if ((EffectType)typeProp.enumValueIndex==EffectType.Multihit)
        {
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                durationProp);
            y += lineHeight + spacing;
        }

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
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                cardIDProp);
            y += lineHeight + spacing;
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.AddCardToHand|| (EffectType)typeProp.enumValueIndex == EffectType.AddCardToDrawPile|| (EffectType)typeProp.enumValueIndex == EffectType.AddCardToDiscardPile)
        {
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                cardIDProp);
            y += lineHeight + spacing;
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.StealBuff || (EffectType)typeProp.enumValueIndex == EffectType.TransferDebuff || (EffectType)typeProp.enumValueIndex == EffectType.DispelBuff || (EffectType)typeProp.enumValueIndex == EffectType.DispelDebuff)
        {
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                trueEffectProp);
            y += lineHeight + spacing;
        }
        if (conditionalProp.boolValue)
        {
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                conditionTypeProp);
            y += lineHeight + spacing;

            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                conditionValueProp);
            y += lineHeight + spacing;
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.CardSelection)
        {
            var cardSelectionSourceProp = property.FindPropertyRelative("cardSelectionSource");
            var cardFilterTagsProp = property.FindPropertyRelative("cardFilterTags");
            var cardSelectionEffectProp = property.FindPropertyRelative("cardSelectionEffect");

            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                cardSelectionSourceProp);
            y += lineHeight + spacing;

            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                cardFilterTagsProp);
            y += lineHeight + spacing*30;

            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                cardSelectionEffectProp);
            y += lineHeight + spacing;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        var typeProp = property.FindPropertyRelative("type");
        var conditionalProp = property.FindPropertyRelative("conditional");
        int lines = 6; // type + value + targetSelf + description+ conditional

        if ((EffectType)typeProp.enumValueIndex == EffectType.Status)
        {
            lines += 4; // header + statusType + duration
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.Multihit)
        {
            lines += 1; // duration for multihit
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.AddCardToHand|| (EffectType)typeProp.enumValueIndex == EffectType.AddCardToDrawPile|| (EffectType)typeProp.enumValueIndex == EffectType.AddCardToDiscardPile)
        {
            lines += 1; // cardID for AddCardToHand
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.StealBuff || (EffectType)typeProp.enumValueIndex == EffectType.TransferDebuff||(EffectType)typeProp.enumValueIndex == EffectType.DispelBuff || (EffectType)typeProp.enumValueIndex == EffectType.DispelDebuff)
        {
            lines += 1; // trueEffect for StealBuff, TransferDebuff, DispelBuff and DispelDebuff
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.CardSelection)
        {
            lines += 6; // cardSelectionSource + cardFilterTags + cardSelectionEffect + (potentially more for cardFilterTags if we wanted to display them individually)
        }
        if (conditionalProp.boolValue)
        {
            lines += 2; // conditionType + conditionValue
        }
        return lines * (lineHeight + spacing);
    }
}