namespace Moba.TrackPlan.Renderer
{
    /// <summary>
    /// Simple model representing rendered track plan dimensions and zoom factor.
    /// Used by renderers such as <see cref="SvgExporter"/>.
    /// </summary>
    public class TrackPlan
    {
        /// <summary>
        /// Gets or sets the height of the rendered track plan in pixels.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the width of the rendered track plan in pixels.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the zoom factor (percentage) applied to the rendered plan.
        /// </summary>
        public int ZoomFactor { get; set; }
    }
}