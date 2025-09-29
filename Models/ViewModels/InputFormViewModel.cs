using System.ComponentModel.DataAnnotations;

namespace QR_Bar_Generator_App.Models.ViewModels
{
    public enum CodeType
    {
        [Display(Name = "QR Code")]
        qr,

        [Display(Name = "Bar Code")]
        bar
    }

    public class InputFormViewModel
    {
        [Required]
        public CodeType? codeType { get; set; } // qr or bar

        [Required]
        public string? inputData { get; set; }

        public string? imageData { get; set; } = "";
    }
}
