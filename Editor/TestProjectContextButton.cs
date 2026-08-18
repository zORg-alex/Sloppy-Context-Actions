using UnityEditor;
using UnityEngine;

namespace ContextActionsSlop.Editor
{
    [InitializeOnLoad]
    internal static class TestProjectContextButton
    {
        private const string RegistrationId = "context-actions-slop.test-button";

        static TestProjectContextButton()
        {
            ProjectContextActionHost.Register(RegistrationId, Draw, order: 0);
        }

        private static void Draw(ProjectContextItem item)
        {
            Rect buttonRect = item.ReserveButtonRect();
            GUIContent content = EditorGUIUtility.TrTextContent(
                "T",
                item.IsFolder ? "Test context action (folder)" : "Test context action (asset)");

            // miniButton supplies Unity-native normal, hover, pressed and focused states.
            if (GUI.Button(buttonRect, content, EditorStyles.miniButton))
            {
                Debug.Log($"Context Actions Slop test: {item.Path}", item.Asset);
            }
        }
    }
}
