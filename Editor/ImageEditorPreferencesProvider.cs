using System.IO;
using UnityEditor;
using UnityEngine;

namespace ContextActionsSlop.Editor
{
    internal static class ImageEditorPreferencesProvider
    {
        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Preferences/Context Actions Slop", SettingsScope.User)
            {
                label = "Context Actions Slop",
                guiHandler = _ => DrawPreferences(),
                keywords = new[]
                {
                    "button", "size", "appearance", "image", "texture", "editor",
                    "Photoshop", "Krita", "Aseprite"
                }
            };
        }

        private static void DrawPreferences()
        {
            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            int buttonSize = EditorGUILayout.IntSlider(
                "Button size",
                ContextActionPreferences.ButtonSize,
                ContextActionPreferences.MinimumButtonSize,
                ContextActionPreferences.MaximumButtonSize);
            if (EditorGUI.EndChangeCheck())
            {
                ContextActionPreferences.ButtonSize = buttonSize;
                ProjectContextActionHost.RepaintProjectWindow();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Image Editors", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The first valid editor is used by left-click. Right-clicking the texture action shows the full list.",
                MessageType.Info);

            var editors = ImageEditorPreferences.Editors;
            bool changed = false;

            for (int index = 0; index < editors.Count; index++)
            {
                ImageEditorEntry editor = editors[index];
                if (editor == null)
                {
                    editors[index] = editor = new ImageEditorEntry("Image Editor", string.Empty);
                    changed = true;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                editor.name = EditorGUILayout.TextField("Name", editor.name);

                GUI.enabled = index > 0;
                if (GUILayout.Button("▲", GUILayout.Width(26)))
                {
                    (editors[index - 1], editors[index]) = (editors[index], editors[index - 1]);
                    changed = true;
                }

                GUI.enabled = index < editors.Count - 1;
                if (GUILayout.Button("▼", GUILayout.Width(26)))
                {
                    (editors[index + 1], editors[index]) = (editors[index], editors[index + 1]);
                    changed = true;
                }

                GUI.enabled = true;
                if (GUILayout.Button("Remove", GUILayout.Width(62)))
                {
                    editors.RemoveAt(index);
                    changed = true;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                editor.executablePath = EditorGUILayout.TextField("Executable", editor.executablePath);
                if (GUILayout.Button("Browse…", GUILayout.Width(72)))
                {
                    string directory = File.Exists(editor.executablePath)
                        ? Path.GetDirectoryName(editor.executablePath)
                        : string.Empty;
                    string selected = EditorUtility.OpenFilePanel("Select Image Editor", directory, "exe");
                    if (!string.IsNullOrEmpty(selected))
                    {
                        editor.executablePath = selected;
                        if (string.IsNullOrWhiteSpace(editor.name))
                            editor.name = Path.GetFileNameWithoutExtension(selected);
                        changed = true;
                    }
                }

                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrEmpty(editor.executablePath) && !File.Exists(editor.executablePath))
                    EditorGUILayout.HelpBox("Executable not found.", MessageType.Warning);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Editor"))
            {
                editors.Add(new ImageEditorEntry("Image Editor", string.Empty));
                changed = true;
            }

            if (GUILayout.Button("Discover Installed Editors"))
                ImageEditorPreferences.DiscoverInstalledEditors();
            EditorGUILayout.EndHorizontal();

            if (changed || GUI.changed) ImageEditorPreferences.Save();
        }
    }
}
