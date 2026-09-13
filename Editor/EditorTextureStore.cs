using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SloppyContextActions.Editor
{
    /// <summary>
    /// Loads editor textures from this asset's configured folder and can hide their
    /// source files from Unity without discarding them. Copy this file into another
    /// asset and change <see cref="TextureFolderGuid"/> and the menu paths.
    /// </summary>
    public static class EditorTextureStore
    {
        private const string TextureFolderGuid = "ba440951382248698988b6ee01ee94ef";
        private const string HiddenFolderName = ".hidden";
        private const string CacheFolderName = ".cache";
        private const string ManifestFileName = "manifest.txt";
        private const string MenuRoot = "Assets/Sloppy Context Actions/Editor Textures/";

        private static readonly Dictionary<string, Texture2D> Cache =
            new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<Texture2D> OwnedTextures = new();

        static EditorTextureStore()
        {
            EditorApplication.projectChanged += ClearCache;
        }

        /// <summary>
        /// Loads a texture by file name from the visible folder first, then from the
        /// hidden cache. Include the extension when two files share the same base name.
        /// </summary>
        public static Texture2D GetTexture(string fileName)
        {
            if (!HasValidTextureFolder()) return null;

            string safeFileName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeFileName)) return null;
            if (Cache.TryGetValue(safeFileName, out Texture2D cached)) return cached;

            Texture2D texture = LoadVisible(safeFileName);
            if (texture == null) texture = LoadHidden(safeFileName);

            Cache[safeFileName] = texture;
            return texture;
        }

        /// <summary>
        /// Moves the configured folder's textures and their meta files into its
        /// dot-prefixed hidden folder. Original files are never deleted.
        /// </summary>
        public static void Hide()
        {
            if (!HasValidTextureFolder()) return;

            string textureFolder = TextureFolderPath;
            List<string> texturePaths = GetVisibleTexturePaths().ToList();
            if (texturePaths.Count == 0) return;

            string hiddenFolder = HiddenFolderPath;
            string cacheFolder = CacheFolderPath;
            Directory.CreateDirectory(hiddenFolder);
            Directory.CreateDirectory(cacheFolder);

            foreach (string texturePath in texturePaths)
            {
                string fileName = Path.GetFileName(texturePath);
                string hiddenPath = Path.Combine(hiddenFolder, fileName);
                if (File.Exists(hiddenPath))
                {
                    Debug.LogError(
                        $"Cannot hide editor textures because '{hiddenPath}' already exists.");
                    DeleteIncompleteCache();
                    return;
                }

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture == null || !TryWritePngCache(texture, fileName))
                {
                    Debug.LogError(
                        $"Could not cache '{texturePath}'. No source files were moved.");
                    DeleteIncompleteCache();
                    return;
                }
            }

            List<string> movedFiles = new();
            bool succeeded = false;
            try
            {
                foreach (string texturePath in texturePaths)
                {
                    string fileName = Path.GetFileName(texturePath);
                    MoveFile(texturePath, Path.Combine(hiddenFolder, fileName), movedFiles);

                    string metaPath = texturePath + ".meta";
                    if (File.Exists(metaPath))
                    {
                        MoveFile(
                            metaPath,
                            Path.Combine(hiddenFolder, fileName + ".meta"),
                            movedFiles);
                    }
                }

                File.WriteAllLines(
                    ManifestPath,
                    texturePaths.Select(Path.GetFileName));
                succeeded = true;
            }
            catch (Exception exception)
            {
                RollBackMoves(movedFiles);
                DeleteIncompleteCache();
                Debug.LogException(exception);
            }
            finally
            {
                ClearCache();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            if (!succeeded) return;

            Debug.Log(
                $"Hidden {texturePaths.Count} editor texture(s) from '{textureFolder}'. " +
                "Use Show Editor Textures on the same folder to restore them.");
        }

        /// <summary>
        /// Restores source textures and their original meta files to the configured folder.
        /// </summary>
        public static void Show()
        {
            if (!HasValidTextureFolder()) return;
            if (!File.Exists(ManifestPath)) return;

            string textureFolder = TextureFolderPath;
            string hiddenFolder = HiddenFolderPath;
            string[] fileNames = File.ReadAllLines(ManifestPath)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (string fileName in fileNames)
            {
                string destinationPath = Path.Combine(textureFolder, fileName);
                if (File.Exists(destinationPath))
                {
                    Debug.LogError(
                        $"Cannot show editor textures because '{destinationPath}' already exists.");
                    return;
                }
            }

            List<string> movedFiles = new();
            bool succeeded = false;
            try
            {
                foreach (string fileName in fileNames)
                {
                    string hiddenPath = Path.Combine(hiddenFolder, fileName);
                    if (!File.Exists(hiddenPath))
                        throw new FileNotFoundException("A hidden texture is missing.", hiddenPath);

                    MoveFile(hiddenPath, Path.Combine(textureFolder, fileName), movedFiles);

                    string hiddenMetaPath = hiddenPath + ".meta";
                    if (File.Exists(hiddenMetaPath))
                    {
                        MoveFile(
                            hiddenMetaPath,
                            Path.Combine(textureFolder, fileName + ".meta"),
                            movedFiles);
                    }
                }

                succeeded = true;
            }
            catch (Exception exception)
            {
                RollBackMoves(movedFiles);
                Debug.LogException(exception);
            }

            if (succeeded)
            {
                try
                {
                    File.Delete(ManifestPath);
                    DeleteDirectoryIfPresent(CacheFolderPath);
                    DeleteDirectoryIfEmpty(HiddenFolderPath);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "The texture sources were restored, but hidden cache cleanup " +
                        $"was incomplete: {exception.Message}");
                }
            }

            ClearCache();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!succeeded) return;

            Debug.Log(
                $"Restored {fileNames.Length} editor texture(s) to '{textureFolder}'.");
        }

        [MenuItem(MenuRoot + "Hide", false, 2000)]
        private static void HideMenuItem()
        {
            Hide();
        }

        [MenuItem(MenuRoot + "Hide", true)]
        private static bool ValidateHideMenuItem()
        {
            return IsConfiguredFolderSelected() && GetVisibleTexturePaths().Any();
        }

        [MenuItem(MenuRoot + "Show", false, 2001)]
        private static void ShowMenuItem()
        {
            Show();
        }

        [MenuItem(MenuRoot + "Show", true)]
        private static bool ValidateShowMenuItem()
        {
            return IsConfiguredFolderSelected() && File.Exists(ManifestPath);
        }

        private static string TextureFolderPath =>
            AssetDatabase.GUIDToAssetPath(TextureFolderGuid);

        private static string HiddenFolderPath =>
            Path.Combine(TextureFolderPath, HiddenFolderName);

        private static string CacheFolderPath =>
            Path.Combine(HiddenFolderPath, CacheFolderName);

        private static string ManifestPath =>
            Path.Combine(HiddenFolderPath, ManifestFileName);

        private static Texture2D LoadVisible(string fileName)
        {
            string exactPath = NormalizeAssetPath(Path.Combine(TextureFolderPath, fileName));
            Texture2D exact = AssetDatabase.LoadAssetAtPath<Texture2D>(exactPath);
            if (exact != null || Path.HasExtension(fileName)) return exact;

            foreach (string path in GetVisibleTexturePaths())
            {
                if (!Path.GetFileNameWithoutExtension(path)
                        .Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            return null;
        }

        private static Texture2D LoadHidden(string fileName)
        {
            string hiddenSourcePath = FindHiddenSourcePath(fileName);
            if (IsDirectlyLoadableImage(hiddenSourcePath))
            {
                Texture2D sourceTexture = LoadImageFile(hiddenSourcePath, fileName);
                if (sourceTexture != null) return sourceTexture;
            }

            string cachePath = FindHiddenCachePath(fileName);
            if (string.IsNullOrEmpty(cachePath) || !File.Exists(cachePath)) return null;

            return LoadImageFile(cachePath, fileName);
        }

        private static Texture2D LoadImageFile(string path, string fileName)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false)
            {
                name = Path.GetFileNameWithoutExtension(fileName),
                hideFlags = HideFlags.HideAndDontSave
            };

            if (ImageConversion.LoadImage(texture, File.ReadAllBytes(path), true))
            {
                OwnedTextures.Add(texture);
                return texture;
            }

            UnityEngine.Object.DestroyImmediate(texture);
            return null;
        }

        private static string FindHiddenSourcePath(string fileName)
        {
            string exactPath = Path.Combine(HiddenFolderPath, fileName);
            if (File.Exists(exactPath) || Path.HasExtension(fileName)) return exactPath;
            if (!File.Exists(ManifestPath)) return null;

            string matchingName = File.ReadAllLines(ManifestPath)
                .Select(Path.GetFileName)
                .FirstOrDefault(name =>
                    string.Equals(
                        Path.GetFileNameWithoutExtension(name),
                        fileName,
                        StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(matchingName)
                ? null
                : Path.Combine(HiddenFolderPath, matchingName);
        }

        private static bool IsDirectlyLoadableImage(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            string extension = Path.GetExtension(path);
            return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        private static string FindHiddenCachePath(string fileName)
        {
            string exactPath = Path.Combine(CacheFolderPath, fileName + ".png");
            if (File.Exists(exactPath) || Path.HasExtension(fileName)) return exactPath;
            if (!Directory.Exists(CacheFolderPath)) return null;

            return Directory.EnumerateFiles(CacheFolderPath, "*.png")
                .FirstOrDefault(path =>
                    Path.GetFileNameWithoutExtension(
                            Path.GetFileNameWithoutExtension(path))
                        .Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> GetVisibleTexturePaths()
        {
            string textureFolder = TextureFolderPath;
            if (!AssetDatabase.IsValidFolder(textureFolder) ||
                !Directory.Exists(textureFolder))
            {
                yield break;
            }

            foreach (string filePath in Directory.EnumerateFiles(
                         textureFolder,
                         "*",
                         SearchOption.TopDirectoryOnly))
            {
                if (filePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                string path = NormalizeAssetPath(filePath);
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null) continue;

                yield return path;
            }
        }

        private static bool TryWritePngCache(Texture2D source, string fileName)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            RenderTexture previous = RenderTexture.active;
            Texture2D readable = null;

            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                readable = new Texture2D(
                    source.width,
                    source.height,
                    TextureFormat.RGBA32,
                    false);
                readable.ReadPixels(
                    new Rect(0, 0, source.width, source.height),
                    0,
                    0);
                readable.Apply();

                byte[] png = readable.EncodeToPNG();
                if (png == null || png.Length == 0) return false;
                File.WriteAllBytes(Path.Combine(CacheFolderPath, fileName + ".png"), png);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
            finally
            {
                if (readable != null) UnityEngine.Object.DestroyImmediate(readable);
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private static void MoveFile(
            string sourcePath,
            string destinationPath,
            ICollection<string> movedDestinations)
        {
            File.Move(sourcePath, destinationPath);
            movedDestinations.Add(destinationPath);
        }

        private static void RollBackMoves(IReadOnlyList<string> movedDestinations)
        {
            for (int index = movedDestinations.Count - 1; index >= 0; index--)
            {
                string destination = movedDestinations[index];
                string sourceDirectory = Path.GetDirectoryName(destination);
                string originalDirectory = sourceDirectory != null &&
                                           sourceDirectory.EndsWith(
                                               HiddenFolderName,
                                               StringComparison.OrdinalIgnoreCase)
                    ? TextureFolderPath
                    : HiddenFolderPath;
                string originalPath = Path.Combine(
                    originalDirectory,
                    Path.GetFileName(destination));

                if (File.Exists(destination) && !File.Exists(originalPath))
                    File.Move(destination, originalPath);
            }
        }

        private static bool IsConfiguredFolderSelected()
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject)
                ?.TrimEnd('/');
            return string.Equals(
                selectedPath,
                TextureFolderPath,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasValidTextureFolder()
        {
            return !string.IsNullOrEmpty(TextureFolderPath) &&
                   AssetDatabase.IsValidFolder(TextureFolderPath);
        }

        private static string NormalizeAssetPath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static void DeleteDirectoryIfPresent(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        private static void DeleteDirectoryIfEmpty(string path)
        {
            if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                Directory.Delete(path);
        }

        private static void DeleteIncompleteCache()
        {
            if (!File.Exists(ManifestPath))
            {
                DeleteDirectoryIfPresent(CacheFolderPath);
                DeleteDirectoryIfEmpty(HiddenFolderPath);
            }
        }

        private static void ClearCache()
        {
            foreach (Texture2D texture in OwnedTextures)
            {
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }

            OwnedTextures.Clear();
            Cache.Clear();
        }
    }
}
