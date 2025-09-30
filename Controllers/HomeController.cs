using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using QR_Bar_Generator_App.Models;
using QR_Bar_Generator_App.Models.ViewModels;
using System.Diagnostics;
using System.Net;
using ZXing;
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
            //user ip session
            string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            // If it's IPv6 localhost, convert to IPv4
            if (ipAddress == "::1")
            {
                ipAddress = Dns.GetHostEntry(Dns.GetHostName())
                               .AddressList
                               .FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
                               .ToString();
            }

            if (ModelState.IsValid) 
            {
                if (model.codeType.ToString()!.ToLower() == "qr")
                {
                    // ----------------- QR Code Session -----------------
                    var width = 150; // width of the QR Code
                    var height = 150; // height of the QR Code
                    var margin = 0;

                    var qrCodeWriter = new ZXing.BarcodeWriterPixelData
                    {
                        Format = ZXing.BarcodeFormat.QR_CODE,
                        Options = new ZXing.QrCode.QrCodeEncodingOptions
                        {
                            Height = height,
                            Width = width,
                            Margin = margin
                        }
                    };

                    var pixelData = qrCodeWriter.Write(model.inputData);

                    using (var bitmap = new System.Drawing.Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
                    {
                        using (var ms = new MemoryStream())
                        {
                            var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, pixelData.Width, pixelData.Height),
                                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                System.Drawing.Imaging.PixelFormat.Format32bppRgb);

                            try
                            {
                                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                            }
                            finally
                            {
                                bitmap.UnlockBits(bitmapData);
                            }

                            string folderPath = Path.Combine(_env.WebRootPath, "assets", "qr");
                            string filePath = Path.Combine(folderPath, $"{ipAddress}.png");

                            if (!Directory.Exists(folderPath))
                                Directory.CreateDirectory(folderPath);

                            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                            model.imageData = $"/assets/qr/{ipAddress}.png";
                        }
                    }
                }
                else
                {
                    // ----------------- Barcode Session -----------------
                    var barcodeWriter = new ZXing.BarcodeWriterPixelData
                    {
                        Format = ZXing.BarcodeFormat.CODE_128, // CODE_128 Format only
                        Options = new ZXing.Common.EncodingOptions
                        {
                            Height = 80,   // barcode height
                            Width = 250,   // barcode width
                            Margin = 2
                        }
                    };

                    var pixelData = barcodeWriter.Write(model.inputData);

                    using (var bitmap = new System.Drawing.Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
                    {
                        using (var ms = new MemoryStream())
                        {
                            var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, pixelData.Width, pixelData.Height),
                                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                System.Drawing.Imaging.PixelFormat.Format32bppRgb);

                            try
                            {
                                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                            }
                            finally
                            {
                                bitmap.UnlockBits(bitmapData);
                            }

                            string folderPath = Path.Combine(_env.WebRootPath, "assets", "bar");
                            string filePath = Path.Combine(folderPath, $"{ipAddress}.png");

                            if (!Directory.Exists(folderPath))
                                Directory.CreateDirectory(folderPath);

                            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                            model.imageData = $"/assets/bar/{ipAddress}.png";
                        }
                    }
                }
            }

            return View(model);
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadFormViewModel model)
        {
            //user ip session
            string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            // If it's IPv6 localhost, convert to IPv4
            if (ipAddress == "::1")
            {
                ipAddress = Dns.GetHostEntry(Dns.GetHostName())
                               .AddressList
                               .FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
                               .ToString();
            }

            if (model.uploadFile != null && model.uploadFile.Length > 0)
            {
                // Save uploaded image
                string folderPath = Path.Combine(_env.WebRootPath, "assets", "upload");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string filePath = Path.Combine(folderPath, Path.GetFileName(model.uploadFile.FileName));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.uploadFile.CopyToAsync(stream);
                }

                // Decode
                var barcodeReader = new BarcodeReaderGeneric
                {
                    AutoRotate = true,
                    Options = new ZXing.Common.DecodingOptions
                    {
                        TryHarder = true,
                        PossibleFormats = model.codeType.ToString()!.ToLower() == "qr"
                            ? new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
                            : new List<BarcodeFormat> { BarcodeFormat.CODE_128, BarcodeFormat.CODE_39, BarcodeFormat.EAN_13 }
                    }
                };

                using (var bitmap = new System.Drawing.Bitmap(filePath))
                {
                    var result = barcodeReader.Decode(bitmap);
                    model.decodedText = result?.Text ?? "Could not decode the image.";
                }

                // Delete temp file after decoding
                //System.IO.File.Delete(filePath);
            }
            else
            {
                ModelState.AddModelError("", "Please upload an image file.");
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
