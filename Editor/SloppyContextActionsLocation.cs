using System.IO;
using UnityEditor;

namespace SloppyContextActions.Editor
{
    internal static class SloppyContextActionsLocation
    {
        private const string AssemblyDefinitionGuid = "e5f7d2f77502cbe46b439e76c5de29fb";

        public static string GetAssetPath(string relativePath)
        {
            string root = GetRootPath();
            return string.IsNullOrEmpty(root)
                ? string.Empty
                : $"{root}/{relativePath.TrimStart('/')}";
        }

        private static string GetRootPath()
        {
            string assemblyPath = AssetDatabase.GUIDToAssetPath(AssemblyDefinitionGuid);
            if (string.IsNullOrEmpty(assemblyPath)) return string.Empty;

            string editorDirectory = Path.GetDirectoryName(assemblyPath)?.Replace('\\', '/');
            return Path.GetDirectoryName(editorDirectory)?.Replace('\\', '/');
        }
    }
}
