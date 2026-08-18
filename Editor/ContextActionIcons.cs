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
        private static bool _addFolderLoaded;
        private static bool _openInImageEditorLoaded;

        public static Texture2D AddFolder
        {
            get
            {
                if (!_addFolderLoaded)
                {
                    _addFolderLoaded = true;
                    _addFolder = AssetDatabase.LoadAssetAtPath<Texture2D>(AddFolderPath);
                }

                return _addFolder;
            }
        }

        public static Texture2D OpenInImageEditor
        {
            get
            {
                if (!_openInImageEditorLoaded)
                {
                    _openInImageEditorLoaded = true;
                    _openInImageEditor =
                        AssetDatabase.LoadAssetAtPath<Texture2D>(OpenInImageEditorPath);
                }

                return _openInImageEditor;
            }
        }
    }
}
