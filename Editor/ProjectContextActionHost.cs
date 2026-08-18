using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ContextActionsSlop.Editor
{
    /// <summary>
    /// Hosts independently registered controls on hovered Project-window folders and assets.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectContextActionHost
    {
        public const float DefaultButtonSize = 32f;
        public const float ButtonSpacing = 2f;
        public const float EdgePadding = 2f;

        private static readonly List<Registration> Registrations = new();
        private static bool _sortRequired;
        private static EditorWindow _projectWindow;
        private static string _hoveredGuid;

        static ProjectContextActionHost()
        {
            EditorApplication.projectWindowItemOnGUI += DrawProjectItem;
            EditorApplication.update += TrackProjectWindow;
        }

        /// <summary>
        /// Registers or replaces a drawer. Lower order values reserve the rightmost slots first.
        /// </summary>
        public static void Register(string id, Action<ProjectContextItem> drawer, int order = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A context action id is required.", nameof(id));
            if (drawer == null)
                throw new ArgumentNullException(nameof(drawer));

            int index = Registrations.FindIndex(item => item.Id == id);
            Registration registration = new(id, drawer, order);

            if (index >= 0) Registrations[index] = registration;
            else Registrations.Add(registration);

            _sortRequired = true;
            RepaintProjectWindow();
        }

        public static void Unregister(string id)
        {
            int removed = Registrations.RemoveAll(item => item.Id == id);
            if (removed > 0) RepaintProjectWindow();
        }

        private static void DrawProjectItem(string guid, Rect itemRect)
        {
            if (Registrations.Count == 0) return;

            ProjectContextItem item = new(guid, itemRect);
            if (!item.IsHovered) return;

            if (_hoveredGuid != guid)
            {
                _hoveredGuid = guid;
                RepaintProjectWindow();
            }

            if (_sortRequired)
            {
                Registrations.Sort((left, right) => left.Order.CompareTo(right.Order));
                _sortRequired = false;
            }

            foreach (Registration registration in Registrations)
            {
                try
                {
                    registration.Drawer(item);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private static void TrackProjectWindow()
        {
            EditorWindow hoveredWindow = EditorWindow.mouseOverWindow;
            if (hoveredWindow == null || hoveredWindow.GetType().Name != "ProjectBrowser") return;

            _projectWindow = hoveredWindow;
            if (!_projectWindow.wantsMouseMove) _projectWindow.wantsMouseMove = true;
        }

        private static void RepaintProjectWindow()
        {
            _projectWindow?.Repaint();
        }

        private readonly struct Registration
        {
            public string Id { get; }
            public Action<ProjectContextItem> Drawer { get; }
            public int Order { get; }

            public Registration(string id, Action<ProjectContextItem> drawer, int order)
            {
                Id = id;
                Drawer = drawer;
                Order = order;
            }
        }
    }
}
