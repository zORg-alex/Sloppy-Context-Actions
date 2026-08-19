using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SloppyContextActions.Editor
{
    internal static class ContextActionIcons
    {
        private const string AddFolderPath =
            "Assets/Plugins/Sloppy Context Actions/Icons/Add-Folder.svg";
        private const string OpenInImageEditorPath =
            "Assets/Plugins/Sloppy Context Actions/Icons/Open-Image-Editor.svg";
        private const string AddScriptPath =
            "Assets/Plugins/Sloppy Context Actions/Icons/Add-Script.svg";
        private const string MaterialPath =
            "Assets/Plugins/Sloppy Context Actions/Icons/Material.svg";
        private const string ShaderPath =
            "Assets/Plugins/Sloppy Context Actions/Icons/Shader.svg";
        private const string AudioPlayPath =
            "Assets/Plugins/Sloppy Context Actions/Icons/Audio-Play.svg";
        private const string AudioStopPath =
            "Assets/Plugins/Sloppy Context Actions/Icons/Audio-Stop.svg";

        private static Texture2D _addFolder;
        private static Texture2D _openInImageEditor;
        private static Texture2D _addScript;
        private static bool _addFolderLoaded;
        private static bool _openInImageEditorLoaded;
        private static bool _addScriptLoaded;
        private static Texture2D _material;
        private static Texture2D _shader;
        private static Texture2D _audioPlay;
        private static Texture2D _audioStop;
        private static bool _materialLoaded;
        private static bool _shaderLoaded;
        private static bool _audioPlayLoaded;
        private static bool _audioStopLoaded;
        private static bool _shaderArtworkChecked;
        private static bool _shaderHasArtwork;

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

        public static Texture2D Material => LoadOnce(
            ref _material,
            ref _materialLoaded,
            MaterialPath);

        public static Texture2D Shader
        {
            get
            {
                Texture2D custom = LoadOnce(ref _shader, ref _shaderLoaded, ShaderPath);
                if (!_shaderArtworkChecked)
                {
                    _shaderArtworkChecked = true;
                    _shaderHasArtwork = HasSvgArtwork(ShaderPath);
                }

                if (_shaderHasArtwork) return custom;

                return EditorGUIUtility.IconContent("Shader Icon").image as Texture2D;
            }
        }

        public static Texture2D AudioPlay => LoadOnce(
            ref _audioPlay,
            ref _audioPlayLoaded,
            AudioPlayPath);

        public static Texture2D AudioStop => LoadOnce(
            ref _audioStop,
            ref _audioStopLoaded,
            AudioStopPath);

        private static Texture2D LoadOnce(
            ref Texture2D texture,
            ref bool loaded,
            string path)
        {
            if (loaded) return texture;

            loaded = true;
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return texture;
        }

        private static bool HasSvgArtwork(string path)
        {
            if (!File.Exists(path)) return false;

            string svg = File.ReadAllText(path);
            string[] artworkElements =
            {
                "<path", "<circle", "<ellipse", "<rect", "<line", "<polyline",
                "<polygon", "<text", "<use", "<image"
            };
            foreach (string element in artworkElements)
            {
                if (svg.IndexOf(element, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static void ClearCache()
        {
            _addFolder = null;
            _openInImageEditor = null;
            _addScript = null;
            _material = null;
            _shader = null;
            _audioPlay = null;
            _audioStop = null;
            _addFolderLoaded = false;
            _openInImageEditorLoaded = false;
            _addScriptLoaded = false;
            _materialLoaded = false;
            _shaderLoaded = false;
            _audioPlayLoaded = false;
            _audioStopLoaded = false;
            _shaderArtworkChecked = false;
            _shaderHasArtwork = false;
        }
    }
}
