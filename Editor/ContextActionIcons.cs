using UnityEditor;
using UnityEngine;

namespace ContextActionsSlop.Editor
{
    internal static class ContextActionIcons
    {
        private const string AddFolderPath =
            "Assets/Plugins/Context Actions Slop/Icons/Add-Folder.svg";
        private const string OpenInImageEditorPath =
            "Assets/Plugins/Context Actions Slop/Icons/Open-Image-Editor.svg";

        private static Texture2D _addFolder;
        private static Texture2D _openInImageEditor;

        public static Texture2D AddFolder
        {
            get
            {
                if (_addFolder == null)
                {
                    _addFolder = LoadTexture(AddFolderPath);
                }

                return _addFolder;
            }
        }

        public static Texture2D OpenInImageEditor
        {
            get
            {
                if (_openInImageEditor == null)
                {
                    _openInImageEditor = LoadTexture(OpenInImageEditorPath);
                }

                return _openInImageEditor;
            }
        }

        private static Texture2D LoadTexture(string path)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null) return texture;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite != null ? sprite.texture : null;
        }
    }
}
