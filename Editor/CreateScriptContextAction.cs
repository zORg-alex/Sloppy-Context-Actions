using System;
using UnityEditor;
using UnityEngine;

namespace ContextActionsSlop.Editor
{
    [InitializeOnLoad]
    internal static class CreateScriptContextAction
    {
        private const string RegistrationId = "context-actions-slop.create-script";
        private const string TemplateRoot =
            "Assets/Plugins/Context Actions Slop/Editor/ScriptTemplates/";

        private static readonly ScriptTemplate[] Templates =
        {
            new("C# Script", "C# Script-NewBehaviourScript.cs.txt", "NewBehaviourScript.cs"),
            new("C# ScriptableObject", "C# ScriptableObject-NewScriptableObject.cs.txt", "NewScriptableObject.cs"),
            new(string.Empty, string.Empty, string.Empty),
            new("C# Class", "C# Class-NewClass.cs.txt", "NewClass.cs"),
            new("C# Interface", "C# Interface-NewInterface.cs.txt", "NewInterface.cs"),
            new("C# Abstract Class", "C# Abstract Class-NewAbstractClass.cs.txt", "NewAbstractClass.cs"),
            new("C# Struct", "C# Struct-NewStruct.cs.txt", "NewStruct.cs"),
            new("C# Enum", "C# Enum-NewEnum.cs.txt", "NewEnum.cs"),
            new(string.Empty, string.Empty, string.Empty),
            new("C# Test Script", "C# Test Script-NewTestScript.cs.txt", "NewTestScript.cs"),
            new(string.Empty, string.Empty, string.Empty),
            new("Assembly Definition", "Assembly Definition-NewAssembly.asmdef.txt", "NewAssembly.asmdef"),
            new("Assembly Definition Reference", "Assembly Definition Reference-NewAssemblyReference.asmref.txt", "NewAssemblyReference.asmref")
        };

        static CreateScriptContextAction()
        {
            ProjectContextActionHost.Register(RegistrationId, Draw, order: -90);
        }

        private static void Draw(ProjectContextItem item)
        {
            if (!item.IsFolder || !item.Path.StartsWith("Assets", StringComparison.Ordinal)) return;
            if (item.Path.IndexOf("Scripts", StringComparison.OrdinalIgnoreCase) < 0) return;

            Rect buttonRect = item.ReserveButtonRect();
            GUIContent content = new()
            {
                image = ContextActionIcons.AddScript,
                tooltip = "Create C# script\nRight-click to choose a template"
            };

            ProjectContextButtonClick click = ProjectContextButton.Draw(buttonRect, content);
            if (click == ProjectContextButtonClick.Left) Create(item, Templates[0]);
            else if (click == ProjectContextButtonClick.Right) ShowTemplateMenu(item);
        }

        private static void ShowTemplateMenu(ProjectContextItem item)
        {
            GenericMenu menu = new();
            foreach (ScriptTemplate template in Templates)
            {
                if (string.IsNullOrEmpty(template.Label))
                {
                    menu.AddSeparator(string.Empty);
                    continue;
                }

                ScriptTemplate capturedTemplate = template;
                menu.AddItem(
                    new GUIContent(capturedTemplate.Label),
                    false,
                    () => Create(item, capturedTemplate));
            }

            menu.ShowAsContext();
        }

        private static void Create(ProjectContextItem item, ScriptTemplate template)
        {
            Selection.activeObject = item.Asset;
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                TemplateRoot + template.FileName,
                template.DefaultName);
        }

        private readonly struct ScriptTemplate
        {
            public string Label { get; }
            public string FileName { get; }
            public string DefaultName { get; }

            public ScriptTemplate(string label, string fileName, string defaultName)
            {
                Label = label;
                FileName = fileName;
                DefaultName = defaultName;
            }
        }
    }
}
