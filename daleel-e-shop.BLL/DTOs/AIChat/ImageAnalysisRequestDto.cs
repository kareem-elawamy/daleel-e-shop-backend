namespace daleel_e_shop.BLL.DTOs.AIChat
{
    public class ImageAnalysisRequestDto
    {
        /// <summary>
        /// Base64-encoded image (data:image/...;base64,... or raw base64).
        /// </summary>
        public string ImageBase64 { get; set; } = string.Empty;

        /// <summary>
        /// MIME type of the image, e.g. "image/jpeg", "image/png", "image/webp".
        /// </summary>
        public string MimeType { get; set; } = "image/jpeg";
    }
}
