using UnityEditor;
using UnityEngine;

namespace SloppyContextActions.Editor
{
    internal static class ContextActionIcons
    {
        private static Texture2D _addFolder;
        private static Texture2D _openInImageEditor;
        private static Texture2D _openInExplorer;
        private static Texture2D _addScript;
        private static bool _addFolderLoaded;
        private static bool _openInImageEditorLoaded;
        private static bool _openInExplorerLoaded;
        private static bool _addScriptLoaded;
        private static Texture2D _material;
        private static Texture2D _shader;
        private static Texture2D _audioPlay;
        private static Texture2D _audioStop;
        private static bool _materialLoaded;
        private static bool _shaderLoaded;
        private static bool _audioPlayLoaded;
        private static bool _audioStopLoaded;
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
                    _addScript = EditorTextureStore.GetTexture("Add-Script.svg");
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
                    _addFolder = EditorTextureStore.GetTexture("Add-Folder.svg");
                }

                return _addFolder;
            }
        }

        public static Texture2D OpenInImageEditor => LoadOnce(
            ref _openInImageEditor,
            ref _openInImageEditorLoaded,
            "Open-Image-Editor.svg");

        public static Texture2D OpenInExplorer => LoadOnce(
            ref _openInExplorer,
            ref _openInExplorerLoaded,
            "Folder.svg");

        public static Texture2D Material => LoadOnce(
            ref _material,
            ref _materialLoaded,
            "Material.svg");

        public static Texture2D Shader
        {
            get
            {
                Texture2D custom = LoadOnce(ref _shader, ref _shaderLoaded, "Shader.svg");
                return custom != null
                    ? custom
                    : EditorGUIUtility.IconContent("Shader Icon").image as Texture2D;
            }
        }

        public static Texture2D AudioPlay => LoadOnce(
            ref _audioPlay,
            ref _audioPlayLoaded,
            "Audio-Play.svg");

        public static Texture2D AudioStop => LoadOnce(
            ref _audioStop,
            ref _audioStopLoaded,
            "Audio-Stop.svg");

        private static Texture2D LoadOnce(
            ref Texture2D texture,
            ref bool loaded,
            string fileName)
        {
            if (loaded) return texture;

            loaded = true;
            texture = EditorTextureStore.GetTexture(fileName);
            return texture;
        }

        private static void ClearCache()
        {
            _addFolder = null;
            _openInImageEditor = null;
            _openInExplorer = null;
            _addScript = null;
            _material = null;
            _shader = null;
            _audioPlay = null;
            _audioStop = null;
            _addFolderLoaded = false;
            _openInImageEditorLoaded = false;
            _openInExplorerLoaded = false;
            _addScriptLoaded = false;
            _materialLoaded = false;
            _shaderLoaded = false;
            _audioPlayLoaded = false;
            _audioStopLoaded = false;
        }
    }
}
