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
        var cardSelectionSourceProp = property.FindPropertyRelative("cardSelectionSource");
        var cardFilterTagsProp = property.FindPropertyRelative("cardFilterTags");
        var cardSelectionEffectProp = property.FindPropertyRelative("cardSelectionEffect");
        var indexProp = property.FindPropertyRelative("index");
        var animationTypeProp = property.FindPropertyRelative("animationType");
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
        if ((EffectType)typeProp.enumValueIndex==EffectType.Damage)
        {
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                animationTypeProp);
            y += lineHeight + spacing;
        }
        if ((EffectType)typeProp.enumValueIndex==EffectType.Multihit)
        {
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                durationProp);
            y += lineHeight + spacing;
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                animationTypeProp);
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
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                indexProp);
            y += lineHeight + spacing;
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.SetStatusToMaxValue||(EffectType)typeProp.enumValueIndex == EffectType.DispelBuffsIntoStatus||(EffectType)typeProp.enumValueIndex == EffectType.DispelSpecificStatus)
        {
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                statusTypeProp);
            y += lineHeight + spacing;
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.AddCardToHand|| (EffectType)typeProp.enumValueIndex == EffectType.AddCardToDrawPile|| (EffectType)typeProp.enumValueIndex == EffectType.AddCardToDiscardPile|| (EffectType)typeProp.enumValueIndex == EffectType.ForceNextCard)
        {
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                cardIDProp);
            y += lineHeight + spacing;
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.AddRandomCard)
        {
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                cardSelectionSourceProp,
                new GUIContent("Destination"));
            y += lineHeight + spacing;

            float tagsHeight = EditorGUI.GetPropertyHeight(cardFilterTagsProp, true);
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, tagsHeight),
                cardFilterTagsProp,
                new GUIContent("Card Filters"),
                true);
            y += tagsHeight + spacing;
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.StealBuff || (EffectType)typeProp.enumValueIndex == EffectType.TransferDebuff || (EffectType)typeProp.enumValueIndex == EffectType.DispelBuff || (EffectType)typeProp.enumValueIndex == EffectType.DispelDebuff)
        {
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                trueEffectProp);
            y += lineHeight + spacing;
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                durationProp);
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
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                cardSelectionSourceProp);
            y += lineHeight + spacing;

            float tagsHeight = EditorGUI.GetPropertyHeight(cardFilterTagsProp, true);
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, tagsHeight),
                cardFilterTagsProp,
                true);
            y += tagsHeight + spacing;

            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                cardSelectionEffectProp);
            y += lineHeight + spacing;
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                durationProp);
            y += lineHeight + spacing;
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.AddCopyOfCard)
        {
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                cardSelectionSourceProp);
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
        float height = 6 * (lineHeight + spacing); // type + value + targetSelf + description + conditional
        if ((EffectType)typeProp.enumValueIndex == EffectType.Damage)
        {
            height += lineHeight + spacing; // animationType for Damage effect
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.Status)
        {
            height += 6 * (lineHeight + spacing); // header + statusType + duration + cardID + index
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.SetStatusToMaxValue|| (EffectType)typeProp.enumValueIndex == EffectType.DispelBuffsIntoStatus||(EffectType)typeProp.enumValueIndex == EffectType.DispelSpecificStatus)
        {
            height += lineHeight + spacing; // statusType
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.Multihit)
        {
            height +=2*(lineHeight + spacing); // duration+animationType for multihit
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.AddCardToHand|| (EffectType)typeProp.enumValueIndex == EffectType.AddCardToDrawPile|| (EffectType)typeProp.enumValueIndex == EffectType.AddCardToDiscardPile|| (EffectType)typeProp.enumValueIndex == EffectType.ForceNextCard)
        {
            height += lineHeight + spacing; // cardID for AddCardToHand and other similar effects
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.AddRandomCard)
        {
            height += lineHeight + spacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("cardFilterTags"), true) + spacing;
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.StealBuff || (EffectType)typeProp.enumValueIndex == EffectType.TransferDebuff||(EffectType)typeProp.enumValueIndex == EffectType.DispelBuff || (EffectType)typeProp.enumValueIndex == EffectType.DispelDebuff)
        {
            height += 2 * (lineHeight + spacing); // trueEffect + duration
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.CardSelection)
        {
            height += lineHeight + spacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("cardFilterTags"), true) + spacing;
            height += lineHeight + spacing;
            height += lineHeight + spacing; // cardSelectionSource + cardSelectionEffect
        }
        if ((EffectType)typeProp.enumValueIndex == EffectType.AddCopyOfCard)
        {
            height += lineHeight + spacing; // source
        }
        if (conditionalProp.boolValue)
        {
            height += 2 * (lineHeight + spacing); // conditionType + conditionValue
        }
        return height;
    }
}