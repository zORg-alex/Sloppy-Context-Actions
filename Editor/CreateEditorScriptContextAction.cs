using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SloppyContextActions.Editor
{
    [InitializeOnLoad]
    internal static class CreateEditorScriptContextAction
    {
        private const string RegistrationId = "sloppy-context-actions.create-editor-script";
        private const string PlacementPreferenceKey =
            "SloppyContextActions.EditorScriptPlacement";
        private const string LegacyPlacementPreferenceKey =
            "ContextActionsSlop.EditorScriptPlacement";

        private enum Placement
        {
            EditorSubfolder,
            SameFolder,
            Inline
        }

        private enum EditorScriptKind
        {
            CustomInspector,
            PropertyDrawer,
            PropertyAttributeDrawer
        }

        static CreateEditorScriptContextAction()
        {
            ProjectContextActionHost.Register(RegistrationId, Draw, order: -80);
        }

        private static Placement CurrentPlacement
        {
            get
            {
                if (!EditorPrefs.HasKey(PlacementPreferenceKey) &&
                    EditorPrefs.HasKey(LegacyPlacementPreferenceKey))
                {
                    EditorPrefs.SetInt(
                        PlacementPreferenceKey,
                        EditorPrefs.GetInt(LegacyPlacementPreferenceKey, (int)Placement.EditorSubfolder));
                }

                Placement placement = (Placement)EditorPrefs.GetInt(
                    PlacementPreferenceKey,
                    (int)Placement.EditorSubfolder);
                if (placement == Placement.Inline &&
                    !ContextActionPreferences.InlineEditorsEnabled)
                {
                    placement = Placement.EditorSubfolder;
                    EditorPrefs.SetInt(PlacementPreferenceKey, (int)placement);
                }

                return placement;
            }
            set => EditorPrefs.SetInt(PlacementPreferenceKey, (int)value);
        }

        private static void Draw(ProjectContextItem item)
        {
            if (item.IsFolder || item.Asset is not MonoScript script) return;

            Type targetType = script.GetClass();
            if (targetType == null) return;

            List<EditorScriptKind> actions = GetAvailableActions(targetType);
            if (actions.Count == 0) return;

            Rect buttonRect = item.ReserveButtonRect();
            GUIContent content = new()
            {
                image = ContextActionIcons.AddScript,
                tooltip = "Create editor script\nRight-click for placement and script type"
            };

            ProjectContextButtonClick click = ProjectContextButton.Draw(buttonRect, content);
            if (click == ProjectContextButtonClick.Left)
                Create(script, targetType, actions[0]);
            else if (click == ProjectContextButtonClick.Right)
                ShowMenu(script, targetType, actions);
        }

        private static List<EditorScriptKind> GetAvailableActions(Type targetType)
        {
            List<EditorScriptKind> actions = new();

            if (typeof(MonoBehaviour).IsAssignableFrom(targetType) ||
                typeof(ScriptableObject).IsAssignableFrom(targetType))
            {
                actions.Add(EditorScriptKind.CustomInspector);
            }

            if (typeof(PropertyAttribute).IsAssignableFrom(targetType))
            {
                actions.Add(EditorScriptKind.PropertyAttributeDrawer);
            }
            else if (!typeof(Object).IsAssignableFrom(targetType) &&
                     (targetType.IsClass || targetType.IsValueType) &&
                     targetType.IsSerializable)
            {
                actions.Add(EditorScriptKind.PropertyDrawer);
            }

            return actions;
        }

        private static void ShowMenu(
            MonoScript script,
            Type targetType,
            IReadOnlyList<EditorScriptKind> actions)
        {
            GenericMenu menu = new();
            Placement placement = CurrentPlacement;

            menu.AddItem(
                new GUIContent("Create in Editor folder"),
                placement == Placement.EditorSubfolder,
                () => CurrentPlacement = Placement.EditorSubfolder);
            menu.AddItem(
                new GUIContent("Create in same folder"),
                placement == Placement.SameFolder,
                () => CurrentPlacement = Placement.SameFolder);
            if (ContextActionPreferences.InlineEditorsEnabled)
            {
                menu.AddItem(
                    new GUIContent("Create inline in target script"),
                    placement == Placement.Inline,
                    () => CurrentPlacement = Placement.Inline);
            }
            menu.AddSeparator(string.Empty);

            foreach (EditorScriptKind action in actions)
            {
                EditorScriptKind capturedAction = action;
                menu.AddItem(
                    new GUIContent(GetMenuLabel(action)),
                    false,
                    () => Create(script, targetType, capturedAction));
            }

            menu.ShowAsContext();
        }

        private static string GetMenuLabel(EditorScriptKind kind)
        {
            return kind switch
            {
                EditorScriptKind.CustomInspector => "Custom Inspector",
                EditorScriptKind.PropertyDrawer => "Property Drawer",
                EditorScriptKind.PropertyAttributeDrawer => "Property Attribute Drawer",
                _ => kind.ToString()
            };
        }

        private static void Create(
            MonoScript sourceScript,
            Type targetType,
            EditorScriptKind kind)
        {
            string sourcePath = AssetDatabase.GetAssetPath(sourceScript);
            string sourceDirectory = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(sourceDirectory)) return;

            Placement placement = CurrentPlacement;
            if (placement == Placement.Inline)
            {
                if (!ContextActionPreferences.InlineEditorsEnabled)
                {
                    CurrentPlacement = placement = Placement.EditorSubfolder;
                }
                else
                {
                    AppendInlineEditor(sourcePath, sourceScript, targetType, kind);
                    return;
                }
            }

            string destinationDirectory = sourceDirectory;
            if (placement == Placement.EditorSubfolder)
            {
                destinationDirectory += "/Editor";
                if (!AssetDatabase.IsValidFolder(destinationDirectory))
                    AssetDatabase.CreateFolder(sourceDirectory, "Editor");
            }

            string suffix = kind == EditorScriptKind.CustomInspector ? "Inspector" : "Drawer";
            string className = GetSimpleTypeName(targetType) + suffix;
            string destinationPath = $"{destinationDirectory}/{className}.cs";

            if (File.Exists(destinationPath) &&
                !EditorUtility.DisplayDialog(
                    "Create Editor Script",
                    $"{destinationPath} already exists.",
                    "Replace",
                    "Cancel"))
            {
                return;
            }

            File.WriteAllText(
                destinationPath,
                BuildSource(targetType, className, kind, placement),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceUpdate);

            Object createdAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(destinationPath);
            Selection.activeObject = createdAsset;
            EditorGUIUtility.PingObject(createdAsset);
        }

        private static void AppendInlineEditor(
            string sourcePath,
            MonoScript sourceScript,
            Type targetType,
            EditorScriptKind kind)
        {
            string suffix = kind == EditorScriptKind.CustomInspector ? "Inspector" : "Drawer";
            string className = GetSimpleTypeName(targetType) + suffix;
            string existingSource = File.ReadAllText(sourcePath);
            if (existingSource.Contains("class " + className) ||
                existingSource.Contains("struct " + className))
            {
                EditorUtility.DisplayDialog(
                    "Create Inline Editor",
                    $"{className} already appears to exist in {sourcePath}.",
                    "OK");
                return;
            }

            StringBuilder updatedSource = new(existingSource.TrimEnd());
            updatedSource.AppendLine();
            updatedSource.AppendLine();
            updatedSource.Append(BuildInlineSource(targetType, className, kind));
            File.WriteAllText(
                sourcePath,
                updatedSource.ToString(),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceUpdate);
            Selection.activeObject = sourceScript;
            EditorGUIUtility.PingObject(sourceScript);
        }

        private static string BuildInlineSource(
            Type targetType,
            string className,
            EditorScriptKind kind)
        {
            StringBuilder source = new();
            source.AppendLine("#if UNITY_EDITOR");
            string targetTypeName = GetCSharpTypeName(targetType);

            if (kind == EditorScriptKind.CustomInspector)
            {
                source.Append("[global::UnityEditor.CustomEditor(typeof(")
                    .Append(targetTypeName).AppendLine("))]");
                source.Append("public sealed class ").Append(className)
                    .AppendLine(" : global::UnityEditor.Editor");
                source.AppendLine("{");
                source.AppendLine("    public override void OnInspectorGUI()");
                source.AppendLine("    {");
                source.AppendLine("        DrawDefaultInspector();");
                source.AppendLine("    }");
                source.AppendLine("}");
            }
            else
            {
                source.Append("[global::UnityEditor.CustomPropertyDrawer(typeof(")
                    .Append(targetTypeName).AppendLine("))]");
                source.Append("public sealed class ").Append(className)
                    .AppendLine(" : global::UnityEditor.PropertyDrawer");
                source.AppendLine("{");
                source.AppendLine("    public override void OnGUI(");
                source.AppendLine("        global::UnityEngine.Rect position,");
                source.AppendLine("        global::UnityEditor.SerializedProperty property,");
                source.AppendLine("        global::UnityEngine.GUIContent label)");
                source.AppendLine("    {");
                source.AppendLine("        global::UnityEditor.EditorGUI.PropertyField(position, property, label, true);");
                source.AppendLine("    }");
                source.AppendLine();
                source.AppendLine("    public override float GetPropertyHeight(");
                source.AppendLine("        global::UnityEditor.SerializedProperty property,");
                source.AppendLine("        global::UnityEngine.GUIContent label)");
                source.AppendLine("    {");
                source.AppendLine("        return global::UnityEditor.EditorGUI.GetPropertyHeight(property, label, true);");
                source.AppendLine("    }");
                source.AppendLine("}");
            }

            source.AppendLine("#endif");
            return source.ToString();
        }

        private static string BuildSource(
            Type targetType,
            string className,
            EditorScriptKind kind,
            Placement placement)
        {
            StringBuilder source = new();
            if (placement == Placement.SameFolder)
                source.AppendLine("#if UNITY_EDITOR");

            source.AppendLine("using UnityEditor;");
            source.AppendLine("using UnityEngine;");
            source.AppendLine();

            string targetTypeName = GetCSharpTypeName(targetType);
            string targetNamespace = targetType.Namespace;
            if (!string.IsNullOrEmpty(targetNamespace))
            {
                source.Append("namespace ").Append(targetNamespace).AppendLine();
                source.AppendLine("{");
            }

            string indent = string.IsNullOrEmpty(targetNamespace) ? string.Empty : "    ";
            if (kind == EditorScriptKind.CustomInspector)
            {
                source.Append(indent).Append("[CustomEditor(typeof(")
                    .Append(targetTypeName).AppendLine("))]");
                source.Append(indent).Append("public sealed class ").Append(className)
                    .AppendLine(" : UnityEditor.Editor");
                source.Append(indent).AppendLine("{");
                source.Append(indent).AppendLine("    public override void OnInspectorGUI()");
                source.Append(indent).AppendLine("    {");
                source.Append(indent).AppendLine("        DrawDefaultInspector();");
                source.Append(indent).AppendLine("    }");
                source.Append(indent).AppendLine("}");
            }
            else
            {
                source.Append(indent).Append("[CustomPropertyDrawer(typeof(")
                    .Append(targetTypeName).AppendLine("))]");
                source.Append(indent).Append("public sealed class ").Append(className)
                    .AppendLine(" : PropertyDrawer");
                source.Append(indent).AppendLine("{");
                source.Append(indent).AppendLine("    public override void OnGUI(");
                source.Append(indent).AppendLine("        Rect position,");
                source.Append(indent).AppendLine("        SerializedProperty property,");
                source.Append(indent).AppendLine("        GUIContent label)");
                source.Append(indent).AppendLine("    {");
                source.Append(indent).AppendLine("        EditorGUI.PropertyField(position, property, label, true);");
                source.Append(indent).AppendLine("    }");
                source.AppendLine();
                source.Append(indent).AppendLine("    public override float GetPropertyHeight(");
                source.Append(indent).AppendLine("        SerializedProperty property,");
                source.Append(indent).AppendLine("        GUIContent label)");
                source.Append(indent).AppendLine("    {");
                source.Append(indent).AppendLine("        return EditorGUI.GetPropertyHeight(property, label, true);");
                source.Append(indent).AppendLine("    }");
                source.Append(indent).AppendLine("}");
            }

            if (!string.IsNullOrEmpty(targetNamespace)) source.AppendLine("}");
            if (placement == Placement.SameFolder) source.AppendLine("#endif");
            return source.ToString();
        }

        private static string GetSimpleTypeName(Type type)
        {
            int genericMarker = type.Name.IndexOf('`');
            return genericMarker < 0 ? type.Name : type.Name.Substring(0, genericMarker);
        }

        private static string GetCSharpTypeName(Type type)
        {
            return "global::" + (type.FullName ?? type.Name).Replace('+', '.');
        }
    }
}
