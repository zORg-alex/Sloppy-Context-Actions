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
                    _openInImageEditor = CreateOpenExternalIcon();
                }

                return _openInImageEditor;
            }
        }

        private static Texture2D CreateOpenExternalIcon()
        {
            const int size = 32;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = "Context Actions Slop - Open Externally",
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32 clear = new(0, 0, 0, 0);
            Color32 white = new(255, 255, 255, 255);
            Color32[] pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            void Pixel(int x, int y)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                    pixels[y * size + x] = white;
            }

            void ThickPixel(int x, int y)
            {
                Pixel(x, y);
                Pixel(x + 1, y);
                Pixel(x, y + 1);
                Pixel(x + 1, y + 1);
            }

            // Open square.
            for (int p = 6; p <= 23; p++)
            {
                ThickPixel(p, 6);
                ThickPixel(6, p);
            }
            for (int p = 6; p <= 18; p++)
            {
                ThickPixel(p, 23);
                ThickPixel(23, p);
            }

            // North-east arrow leaving the square.
            for (int p = 13; p <= 26; p++) ThickPixel(p, p);
            for (int p = 18; p <= 26; p++)
            {
                ThickPixel(p, 26);
                ThickPixel(26, p);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
