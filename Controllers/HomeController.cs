using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using QR_Bar_Generator_App.Models;
using QR_Bar_Generator_App.Models.ViewModels;
using System.Diagnostics;
using ZXing.QrCode;

namespace QR_Bar_Generator_App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(InputFormViewModel model)
        {
            if (ModelState.IsValid) 
            {
                if(model.codeType.ToString() == "qr")
                {
                    //qr code session
                    var width = 150; // width of the QR Code
                    var height = 150; // height of the QR Code
                    var margin = 0;
                    // BarcodeWriterPixelData acts as a QR code generator
                    var qrCodeWriter = new ZXing.BarcodeWriterPixelData
                    {
                        Format = ZXing.BarcodeFormat.QR_CODE,
                        Options = new QrCodeEncodingOptions
                        {
                            Height = height,
                            Width = width,
                            Margin = margin
                        }
                    };
                    var pixelData = qrCodeWriter.Write(model.inputData);
                    // creating a PNG bitmap from the raw pixel data; if only black and white colors are used it makes no difference if the raw pixel data is BGRA oriented and the bitmap is initialized with RGB
                    using (var bitmap = new System.Drawing.Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
                    {
                        using (var ms = new MemoryStream())
                        {
                            var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, pixelData.Width, pixelData.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                            try
                            {
                                // we assume that the row stride of the bitmap is aligned to 4 byte multiplied by the width of the image
                                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                            }
                            finally
                            {
                                bitmap.UnlockBits(bitmapData);
                            }

                            // Generate unique filename
                            string fileGuid = Guid.NewGuid().ToString();
                            string folderPath = Path.Combine(_env.WebRootPath, "assets", "qr");
                            string filePath = Path.Combine(folderPath, $"file-{fileGuid}.png");

                            // Ensure folder exists
                            if (!Directory.Exists(folderPath))
                                Directory.CreateDirectory(folderPath);

                            // Save as PNG
                            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

                            //update image
                            model.imageData = $"/assets/qr/file-{fileGuid}.png";
                        }
                    }
                }
                else
                {
                    //bar code session
                }
            }

            return View(model);
        }

        public IActionResult FormDetail()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
