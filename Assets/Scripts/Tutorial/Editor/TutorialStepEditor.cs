using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for TutorialStep to provide a better editing experience
/// </summary>
#if UNITY_EDITOR
[CustomEditor(typeof(TutorialStep))]
public class TutorialStepEditor : Editor
{
    private SerializedProperty stepIdProp;
    private SerializedProperty titleProp;
    private SerializedProperty descriptionProp;
    private SerializedProperty highlightTypeProp;
    private SerializedProperty targetTagProp;
    private SerializedProperty targetTransformProp;
    private SerializedProperty overlayAlphaProp;
    private SerializedProperty highlightPaddingProp;
    private SerializedProperty pulseHighlightProp;
    private SerializedProperty advanceTypeProp;
    private SerializedProperty buttonTextProp;
    private SerializedProperty waitForTargetClickProp;
    private SerializedProperty autoAdvanceDelayProp;
    private SerializedProperty openTopMenuProp;
    private SerializedProperty navigateToScreenProp;
    private SerializedProperty delayBeforeShowProp;
    private SerializedProperty iconProp;
    
    private void OnEnable()
    {
        stepIdProp = serializedObject.FindProperty("stepId");
        titleProp = serializedObject.FindProperty("title");
        descriptionProp = serializedObject.FindProperty("description");
        highlightTypeProp = serializedObject.FindProperty("highlightType");
        targetTagProp = serializedObject.FindProperty("targetTag");
        targetTransformProp = serializedObject.FindProperty("targetTransform");
        overlayAlphaProp = serializedObject.FindProperty("overlayAlpha");
        highlightPaddingProp = serializedObject.FindProperty("highlightPadding");
        pulseHighlightProp = serializedObject.FindProperty("pulseHighlight");
        advanceTypeProp = serializedObject.FindProperty("advanceType");
        buttonTextProp = serializedObject.FindProperty("buttonText");
        waitForTargetClickProp = serializedObject.FindProperty("waitForTargetClick");
        autoAdvanceDelayProp = serializedObject.FindProperty("autoAdvanceDelay");
        openTopMenuProp = serializedObject.FindProperty("openTopMenu");
        navigateToScreenProp = serializedObject.FindProperty("navigateToScreen");
        delayBeforeShowProp = serializedObject.FindProperty("delayBeforeShow");
        iconProp = serializedObject.FindProperty("icon");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Step Information
        EditorGUILayout.LabelField("Step Information", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stepIdProp);
        EditorGUILayout.PropertyField(titleProp);
        EditorGUILayout.PropertyField(descriptionProp);
        EditorGUILayout.PropertyField(iconProp);
        
        EditorGUILayout.Space(10);
        
        // Target Configuration
        EditorGUILayout.LabelField("Target Configuration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(highlightTypeProp);
        
        HighlightType highlightType = (HighlightType)highlightTypeProp.enumValueIndex;
        if (highlightType != HighlightType.None)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(targetTransformProp, new GUIContent("Target (Direct Reference)"));
            
            if (targetTransformProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("If no direct reference, the system will search by Tag/Name below.", MessageType.Info);
                EditorGUILayout.PropertyField(targetTagProp, new GUIContent("Target Tag/Name"));
            }
            
            EditorGUILayout.PropertyField(overlayAlphaProp);
            EditorGUILayout.PropertyField(highlightPaddingProp);
            EditorGUILayout.PropertyField(pulseHighlightProp);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(10);
        
        // Interaction
        EditorGUILayout.LabelField("Interaction", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(advanceTypeProp);
        
        AdvanceType advanceType = (AdvanceType)advanceTypeProp.enumValueIndex;
        EditorGUI.indentLevel++;
        
        switch (advanceType)
        {
            case AdvanceType.Button:
                EditorGUILayout.PropertyField(buttonTextProp);
                break;
                
            case AdvanceType.TargetClick:
                EditorGUILayout.PropertyField(waitForTargetClickProp);
                EditorGUILayout.HelpBox("User must click the highlighted element to proceed.", MessageType.Info);
                break;
                
            case AdvanceType.Automatic:
                EditorGUILayout.PropertyField(autoAdvanceDelayProp, new GUIContent("Delay (seconds)"));
                if (autoAdvanceDelayProp.floatValue <= 0)
                {
                    EditorGUILayout.HelpBox("Delay must be > 0 for automatic advance.", MessageType.Warning);
                }
                break;
        }
        
        EditorGUI.indentLevel--;
        
        EditorGUILayout.Space(10);
        
        // Actions
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(delayBeforeShowProp);
        EditorGUILayout.PropertyField(openTopMenuProp);
        EditorGUILayout.PropertyField(navigateToScreenProp);
        
        if (navigateToScreenProp.intValue >= 0)
        {
            EditorGUILayout.HelpBox($"Will navigate to screen index {navigateToScreenProp.intValue}", MessageType.Info);
        }
        
        EditorGUILayout.Space(10);
        
        // Preview
        if (GUILayout.Button("Preview in Console", GUILayout.Height(30)))
        {
            PreviewStep();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void PreviewStep()
    {
        TutorialStep step = (TutorialStep)target;
        Debug.Log($"=== Tutorial Step Preview ===\n" +
                  $"ID: {step.stepId}\n" +
                  $"Title: {step.title}\n" +
                  $"Description: {step.description}\n" +
                  $"Highlight: {step.highlightType}\n" +
                  $"Target: {step.targetTag}\n" +
                  $"Advance: {step.advanceType}\n" +
                  $"Open Menu: {step.openTopMenu}\n" +
                  $"Navigate to Screen: {step.navigateToScreen}\n" +
                  $"=========================");
    }
}

/// <summary>
/// Menu items to quickly create tutorial assets
/// </summary>
public class TutorialMenuItems
{
    [MenuItem("Tools/Tutorial/Create First-Time Tutorial Template")]
    public static void CreateFirstTimeTutorial()
    {
        // Create folder if it doesn't exist
        string folderPath = "Assets/Resources/Tutorials";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Tutorials");
        }
        
        // Create 5 example steps
        CreateStep(folderPath, "Step1_Welcome", "Bienvenue !", 
            "Bienvenue dans INSASTRONAUTE ! Découvrez l'univers des cartes spatiales.", 
            HighlightType.None, "", AdvanceType.Button);
        
        CreateStep(folderPath, "Step2_Menu", "Menu Principal", 
            "Cliquez sur ce bouton pour ouvrir le menu principal.", 
            HighlightType.Circle, "MenuButton", AdvanceType.TargetClick, openMenu: false);
        
        CreateStep(folderPath, "Step3_Collection", "Votre Collection", 
            "Ici vous pouvez voir toutes vos cartes. Collectionnez-les toutes !", 
            HighlightType.Rectangle, "CollectionButton", AdvanceType.Button, openMenu: true, screenIndex: 0);
        
        CreateStep(folderPath, "Step4_Shop", "La Boutique", 
            "Achetez des packs de cartes pour agrandir votre collection.", 
            HighlightType.Rectangle, "ShopButton", AdvanceType.Button, openMenu: true, screenIndex: 1);
        
        CreateStep(folderPath, "Step5_Done", "C'est parti !", 
            "Vous êtes prêt ! Amusez-vous bien et bonne chance !", 
            HighlightType.None, "", AdvanceType.Button, buttonText: "Commencer");
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"✅ Created 5 tutorial step templates in {folderPath}");
        EditorUtility.DisplayDialog("Tutorial Template Created", 
            $"Created 5 tutorial step templates in:\n{folderPath}\n\nConfigure them and add to TutorialManager!", "OK");
    }
    
    private static void CreateStep(string folder, string fileName, string title, string description, 
        HighlightType highlightType, string targetTag, AdvanceType advanceType, 
        bool openMenu = false, int screenIndex = -1, string buttonText = "Suivant")
    {
        TutorialStep step = ScriptableObject.CreateInstance<TutorialStep>();
        step.stepId = fileName;
        step.title = title;
        step.description = description;
        step.highlightType = highlightType;
        step.targetTag = targetTag;
        step.advanceType = advanceType;
        step.openTopMenu = openMenu;
        step.navigateToScreen = screenIndex;
        step.buttonText = buttonText;
        step.pulseHighlight = highlightType != HighlightType.None;
        step.overlayAlpha = 0.7f;
        
        string path = $"{folder}/{fileName}.asset";
        AssetDatabase.CreateAsset(step, path);
    }
    
    [MenuItem("Tools/Tutorial/Reset All Tutorial Progress")]
    public static void ResetAllProgress()
    {
        if (EditorUtility.DisplayDialog("Reset Tutorial Progress", 
            "This will reset all tutorial completion flags in PlayerPrefs.\n\nThis is useful for testing.", "Reset", "Cancel"))
        {
            // Reset tutorial flags
            PlayerPrefs.DeleteKey("HasSeenAnyTutorial");
            
            // You might want to reset specific tutorials
            // Add your tutorial IDs here
            string[] tutorialIds = { "FirstTime", "CollectionFeature", "ShopFeature", "EventsFeature" };
            foreach (var id in tutorialIds)
            {
                PlayerPrefs.DeleteKey("Tutorial_Completed_" + id);
            }
            
            PlayerPrefs.Save();
            Debug.Log("✅ All tutorial progress reset!");
            EditorUtility.DisplayDialog("Success", "All tutorial progress has been reset!", "OK");
        }
    }
}
#endif
