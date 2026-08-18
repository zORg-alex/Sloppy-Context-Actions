using UnityEditor;
using UnityEngine;

namespace ContextActionsSlop.Editor
{
    internal static class ContextActionPreferences
    {
        private const string ButtonSizeKey = "ContextActionsSlop.ButtonSize.v1";

        public const int MinimumButtonSize = 12;
        public const int MaximumButtonSize = 40;
        public const int DefaultButtonSize = 24;

        public static int ButtonSize
        {
            get => Mathf.Clamp(
                EditorPrefs.GetInt(ButtonSizeKey, DefaultButtonSize),
                MinimumButtonSize,
                MaximumButtonSize);
            set => EditorPrefs.SetInt(
                ButtonSizeKey,
                Mathf.Clamp(value, MinimumButtonSize, MaximumButtonSize));
        }
    }
}
