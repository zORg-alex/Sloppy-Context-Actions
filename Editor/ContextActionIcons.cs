using UnityEditor;
using UnityEngine;

namespace ContextActionsSlop.Editor
{
    internal static class ContextActionIcons
    {
        private const string AddFolderPath =
            "Assets/Plugins/Context Actions Slop/Icons/Add-Folder.png";

        private static Texture2D _addFolder;
        private static Texture2D _openInImageEditor;

        public static Texture2D AddFolder
        {
            get
            {
                if (_addFolder == null)
                {
                    _addFolder = AssetDatabase.LoadAssetAtPath<Texture2D>(AddFolderPath);
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
                    GUIContent icon = EditorGUIUtility.IconContent(
                        EditorGUIUtility.isProSkin ? "d_editicon.sml" : "editicon.sml");
                    _openInImageEditor = icon?.image as Texture2D;
                }

                return _openInImageEditor;
            }
        }
    }
}
