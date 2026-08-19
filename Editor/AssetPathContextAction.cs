using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SloppyContextActions.Editor
{
    [InitializeOnLoad]
    internal static class AssetPathContextAction
    {
        private const string RegistrationId = "sloppy-context-actions.asset-path";

        static AssetPathContextAction()
        {
            ProjectContextActionHost.Register(RegistrationId, Draw, order: 100);
            CurrentFolderActionHost.Register(
                RegistrationId,
                Draw,
                path => !string.IsNullOrEmpty(path) && !CreateFolderContextAction.CanCreateIn(path),
                order: 100);
        }

        private static void Draw(ProjectContextItem item)
        {
            if (string.IsNullOrEmpty(item.Path) ||
                (item.Surface == ProjectContextSurface.TreeFolder &&
                 CreateFolderContextAction.CanCreateIn(item.Path)))
                return;

            Rect buttonRect = item.ReserveButtonRect();
            GUIContent content = new()
            {
                image = ContextActionIcons.OpenInExplorer,
                tooltip = "Show in file browser\nRight-click to copy paths"
            };

            ProjectContextButtonClick click = ProjectContextButton.Draw(buttonRect, content);
            if (click == ProjectContextButtonClick.Left) Reveal(item.Path);
            else if (click == ProjectContextButtonClick.Right) ShowCopyMenu(item.Path);
        }

        public static void AppendMenu(GenericMenu menu, string assetPath)
        {
            menu.AddItem(new GUIContent("Open in File Browser"), false, () => Reveal(assetPath));
            menu.AddItem(new GUIContent("Copy Path/Asset Path"), false, () => Copy(assetPath));
            menu.AddItem(new GUIContent("Copy Path/Full Path"), false, () => Copy(GetFullPath(assetPath)));
            menu.AddItem(
                new GUIContent("Copy Path/Parent Folder Path"),
                false,
                () => Copy(GetParentPath(assetPath)));
        }

        private static void ShowCopyMenu(string assetPath)
        {
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Asset Path"), false, () => Copy(assetPath));
            menu.AddItem(new GUIContent("Full Path"), false, () => Copy(GetFullPath(assetPath)));
            menu.AddItem(
                new GUIContent("Parent Folder Path"),
                false,
                () => Copy(GetParentPath(assetPath)));
            menu.ShowAsContext();
        }

        private static void Reveal(string assetPath)
        {
            EditorUtility.RevealInFinder(GetFullPath(assetPath));
        }

        private static string GetFullPath(string assetPath)
        {
            string physicalPath = FileUtil.GetPhysicalPath(assetPath);
            if (!string.IsNullOrEmpty(physicalPath)) return Path.GetFullPath(physicalPath);
            return Path.GetFullPath(assetPath);
        }

        private static string GetParentPath(string assetPath)
        {
            string fullPath = GetFullPath(assetPath);
            return Directory.GetParent(fullPath)?.FullName ?? fullPath;
        }

        private static void Copy(string value)
        {
            EditorGUIUtility.systemCopyBuffer = value ?? string.Empty;
        }
    }
}
