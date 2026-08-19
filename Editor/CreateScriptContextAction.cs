using System;
using System.Reflection;
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
            new("C# Custom Editor Script", "C# Custom Editor-NewCustomEditor.cs.txt", "NewCustomEditor.cs", requiresEditorFolder: true),
            new("C# Custom Property Drawer", "C# Custom Property Drawer-NewPropertyDrawer.cs.txt", "NewPropertyDrawer.cs", requiresEditorFolder: true),
            new("C# Editor Window Script", "C# Editor Window-NewEditorWindow.cs.txt", "NewEditorWindow.cs", requiresEditorFolder: true),
            new(string.Empty, string.Empty, string.Empty),
            new("C# Test Script", "C# Test Script-NewTestScript.cs.txt", "NewTestScript.cs"),
            new(string.Empty, string.Empty, string.Empty),
            new("Assembly Definition", "Assembly Definition-NewAssembly.asmdef.txt", "NewAssembly.asmdef"),
            new("Assembly Definition Reference", "Assembly Definition Reference-NewAssemblyReference.asmref.txt", "NewAssemblyReference.asmref")
        };

        private static readonly PackageTemplate[] PackageTemplates =
        {
            new("Job System/IJob", "Jobs IJob-NewJob.cs.txt", "NewJob.cs", "Unity.Jobs.IJob", "Jobs IJob Burst-NewJob.cs.txt"),
            new("Job System/IJobParallelFor", "Jobs IJobParallelFor-NewJob.cs.txt", "NewJob.cs", "Unity.Jobs.IJobParallelFor", "Jobs IJobParallelFor Burst-NewJob.cs.txt"),
            new("Job System/IJobParallelForTransform", "Jobs IJobParallelForTransform-NewJob.cs.txt", "NewJob.cs", "UnityEngine.Jobs.IJobParallelForTransform", "Jobs IJobParallelForTransform Burst-NewJob.cs.txt"),
            new("Job System/IJobFor", "Jobs IJobFor-NewJob.cs.txt", "NewJob.cs", "Unity.Jobs.IJobFor", "Jobs IJobFor Burst-NewJob.cs.txt"),
            new("Entities/IComponentData", "Entities IComponentData-NewComponentData.cs.txt", "NewComponentData.cs", "Unity.Entities.IComponentData"),
            new("Entities/IJobEntity", "Entities IJobEntity-NewJob.cs.txt", "NewJob.cs", "Unity.Entities.IJobEntity"),
            new("Entities/ISystem", "Entities ISystem-NewSystem.cs.txt", "NewSystem.cs", "Unity.Entities.ISystem"),
            new("Entities/Baker", "Entities Baker-NewAuthoring.cs.txt", "NewAuthoring.cs", "Unity.Entities.Baker`1"),
            new("Entities/SystemBase", "Entities SystemBase-NewSystem.cs.txt", "NewSystem.cs", "Unity.Entities.SystemBase"),
            new("Zenject/Mono Installer", "Zenject MonoInstaller-NewMonoInstaller.cs.txt", "NewMonoInstaller.cs", "Zenject.MonoInstaller"),
            new("Zenject/ScriptableObject Installer", "Zenject ScriptableObjectInstaller-NewScriptableObjectInstaller.cs.txt", "NewScriptableObjectInstaller.cs", "Zenject.ScriptableObjectInstaller")
        };

        static CreateScriptContextAction()
        {
            CurrentFolderActionHost.Register(
                RegistrationId,
                Draw,
                path =>
                    !string.IsNullOrEmpty(path) &&
                    path.StartsWith("Assets", StringComparison.Ordinal) &&
                    path.IndexOf("Scripts", StringComparison.OrdinalIgnoreCase) >= 0,
                order: -90);
            ProjectContextActionHost.RegisterTreeFolder(RegistrationId, Draw, order: -90);
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
                if (capturedTemplate.RequiresEditorFolder && !IsEditorFolder(item.Path))
                {
                    menu.AddDisabledItem(
                        new GUIContent(capturedTemplate.Label + " (requires Editor folder)"));
                    continue;
                }

                menu.AddItem(
                    new GUIContent(capturedTemplate.Label),
                    false,
                    () => Create(item, capturedTemplate));
            }

            AppendPackageTemplates(menu, item);

            menu.ShowAsContext();
        }

        private static void AppendPackageTemplates(GenericMenu menu, ProjectContextItem item)
        {
            bool separatorAdded = false;
            bool hasBurst = HasType("Unity.Burst.BurstCompileAttribute");

            foreach (PackageTemplate template in PackageTemplates)
            {
                if (!HasType(template.RequiredTypeName)) continue;
                if (!separatorAdded)
                {
                    menu.AddSeparator(string.Empty);
                    separatorAdded = true;
                }

                string fileName = hasBurst && !string.IsNullOrEmpty(template.BurstFileName)
                    ? template.BurstFileName
                    : template.FileName;
                ScriptTemplate capturedTemplate = new(
                    template.Label,
                    fileName,
                    template.DefaultName);
                menu.AddItem(
                    new GUIContent(capturedTemplate.Label),
                    false,
                    () => Create(item, capturedTemplate));
            }
        }

        private static bool HasType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType(fullName, throwOnError: false) != null) return true;
            }

            return false;
        }

        private static bool IsEditorFolder(string path)
        {
            foreach (string segment in path.Split('/'))
            {
                if (segment.Equals("Editor", StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
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
            public bool RequiresEditorFolder { get; }

            public ScriptTemplate(
                string label,
                string fileName,
                string defaultName,
                bool requiresEditorFolder = false)
            {
                Label = label;
                FileName = fileName;
                DefaultName = defaultName;
                RequiresEditorFolder = requiresEditorFolder;
            }
        }

        private readonly struct PackageTemplate
        {
            public string Label { get; }
            public string FileName { get; }
            public string DefaultName { get; }
            public string RequiredTypeName { get; }
            public string BurstFileName { get; }

            public PackageTemplate(
                string label,
                string fileName,
                string defaultName,
                string requiredTypeName,
                string burstFileName = null)
            {
                Label = label;
                FileName = fileName;
                DefaultName = defaultName;
                RequiredTypeName = requiredTypeName;
                BurstFileName = burstFileName;
            }
        }
    }
}
