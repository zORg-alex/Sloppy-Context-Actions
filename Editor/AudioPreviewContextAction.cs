using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SloppyContextActions.Editor
{
    [InitializeOnLoad]
    internal static class AudioPreviewContextAction
    {
        private const string RegistrationId = "sloppy-context-actions.audio-preview";
        private static AudioClip _activeClip;

        static AudioPreviewContextAction()
        {
            ProjectContextActionHost.Register(RegistrationId, Draw, order: 70);
            EditorApplication.update += TrackPlayback;
        }

        private static void Draw(ProjectContextItem item)
        {
            if (item.IsFolder || item.Asset is not AudioClip clip) return;

            bool isPlaying = _activeClip == clip && AudioPreviewUtility.IsPlaying;
            if (_activeClip == clip && !isPlaying) _activeClip = null;

            Rect buttonRect = item.ReserveButtonRect();
            GUIContent content = new()
            {
                image = isPlaying
                    ? ContextActionIcons.AudioStop
                    : ContextActionIcons.AudioPlay,
                tooltip = isPlaying ? "Stop Audio Preview" : "Play Audio Preview"
            };

            if (ProjectContextButton.Draw(buttonRect, content) != ProjectContextButtonClick.Left)
                return;

            if (isPlaying)
            {
                AudioPreviewUtility.StopAll();
                _activeClip = null;
            }
            else
            {
                AudioPreviewUtility.StopAll();
                if (AudioPreviewUtility.Play(clip)) _activeClip = clip;
            }

            ProjectContextActionHost.RepaintProjectWindow();
        }

        private static void TrackPlayback()
        {
            if (_activeClip == null || AudioPreviewUtility.IsPlaying) return;

            _activeClip = null;
            ProjectContextActionHost.RepaintProjectWindow();
        }
    }

    internal static class AudioPreviewUtility
    {
        private const BindingFlags StaticMembers =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Type AudioUtilType =
            typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AudioUtil");
        private static readonly MethodInfo PlayMethod = AudioUtilType?.GetMethod(
            "PlayPreviewClip",
            StaticMembers,
            null,
            new[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null);
        private static readonly MethodInfo StopAllMethod =
            AudioUtilType?.GetMethod("StopAllPreviewClips", StaticMembers);
        private static readonly MethodInfo IsPlayingMethod =
            AudioUtilType?.GetMethod("IsPreviewClipPlaying", StaticMembers);
        private static bool _missingApiReported;

        public static bool IsPlaying
        {
            get
            {
                if (!EnsureAvailable()) return false;

                try
                {
                    return (bool)IsPlayingMethod.Invoke(null, Array.Empty<object>());
                }
                catch (Exception exception)
                {
                    ReportInvocationFailure(exception);
                    return false;
                }
            }
        }

        public static bool Play(AudioClip clip)
        {
            if (!EnsureAvailable()) return false;

            try
            {
                PlayMethod.Invoke(null, new object[] { clip, 0, false });
                return true;
            }
            catch (Exception exception)
            {
                ReportInvocationFailure(exception);
                return false;
            }
        }

        public static void StopAll()
        {
            if (!EnsureAvailable()) return;

            try
            {
                StopAllMethod.Invoke(null, Array.Empty<object>());
            }
            catch (Exception exception)
            {
                ReportInvocationFailure(exception);
            }
        }

        private static bool EnsureAvailable()
        {
            if (PlayMethod != null && StopAllMethod != null && IsPlayingMethod != null)
                return true;
            if (_missingApiReported) return false;

            _missingApiReported = true;
            Debug.LogError(
                "Sloppy Context Actions could not locate Unity's internal AudioUtil preview API. " +
                "Audio preview is unavailable in this Unity version.");
            return false;
        }

        private static void ReportInvocationFailure(Exception exception)
        {
            if (_missingApiReported) return;

            _missingApiReported = true;
            Debug.LogError(
                "Sloppy Context Actions failed to invoke Unity's AudioUtil preview API.\n" +
                exception);
        }
    }
}
