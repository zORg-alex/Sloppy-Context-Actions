using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SloppyContextActions.Editor
{
    [Serializable]
    internal sealed class ImageEditorEntry
    {
        public string name;
        public string executablePath;
        public bool reuseRunningInstance;

        public ImageEditorEntry(
            string name,
            string executablePath,
            bool reuseRunningInstance = true)
        {
            this.name = name;
            this.executablePath = executablePath;
            this.reuseRunningInstance = reuseRunningInstance;
        }
    }

    [Serializable]
    internal sealed class ImageEditorCollection
    {
        public int settingsVersion = 2;
        public List<ImageEditorEntry> editors = new();
    }

    internal static class ImageEditorPreferences
    {
        private const string EditorPrefsKey = "SloppyContextActions.ImageEditors.v1";
        private const string LegacyEditorPrefsKey = "ContextActionsSlop.ImageEditors.v1";
        private const string PreferencesPath = "Preferences/Sloppy Context Actions";

        private static ImageEditorCollection _data;

        public static List<ImageEditorEntry> Editors
        {
            get
            {
                EnsureLoaded();
                return _data.editors;
            }
        }

        public static IEnumerable<ImageEditorEntry> ValidEditors =>
            Editors.Where(editor =>
                editor != null &&
                !string.IsNullOrWhiteSpace(editor.name) &&
                File.Exists(editor.executablePath));

        public static void OpenPreferences()
        {
            SettingsService.OpenUserPreferences(PreferencesPath);
        }

        public static void Save()
        {
            EnsureLoaded();
            EditorPrefs.SetString(EditorPrefsKey, JsonUtility.ToJson(_data));
        }

        public static int DiscoverInstalledEditors()
        {
            EnsureLoaded();
            int added = 0;

            foreach (ImageEditorEntry discovered in GetDiscoveredEditors())
            {
                bool exists = _data.editors.Any(editor =>
                    string.Equals(
                        editor.executablePath,
                        discovered.executablePath,
                        StringComparison.OrdinalIgnoreCase));

                if (exists) continue;
                _data.editors.Add(discovered);
                added++;
            }

            if (added > 0) Save();
            return added;
        }

        private static void EnsureLoaded()
        {
            if (_data != null) return;

            MigrateLegacyPreference();
            string json = EditorPrefs.GetString(EditorPrefsKey, string.Empty);
            _data = string.IsNullOrEmpty(json)
                ? new ImageEditorCollection()
                : JsonUtility.FromJson<ImageEditorCollection>(json);

            _data ??= new ImageEditorCollection();
            _data.editors ??= new List<ImageEditorEntry>();

            if (_data.settingsVersion < 2)
            {
                foreach (ImageEditorEntry editor in _data.editors)
                {
                    if (editor != null) editor.reuseRunningInstance = true;
                }

                _data.settingsVersion = 2;
                Save();
            }

            if (!EditorPrefs.HasKey(EditorPrefsKey))
            {
                DiscoverInstalledEditors();
                Save();
            }
        }

        private static void MigrateLegacyPreference()
        {
            if (EditorPrefs.HasKey(EditorPrefsKey) || !EditorPrefs.HasKey(LegacyEditorPrefsKey)) return;
            EditorPrefs.SetString(EditorPrefsKey, EditorPrefs.GetString(LegacyEditorPrefsKey, string.Empty));
        }

        private static IEnumerable<ImageEditorEntry> GetDiscoveredEditors()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            foreach (string photoshop in FindPhotoshopExecutables(programFiles))
                yield return new ImageEditorEntry("Photoshop", photoshop);

            string[] candidates =
            {
                Path.Combine(programFiles, "Krita (x64)", "bin", "krita.exe"),
                Path.Combine(programFiles, "Krita", "bin", "krita.exe"),
                Path.Combine(programFilesX86, "Steam", "steamapps", "common", "Aseprite", "Aseprite.exe"),
                Path.Combine(programFiles, "Steam", "steamapps", "common", "Aseprite", "Aseprite.exe"),
                Path.Combine(localAppData, "Programs", "Aseprite", "Aseprite.exe")
            };

            foreach (string candidate in candidates)
            {
                if (!File.Exists(candidate)) continue;
                string name = Path.GetFileNameWithoutExtension(candidate);
                if (name.Equals("krita", StringComparison.OrdinalIgnoreCase)) name = "Krita";
                else if (name.Equals("aseprite", StringComparison.OrdinalIgnoreCase)) name = "Aseprite";
                yield return new ImageEditorEntry(name, candidate);
            }
        }

        private static IEnumerable<string> FindPhotoshopExecutables(string programFiles)
        {
            string adobeDirectory = Path.Combine(programFiles, "Adobe");
            if (!Directory.Exists(adobeDirectory)) yield break;

            foreach (string directory in Directory.EnumerateDirectories(adobeDirectory, "Adobe Photoshop *"))
            {
                string executable = Path.Combine(directory, "Photoshop.exe");
                if (File.Exists(executable)) yield return executable;
            }
        }
    }
}
