using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SloppyContextActions.Editor
{
    [InitializeOnLoad]
    internal static class CreateShaderContextAction
    {
        private const string RegistrationId = "sloppy-context-actions.create-shader";
        private const string CreateMenuPrefix = "Assets/Create/";
        private const string ShaderPrefix = "Assets/Create/Shader/";
        private const string ShaderGraphPrefix = "Assets/Create/Shader Graph/";
        private const string ShaderGraphFromTemplate =
            "Assets/Create/Shader Graph/From Template...";
        private const string EmptyFullscreenGraph =
            "Assets/Create/Shader Graph/URP/Fullscreen Shader Graph";
        private static string FullscreenGraphTemplatePath =>
            SloppyContextActionsLocation.GetAssetPath(
                "Editor/ShaderTemplates/URP Fullscreen Blit.shadergraph.txt");

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

            if (templates.Remove(ShaderGraphFromTemplate))
            {
                menu.AddItem(
                    new GUIContent("Shader Graph/From Template..."),
                    false,
                    () => ExecuteTemplate(item, ShaderGraphFromTemplate));
                menu.AddSeparator("Shader Graph/");
            }

            foreach (string template in templates)
            {
                string capturedTemplate = template;
                string label = template.Substring(CreateMenuPrefix.Length);
                menu.AddItem(
                    new GUIContent(label),
                    false,
                    () =>
                    {
                        if (capturedTemplate == EmptyFullscreenGraph)
                            CreateFullscreenGraph(item);
                        else
                            ExecuteTemplate(item, capturedTemplate);
                    });
            }

            UrpScriptTemplates.AppendMenu(menu, item, "C#/", addSeparator: true);

            menu.ShowAsContext();
        }

        private static void CreateFullscreenGraph(ProjectContextItem item)
        {
            Selection.activeObject = item.Asset;
            CreateShaderGraphAction action =
                ScriptableObject.CreateInstance<CreateShaderGraphAction>();
            Texture2D icon = EditorGUIUtility.IconContent("Shader Icon").image as Texture2D;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                EntityId.None,
                action,
                "New Fullscreen Blit.shadergraph",
                icon,
                FullscreenGraphTemplatePath);
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
                    $"Sloppy Context Actions could not execute Unity menu item '{menuPath}'.");
            }
        }

        private sealed class CreateShaderGraphAction : AssetCreationEndAction
        {
            public override void Action(
                EntityId entityId,
                string pathName,
                string resourceFile)
            {
                string destinationPath = AssetDatabase.GenerateUniqueAssetPath(pathName);
                File.Copy(resourceFile, destinationPath);
                AssetDatabase.ImportAsset(
                    destinationPath,
                    ImportAssetOptions.ForceSynchronousImport);

                Object createdAsset = AssetDatabase.LoadMainAssetAtPath(destinationPath);
                ProjectWindowUtil.ShowCreatedAsset(createdAsset);
            }
        }
    }
}
