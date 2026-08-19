using UnityEditor;
using UnityEngine;

namespace SloppyContextActions.Editor
{
    public enum ProjectContextSurface
    {
        AssetList,
        TreeFolder,
        CurrentFolder
    }

    /// <summary>Information and layout state for one item drawn in the Project window.</summary>
    public sealed class ProjectContextItem
    {
        private Object _asset;
        private bool _assetLoaded;
        private float _rightEdge;
        private float _leftEdge;
        private readonly bool _layoutFromLeft;

        public string Guid { get; }
        public string Path { get; }
        public Rect ItemRect { get; }
        public bool IsHovered { get; }
        public bool IsFolder { get; }
        public ProjectContextSurface Surface { get; }

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

        internal ProjectContextItem(
            string guid,
            Rect itemRect,
            bool layoutFromLeft = false,
            ProjectContextSurface surface = ProjectContextSurface.AssetList)
        {
            Guid = guid;
            Path = AssetDatabase.GUIDToAssetPath(guid);
            ItemRect = itemRect;
            IsHovered = itemRect.Contains(Event.current.mousePosition);
            IsFolder = !string.IsNullOrEmpty(Path) && AssetDatabase.IsValidFolder(Path);
            Surface = surface;
            _rightEdge = itemRect.xMax - ProjectContextActionHost.EdgePadding;
            _leftEdge = itemRect.xMin;
            _layoutFromLeft = layoutFromLeft;
        }

        /// <summary>
        /// Reserves a top-right slot. Calling this repeatedly lays buttons out from right to left.
        /// </summary>
        public Rect ReserveButtonRect(float width = -1f)
        {
            float buttonSize = ContextActionPreferences.ButtonSize;
            if (width < 0f) width = buttonSize;
            float height = Mathf.Min(buttonSize, ItemRect.height);

            if (_layoutFromLeft)
            {
                Rect leftResult = new Rect(_leftEdge, ItemRect.yMin, width, height);
                _leftEdge += width + ProjectContextActionHost.ButtonSpacing;
                return leftResult;
            }

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
