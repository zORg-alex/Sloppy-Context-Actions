using UnityEditor;
using UnityEngine;

namespace SloppyContextActions.Editor
{
    internal static class ContextActionPreferences
    {
        private const string ButtonSizeKey = "SloppyContextActions.ButtonSize.v1";
        private const string LegacyButtonSizeKey = "ContextActionsSlop.ButtonSize.v1";

        public const int MinimumButtonSize = 12;
        public const int MaximumButtonSize = 40;
        public const int DefaultButtonSize = 16;

        public static int ButtonSize
        {
            get
            {
                MigrateLegacyPreference();
                return Mathf.Clamp(
                    EditorPrefs.GetInt(ButtonSizeKey, DefaultButtonSize),
                    MinimumButtonSize,
                    MaximumButtonSize);
            }
            set => EditorPrefs.SetInt(
                ButtonSizeKey,
                Mathf.Clamp(value, MinimumButtonSize, MaximumButtonSize));
        }

        private static void MigrateLegacyPreference()
        {
            if (EditorPrefs.HasKey(ButtonSizeKey) || !EditorPrefs.HasKey(LegacyButtonSizeKey)) return;
            EditorPrefs.SetInt(ButtonSizeKey, EditorPrefs.GetInt(LegacyButtonSizeKey, DefaultButtonSize));
        }
    }
}
