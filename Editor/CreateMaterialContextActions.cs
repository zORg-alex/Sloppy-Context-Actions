using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ContextActionsSlop.Editor
{
    [InitializeOnLoad]
    internal static class CreateMaterialContextActions
    {
        private const string FolderRegistrationId =
            "context-actions-slop.create-material-in-folder";
        private const string TextureRegistrationId =
            "context-actions-slop.create-material-from-texture";
        private const string ShaderRegistrationId =
            "context-actions-slop.create-material-from-shader";

        static CreateMaterialContextActions()
        {
            CurrentFolderActionHost.Register(
                FolderRegistrationId,
                DrawFolderButton,
                IsMaterialFolder,
                order: -80);
            ProjectContextActionHost.RegisterTreeFolder(
                FolderRegistrationId,
                DrawFolderButton,
                order: -80);
            ProjectContextActionHost.Register(
                TextureRegistrationId,
                DrawTextureButton,
                order: 60);
            ProjectContextActionHost.Register(
                ShaderRegistrationId,
                DrawShaderButton,
                order: 60);
        }

        private static bool IsMaterialFolder(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.StartsWith("Assets", StringComparison.Ordinal) &&
                   path.IndexOf("Material", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void DrawFolderButton(ProjectContextItem item)
        {
            if (!item.IsFolder || !IsMaterialFolder(item.Path)) return;

            Rect buttonRect = item.ReserveButtonRect();
            GUIContent content = new()
            {
                image = ContextActionIcons.Material,
                tooltip = "Create Material"
            };

            if (ProjectContextButton.Draw(buttonRect, content) == ProjectContextButtonClick.Left)
                CreateMaterial(item.Asset, null, null, "New Material.mat");
        }

        private static void DrawTextureButton(ProjectContextItem item)
        {
            if (item.IsFolder || item.Asset is not Texture2D texture) return;

            Rect buttonRect = item.ReserveButtonRect();
            GUIContent content = new()
            {
                image = ContextActionIcons.Material,
                tooltip = "Create Material from Texture"
            };

            if (ProjectContextButton.Draw(buttonRect, content) == ProjectContextButtonClick.Left)
                CreateMaterial(texture, texture, null, texture.name + ".mat");
        }

        private static void DrawShaderButton(ProjectContextItem item)
        {
            if (item.IsFolder || item.Asset is not Shader shader) return;

            Rect buttonRect = item.ReserveButtonRect();
            GUIContent content = new()
            {
                image = ContextActionIcons.Material,
                tooltip = "Create Material from Shader"
            };

            if (ProjectContextButton.Draw(buttonRect, content) != ProjectContextButtonClick.Left)
                return;

            string shaderFileName = Path.GetFileNameWithoutExtension(item.Path);
            CreateMaterial(shader, null, shader, shaderFileName + ".mat");
        }

        private static void CreateMaterial(
            UnityEngine.Object destination,
            Texture texture,
            Shader selectedShader,
            string defaultName)
        {
            Shader shader = selectedShader != null ? selectedShader : FindDefaultShader();
            if (shader == null)
            {
                Debug.LogError(
                    "Context Actions Slop could not find a default Lit shader for the active render pipeline.");
                return;
            }

            Selection.activeObject = destination;
            Material material = new(shader);
            if (texture != null) material.mainTexture = texture;
            ProjectWindowUtil.CreateAsset(material, defaultName);
        }

        private static Shader FindDefaultShader()
        {
            RenderPipelineAsset pipeline = GraphicsSettings.defaultRenderPipeline;
            if (pipeline == null) return Shader.Find("Standard");

            string pipelineType = pipeline.GetType().Name;
            if (pipelineType.IndexOf("HDRenderPipeline", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Shader.Find("HDRP/Lit") ?? Shader.Find("HDRenderPipeline/Lit");
            }

            return Shader.Find("Universal Render Pipeline/Lit");
        }
    }
}
