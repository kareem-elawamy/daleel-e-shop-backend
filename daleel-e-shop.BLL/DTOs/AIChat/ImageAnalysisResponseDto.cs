namespace daleel_e_shop.BLL.DTOs.AIChat
{
    public class ImageAnalysisResponseDto
    {
        public bool Success { get; set; }
        public string? Error { get; set; }

        /// <summary>
        /// Detected product category, e.g. "Electronics", "Fashion", "Beauty &amp; Oud".
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// 3-8 short search keywords describing the detected object.
        /// </summary>
        public List<string> Keywords { get; set; } = new();

        /// <summary>
        /// Dominant color detected in the image.
        /// </summary>
        public string? DominantColor { get; set; }

        /// <summary>
        /// One-sentence description of the product.
        /// </summary>
        public string? Description { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
