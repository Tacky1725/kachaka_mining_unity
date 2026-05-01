using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameInputController))]
public class GameInputControllerEditor : Editor
{
    private const float UseJoyconWidth = 82f;
    private const float SideWidth = 72f;
    private const float ButtonWidth = 130f;
    private const float RemoveWidth = 24f;

    private SerializedProperty bindingsProperty;

    private void OnEnable()
    {
        bindingsProperty = serializedObject.FindProperty("bindings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Input Bindings", EditorStyles.boldLabel);
        DrawHeader();

        if (bindingsProperty != null)
        {
            for (int i = 0; i < bindingsProperty.arraySize; i++)
            {
                DrawBindingRow(i);
            }

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Add Binding"))
            {
                AddDefaultBindingRow();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Action", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("Key", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("Joy-Con", EditorStyles.miniBoldLabel, GUILayout.Width(UseJoyconWidth));
        EditorGUILayout.LabelField("Side", EditorStyles.miniBoldLabel, GUILayout.Width(SideWidth));
        EditorGUILayout.LabelField("Button", EditorStyles.miniBoldLabel, GUILayout.Width(ButtonWidth));
        GUILayout.Space(RemoveWidth);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBindingRow(int index)
    {
        SerializedProperty binding = bindingsProperty.GetArrayElementAtIndex(index);
        SerializedProperty action = binding.FindPropertyRelative("action");
        SerializedProperty key = binding.FindPropertyRelative("key");
        SerializedProperty useJoyconButton = binding.FindPropertyRelative("useJoyconButton");
        SerializedProperty joyconSide = binding.FindPropertyRelative("joyconSide");
        SerializedProperty joyconButton = binding.FindPropertyRelative("joyconButton");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(action, GUIContent.none);
        EditorGUILayout.PropertyField(key, GUIContent.none);
        EditorGUILayout.PropertyField(useJoyconButton, GUIContent.none, GUILayout.Width(UseJoyconWidth));

        EditorGUI.BeginDisabledGroup(!useJoyconButton.boolValue);
        EditorGUILayout.PropertyField(joyconSide, GUIContent.none, GUILayout.Width(SideWidth));
        EditorGUILayout.PropertyField(joyconButton, GUIContent.none, GUILayout.Width(ButtonWidth));
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("-", GUILayout.Width(RemoveWidth)))
        {
            bindingsProperty.DeleteArrayElementAtIndex(index);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void AddDefaultBindingRow()
    {
        int newIndex = bindingsProperty.arraySize;
        bindingsProperty.InsertArrayElementAtIndex(newIndex);

        SerializedProperty binding = bindingsProperty.GetArrayElementAtIndex(newIndex);
        binding.FindPropertyRelative("action").enumValueIndex = 0;
        binding.FindPropertyRelative("key").intValue = (int)KeyCode.None;
        binding.FindPropertyRelative("useJoyconButton").boolValue = true;
        binding.FindPropertyRelative("joyconSide").enumValueIndex = 0;
        binding.FindPropertyRelative("joyconButton").intValue = (int)Joycon.Button.PLUS;
    }
}
