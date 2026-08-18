using UnityEditor;
using UnityEngine;

namespace ContextActionsSlop.Editor
{
    internal static class ContextActionIcons
    {
        private const string AddFolderPath =
            "Assets/Plugins/Context Actions Slop/Icons/Add-Folder.png";

        private static Texture2D _addFolder;

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
    }
}
