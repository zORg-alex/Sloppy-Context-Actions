using UnityEditor;
using UnityEngine;

namespace ContextActionsSlop.Editor
{
    /// <summary>Information and layout state for one item drawn in the Project window.</summary>
    public sealed class ProjectContextItem
    {
        private Object _asset;
        private bool _assetLoaded;
        private float _rightEdge;

        public string Guid { get; }
        public string Path { get; }
        public Rect ItemRect { get; }
        public bool IsHovered { get; }
        public bool IsFolder { get; }

        public Object Asset
        {
            get
            {
                if (!_assetLoaded)
                {
                    _asset = AssetDatabase.LoadMainAssetAtPath(Path);
                    _assetLoaded = true;
                }

                return _asset;
            }
        }

        internal ProjectContextItem(string guid, Rect itemRect)
        {
            Guid = guid;
            Path = AssetDatabase.GUIDToAssetPath(guid);
            ItemRect = itemRect;
            IsHovered = itemRect.Contains(Event.current.mousePosition);
            IsFolder = !string.IsNullOrEmpty(Path) && AssetDatabase.IsValidFolder(Path);
            _rightEdge = itemRect.xMax - ProjectContextActionHost.EdgePadding;
        }

        /// <summary>
        /// Reserves a top-right slot. Calling this repeatedly lays buttons out from right to left.
        /// </summary>
        public Rect ReserveButtonRect(float width = -1f)
        {
            float buttonSize = ContextActionPreferences.ButtonSize;
            if (width < 0f) width = buttonSize;
            float height = Mathf.Min(buttonSize, ItemRect.height);
            _rightEdge -= width;

            Rect result = new Rect(
                _rightEdge,
                ItemRect.yMin,
                width,
                height);

            _rightEdge -= ProjectContextActionHost.ButtonSpacing;
            return result;
        }
    }
}
