using System.ComponentModel.DataAnnotations;

namespace QR_Bar_Generator_App.Models.ViewModels
{
    public class UploadFormViewModel
    {
        [Required]
        public CodeType? codeType { get; set; }
        [Required]
        public IFormFile? uploadFile { get; set; }
        public string? decodedText { get; set; }
    }
}
