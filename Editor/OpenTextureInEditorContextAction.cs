using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ContextActionsSlop.Editor
{
    [InitializeOnLoad]
    internal static class OpenTextureInEditorContextAction
    {
        private const string RegistrationId = "context-actions-slop.open-texture-in-editor";

        static OpenTextureInEditorContextAction()
        {
            ProjectContextActionHost.Register(RegistrationId, Draw, order: 100);
        }

        private static void Draw(ProjectContextItem item)
        {
            if (item.Asset is not Texture2D) return;
            if (!item.Path.StartsWith("Assets", StringComparison.Ordinal)) return;

            Rect buttonRect = item.ReserveButtonRect();
            Texture2D icon = ContextActionIcons.OpenInImageEditor;
            GUIContent content = new()
            {
                image = icon,
                tooltip = "Open texture in image editor\nRight-click to choose an editor"
            };

            ProjectContextButtonClick click = ProjectContextButton.Draw(buttonRect, content);
            if (click == ProjectContextButtonClick.Left) OpenInDefaultEditor(item.Path);
            else if (click == ProjectContextButtonClick.Right) ShowEditorMenu(item.Path);
        }

        private static void OpenInDefaultEditor(string assetPath)
        {
            ImageEditorEntry editor = ImageEditorPreferences.ValidEditors.FirstOrDefault();
            if (editor == null)
            {
                ImageEditorPreferences.OpenPreferences();
                return;
            }

            Open(editor, assetPath);
        }

        private static void ShowEditorMenu(string assetPath)
        {
            GenericMenu menu = new();
            bool hasEditor = false;

            foreach (ImageEditorEntry editor in ImageEditorPreferences.Editors)
            {
                if (editor == null || string.IsNullOrWhiteSpace(editor.name)) continue;

                ImageEditorEntry capturedEditor = editor;
                if (File.Exists(editor.executablePath))
                {
                    hasEditor = true;
                    menu.AddItem(
                        new GUIContent(editor.name),
                        false,
                        () => Open(capturedEditor, assetPath));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent($"{editor.name} (missing)"));
                }
            }

            if (!hasEditor) menu.AddDisabledItem(new GUIContent("No image editors configured"));
            menu.ShowAsContext();
        }

        private static void Open(ImageEditorEntry editor, string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            string absolutePath = Path.GetFullPath(Path.Combine(projectRoot ?? string.Empty, assetPath));

            if (!File.Exists(absolutePath))
            {
                Debug.LogError($"Texture file not found: {absolutePath}");
                return;
            }

            try
            {
                bool isAseprite = Path.GetFileName(editor.executablePath)
                    .Equals("aseprite.exe", StringComparison.OrdinalIgnoreCase);

                if (isAseprite && AsepriteRunningInstance.TryOpen(absolutePath)) return;

                Process.Start(new ProcessStartInfo
                {
                    FileName = editor.executablePath,
                    Arguments = $"\"{absolutePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not open '{assetPath}' in {editor.name}: {exception.Message}");
            }
        }
    }
}
