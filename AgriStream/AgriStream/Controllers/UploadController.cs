using AgriStream.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgriStream.Controllers
{
    public class UploadController : Controller
    {
        private readonly BlobStorageService _blobStorage;

        public UploadController(BlobStorageService blobStorage)
        {
            _blobStorage = blobStorage;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Message = "Please select an image.";
                return View("Index");
            }

            string fileName = await _blobStorage.UploadImageAsync(file);

            ViewBag.Message = "Image uploaded successfully: " + fileName;

            return View("Index");
        }
    }
}
