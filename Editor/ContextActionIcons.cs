using UnityEditor;
using UnityEngine;

namespace ContextActionsSlop.Editor
{
    internal static class ContextActionIcons
    {
        private const string AddFolderGuid = "5d16f3ad8fcb4fb6a529123357671bb4";

        private static Texture2D _addFolder;

        public static Texture2D AddFolder
        {
            get
            {
                if (_addFolder == null)
                {
                    string path = AssetDatabase.GUIDToAssetPath(AddFolderGuid);
                    _addFolder = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }

                return _addFolder;
            }
        }
    }
}
