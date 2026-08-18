using System.IO;
using UnityEditor;
using UnityEngine;

namespace ContextActionsSlop.Editor
{
    [InitializeOnLoad]
    internal static class CreateFolderContextAction
    {
        private const string RegistrationId = "context-actions-slop.create-folder";

        private static readonly string[] PresetNames =
        {
            "Animations",
            "Audio",
            "Editor",
            "Materials",
            "Models",
            "Plugins",
            "Prefabs",
            "Resources",
            "Scenes",
            "Scripts",
            "Settings",
            "Shaders",
            "StreamingAssets",
            "Textures",
            "UI"
        };

        static CreateFolderContextAction()
        {
            CurrentFolderActionHost.Register(
                RegistrationId,
                Draw,
                path => !string.IsNullOrEmpty(path) && path.StartsWith("Assets"),
                order: -100);
        }

        private static void Draw(ProjectContextItem item)
        {
            if (!item.IsFolder || !item.Path.StartsWith("Assets")) return;

            Rect buttonRect = item.ReserveButtonRect();
            GUIContent content = new()
            {
                image = ContextActionIcons.AddFolder,
                tooltip = "Create subfolder\nRight-click for preset names"
            };

            ProjectContextButtonClick click = ProjectContextButton.Draw(buttonRect, content);
            if (click == ProjectContextButtonClick.Left) CreateRenameableFolder(item);
            else if (click == ProjectContextButtonClick.Right) ShowPresetMenu(item);
        }

        private static void CreateRenameableFolder(ProjectContextItem item)
        {
            Selection.activeObject = item.Asset;
            EditorApplication.ExecuteMenuItem("Assets/Create/Folder");
        }

        private static void ShowPresetMenu(ProjectContextItem item)
        {
            GenericMenu menu = new();

            foreach (string presetName in PresetNames)
            {
                string capturedName = presetName;
                menu.AddItem(
                    new GUIContent(capturedName),
                    false,
                    () => CreateNamedFolder(item.Path, capturedName));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(
                new GUIContent("New Folder"),
                false,
                () => CreateNamedFolder(item.Path, "New Folder"));
            menu.ShowAsContext();
        }

        private static void CreateNamedFolder(string parentPath, string requestedName)
        {
            string desiredPath = $"{parentPath}/{requestedName}";
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(desiredPath);
            string uniqueName = Path.GetFileName(uniquePath);
            string guid = AssetDatabase.CreateFolder(parentPath, uniqueName);

            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"Could not create folder '{uniquePath}'.");
                return;
            }

            Object folder = AssetDatabase.LoadAssetAtPath<Object>(uniquePath);
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
    }
}
