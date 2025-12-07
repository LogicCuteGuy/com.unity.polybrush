using UnityEngine;

namespace UnityEditor.Polybrush
{
    /// <summary>
    /// Settings for image-based brush functionality.
    /// Allows using grayscale textures to define brush shape and intensity distribution.
    /// </summary>
    [System.Serializable]
    internal class ImageBrushSettings
    {
        /// <summary>
        /// The grayscale texture used to define brush shape and intensity.
        /// Lighter values in the texture result in stronger brush influence.
        /// </summary>
        [SerializeField]
        private Texture2D _brushTexture;

        /// <summary>
        /// Rotation angle of the brush texture in degrees (0-360).
        /// </summary>
        [SerializeField]
        private float _rotation = 0f;

        /// <summary>
        /// Whether to preserve the aspect ratio of non-square textures.
        /// When true, the texture will maintain its original proportions in world space.
        /// </summary>
        [SerializeField]
        private bool _preserveAspectRatio = true;

        /// <summary>
        /// Sampling mode for texture filtering.
        /// </summary>
        [SerializeField]
        private FilterMode _samplingMode = FilterMode.Bilinear;

        /// <summary>
        /// Whether image brush mode is currently enabled.
        /// </summary>
        [SerializeField]
        private bool _enabled = false;

        /// <summary>
        /// Gets or sets the brush texture.
        /// </summary>
        internal Texture2D brushTexture
        {
            get { return _brushTexture; }
            set { _brushTexture = value; }
        }

        /// <summary>
        /// Gets or sets the rotation angle in degrees (0-360).
        /// </summary>
        internal float rotation
        {
            get { return _rotation; }
            set { _rotation = Mathf.Repeat(value, 360f); }
        }

        /// <summary>
        /// Gets or sets whether to preserve aspect ratio.
        /// </summary>
        internal bool preserveAspectRatio
        {
            get { return _preserveAspectRatio; }
            set { _preserveAspectRatio = value; }
        }

        /// <summary>
        /// Gets or sets the texture sampling mode.
        /// </summary>
        internal FilterMode samplingMode
        {
            get { return _samplingMode; }
            set { _samplingMode = value; }
        }

        /// <summary>
        /// Gets or sets whether image brush mode is enabled.
        /// </summary>
        internal bool enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        /// <summary>
        /// Returns true if the image brush is valid and ready to use.
        /// </summary>
        internal bool IsValid()
        {
            return _enabled && _brushTexture != null && _brushTexture.isReadable;
        }

        /// <summary>
        /// Creates a new ImageBrushSettings with default values.
        /// </summary>
        internal ImageBrushSettings()
        {
            _brushTexture = null;
            _rotation = 0f;
            _preserveAspectRatio = true;
            _samplingMode = FilterMode.Bilinear;
            _enabled = false;
        }

        /// <summary>
        /// Deep copy this ImageBrushSettings.
        /// </summary>
        internal ImageBrushSettings DeepCopy()
        {
            ImageBrushSettings copy = new ImageBrushSettings();
            CopyTo(copy);
            return copy;
        }

        /// <summary>
        /// Copy all properties to target.
        /// </summary>
        internal void CopyTo(ImageBrushSettings target)
        {
            target._brushTexture = this._brushTexture;
            target._rotation = this._rotation;
            target._preserveAspectRatio = this._preserveAspectRatio;
            target._samplingMode = this._samplingMode;
            target._enabled = this._enabled;
        }
    }
}
