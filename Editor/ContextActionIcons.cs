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
        private const string AddScriptPath =
            "Assets/Plugins/Context Actions Slop/Icons/Add-Script.svg";

        private static Texture2D _addFolder;
        private static Texture2D _openInImageEditor;
        private static Texture2D _addScript;
        private static bool _addFolderLoaded;
        private static bool _openInImageEditorLoaded;
        private static bool _addScriptLoaded;

        static ContextActionIcons()
        {
            EditorApplication.projectChanged += ClearCache;
        }

        public static Texture2D AddScript
        {
            get
            {
                if (!_addScriptLoaded)
                {
                    _addScriptLoaded = true;
                    _addScript = AssetDatabase.LoadAssetAtPath<Texture2D>(AddScriptPath);
                }

                return _addScript;
            }
        }

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

        private static void ClearCache()
        {
            _addFolder = null;
            _openInImageEditor = null;
            _addScript = null;
            _addFolderLoaded = false;
            _openInImageEditorLoaded = false;
            _addScriptLoaded = false;
        }
    }
}
