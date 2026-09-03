using System;
using UnityEditor;
using UnityEngine;

namespace SloppyContextActions.Editor
{
    internal static class UrpScriptTemplates
    {
        private const string RequiredTypeName =
            "UnityEngine.Rendering.Universal.ScriptableRendererFeature";

        private static string TemplateRoot =>
            SloppyContextActionsLocation.GetAssetPath("Editor/ScriptTemplates/");

        private static readonly Definition[] Definitions =
        {
            new(
                "URP/Fullscreen Blit Renderer Feature",
                "URP Fullscreen Blit Renderer Feature-NewFullscreenBlitRendererFeature.cs.txt",
                "NewFullscreenBlitRendererFeature.cs"),
            new(
                "URP/Draw Objects Renderer Feature",
                "URP Draw Objects Renderer Feature-NewDrawObjectsRendererFeature.cs.txt",
                "NewDrawObjectsRendererFeature.cs"),
            new(
                "URP/Volume Component",
                "URP Volume Component-NewVolumeComponent.cs.txt",
                "NewVolumeComponent.cs")
        };

        public static bool IsAvailable =>
            ContextActionTypeCache.Find(RequiredTypeName) != null;

        public static void AppendMenu(
            GenericMenu menu,
            ProjectContextItem item,
            string labelPrefix,
            bool addSeparator)
        {
            if (!IsAvailable) return;
            if (addSeparator) menu.AddSeparator(string.Empty);

            foreach (Definition definition in Definitions)
            {
                Definition captured = definition;
                menu.AddItem(
                    new GUIContent(labelPrefix + captured.Label),
                    false,
                    () => Create(item, captured));
            }
        }

        private static void Create(ProjectContextItem item, Definition definition)
        {
            ScriptAssetTemplateCreator.Create(
                item,
                TemplateRoot + definition.FileName,
                definition.DefaultName);
        }

        private readonly struct Definition
        {
            public string Label { get; }
            public string FileName { get; }
            public string DefaultName { get; }

            public Definition(string label, string fileName, string defaultName)
            {
                Label = label;
                FileName = fileName;
                DefaultName = defaultName;
            }
        }
    }
}
