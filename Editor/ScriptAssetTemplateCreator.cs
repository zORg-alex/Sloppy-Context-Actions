using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace SloppyContextActions.Editor
{
    internal static class ScriptAssetTemplateCreator
    {
        private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
            "char", "checked", "class", "const", "continue", "decimal", "default",
            "delegate", "do", "double", "else", "enum", "event", "explicit",
            "extern", "false", "finally", "fixed", "float", "for", "foreach",
            "goto", "if", "implicit", "in", "int", "interface", "internal", "is",
            "lock", "long", "namespace", "new", "null", "object", "operator", "out",
            "override", "params", "private", "protected", "public", "readonly", "ref",
            "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static",
            "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

        public static void Create(
            ProjectContextItem item,
            string templatePath,
            string defaultName,
            bool appendExtensionsSuffix = false)
        {
            Selection.activeObject = item.Asset;
            CreateScriptAssetAction action =
                ScriptableObject.CreateInstance<CreateScriptAssetAction>();
            action.AppendExtensionsSuffix = appendExtensionsSuffix;
            Texture2D icon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                EntityId.None,
                action,
                defaultName,
                icon,
                templatePath);
        }

        public static string GetNamespaceForDirectory(string assetDirectory)
        {
            if (string.IsNullOrWhiteSpace(assetDirectory)) return string.Empty;

            string normalized = assetDirectory.Replace('\\', '/').Trim('/');
            string[] segments = normalized.Split('/');
            if (segments.Length == 0 ||
                !segments[0].Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            List<string> namespaceSegments = new();
            for (int index = 1; index < segments.Length; index++)
            {
                string identifier = SanitizeIdentifier(segments[index]);
                if (!string.IsNullOrEmpty(identifier)) namespaceSegments.Add(identifier);
            }

            return string.Join(".", namespaceSegments);
        }

        private static string ProcessTemplate(
            string template,
            string scriptName,
            string typeName,
            string destinationPath)
        {
            string source = template
                .Replace("#SCRIPTNAME#", scriptName)
                .Replace("#TYPE#", typeName);

            bool hasNamespaceMarkers =
                source.Contains("#ROOTNAMESPACEBEGIN#") &&
                source.Contains("#ROOTNAMESPACEEND#");

            if (!destinationPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return RemoveNamespaceMarkers(source).Trim() + Environment.NewLine;
            }

            string directory = Path.GetDirectoryName(destinationPath)?.Replace('\\', '/');
            string namespaceName = GetNamespaceForDirectory(directory);
            if (string.IsNullOrEmpty(namespaceName))
            {
                return RemoveNamespaceMarkers(source).Trim() + Environment.NewLine;
            }

            if (hasNamespaceMarkers)
            {
                return source
                    .Replace(
                        "#ROOTNAMESPACEBEGIN#",
                        $"namespace {namespaceName}{Environment.NewLine}{{")
                    .Replace("#ROOTNAMESPACEEND#", "}")
                    .Trim() + Environment.NewLine;
            }

            source = RemoveNamespaceMarkers(source).Trim();

            return $"namespace {namespaceName}{Environment.NewLine}" +
                   $"{{{Environment.NewLine}" +
                   Indent(source) + Environment.NewLine +
                   $"}}{Environment.NewLine}";
        }

        private static string RemoveNamespaceMarkers(string source)
        {
            return source
                .Replace("#ROOTNAMESPACEBEGIN#", string.Empty)
                .Replace("#ROOTNAMESPACEEND#", string.Empty);
        }

        private static string Indent(string source)
        {
            string normalized = source.Replace("\r\n", "\n");
            string[] lines = normalized.Split('\n');
            StringBuilder result = new();
            for (int index = 0; index < lines.Length; index++)
            {
                if (lines[index].Length > 0) result.Append("    ");
                result.Append(lines[index]);
                if (index < lines.Length - 1) result.AppendLine();
            }

            return result.ToString();
        }

        private static string RemoveExtensionsSuffix(string name)
        {
            const string suffix = "Extensions";
            return name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                ? name.Substring(0, name.Length - suffix.Length)
                : name;
        }

        private static string SanitizeIdentifier(string value)
        {
            StringBuilder result = new();
            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character) || character == '_')
                    result.Append(character);
            }

            if (result.Length == 0) return string.Empty;
            if (char.IsDigit(result[0])) result.Insert(0, '_');
            if (Keywords.Contains(result.ToString())) result.Insert(0, '_');
            return result.ToString();
        }

        private sealed class CreateScriptAssetAction : AssetCreationEndAction
        {
            public bool AppendExtensionsSuffix { get; set; }

            public override void Action(
                EntityId entityId,
                string pathName,
                string resourceFile)
            {
                string enteredName = Path.GetFileNameWithoutExtension(pathName);
                string typeName = SanitizeIdentifier(
                    AppendExtensionsSuffix
                        ? RemoveExtensionsSuffix(enteredName)
                        : enteredName);
                if (string.IsNullOrEmpty(typeName)) typeName = "NewType";

                string scriptName = AppendExtensionsSuffix
                    ? typeName + "Extensions"
                    : typeName;
                string directory = Path.GetDirectoryName(pathName)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(directory)) return;

                string extension = Path.GetExtension(pathName);
                string destinationPath = $"{directory}/{scriptName}{extension}";
                if (File.Exists(destinationPath))
                {
                    EditorUtility.DisplayDialog(
                        "Create Script Asset",
                        $"{destinationPath} already exists.",
                        "OK");
                    return;
                }

                string template = File.ReadAllText(resourceFile);
                string source = ProcessTemplate(
                    template,
                    scriptName,
                    typeName,
                    destinationPath);
                File.WriteAllText(
                    destinationPath,
                    source,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                AssetDatabase.ImportAsset(
                    destinationPath,
                    ImportAssetOptions.ForceSynchronousImport);

                UnityEngine.Object createdAsset =
                    AssetDatabase.LoadMainAssetAtPath(destinationPath);
                ProjectWindowUtil.ShowCreatedAsset(createdAsset);
            }
        }
    }
}
