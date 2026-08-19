using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ContextActionsSlop.Editor
{
    [InitializeOnLoad]
    internal static class CreateShaderContextAction
    {
        private const string RegistrationId = "context-actions-slop.create-shader";
        private const string CreateMenuPrefix = "Assets/Create/";
        private const string ShaderPrefix = "Assets/Create/Shader/";
        private const string ShaderGraphPrefix = "Assets/Create/Shader Graph/";

        static CreateShaderContextAction()
        {
            CurrentFolderActionHost.Register(
                RegistrationId,
                Draw,
                IsShaderFolder,
                order: -70);
            ProjectContextActionHost.RegisterTreeFolder(
                RegistrationId,
                Draw,
                order: -70);
        }

        private static bool IsShaderFolder(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.StartsWith("Assets", StringComparison.Ordinal) &&
                   path.IndexOf("Shader", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void Draw(ProjectContextItem item)
        {
            if (!item.IsFolder || !IsShaderFolder(item.Path)) return;

            Rect buttonRect = item.ReserveButtonRect();
            GUIContent content = new()
            {
                image = ContextActionIcons.Shader,
                tooltip = "Create Shader or Shader Graph"
            };

            if (ProjectContextButton.Draw(buttonRect, content) != ProjectContextButtonClick.None)
                ShowTemplates(item);
        }

        private static void ShowTemplates(ProjectContextItem item)
        {
            GenericMenu menu = new();
            List<string> templates = FindTemplates();
            if (templates.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No shader templates found"));
                menu.ShowAsContext();
                return;
            }

            foreach (string template in templates)
            {
                string capturedTemplate = template;
                string label = template.Substring(CreateMenuPrefix.Length);
                menu.AddItem(
                    new GUIContent(label),
                    false,
                    () => ExecuteTemplate(item, capturedTemplate));
            }

            menu.ShowAsContext();
        }

        private static List<string> FindTemplates()
        {
            using (ContextActionPerformance.Measure(
                       "Shader template menu discovery",
                       "The lookup asks Unity for all Assets menu paths and filters installed Shader and Shader Graph creation commands."))
            {
                List<string> templates = new();
                foreach (string menuPath in Unsupported.GetSubmenus("Assets"))
                {
                    if (!menuPath.StartsWith(ShaderPrefix, StringComparison.Ordinal) &&
                        !menuPath.StartsWith(ShaderGraphPrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    templates.Add(menuPath);
                }

                templates.Sort(StringComparer.OrdinalIgnoreCase);
                return templates;
            }
        }

        private static void ExecuteTemplate(ProjectContextItem item, string menuPath)
        {
            Selection.activeObject = item.Asset;
            if (!EditorApplication.ExecuteMenuItem(menuPath))
            {
                Debug.LogError(
                    $"Context Actions Slop could not execute Unity menu item '{menuPath}'.");
            }
        }
    }
}
