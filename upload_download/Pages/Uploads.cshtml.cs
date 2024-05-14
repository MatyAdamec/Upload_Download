using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;
using upload_download.Data;
using upload_download.Models;

namespace upload_download.Pages
{
    public class UploadsModel : PageModel
    {
        private IWebHostEnvironment _environment;
        private readonly ILogger<UploadsModel> _logger;
        private readonly ApplicationDbContext _context;

        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        public List<StoredFile> Files { get; set; } = new List<StoredFile>();

        public UploadsModel(ILogger<UploadsModel> logger, IWebHostEnvironment environment, ApplicationDbContext context)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
        }

        public void OnGet()
        {
            Files = _context.StoredFiles.ToList();
        }

        public IActionResult OnGetDownload(Guid fileid)
        {
            var file = _context.StoredFiles.FirstOrDefault(f => f.Id == fileid);

            if (file == null)
            {
                Console.WriteLine("NOT FOUND");
                return NotFound();
            }

            var fullName = Path.Combine(_environment.ContentRootPath, "Uploads", file.Id.ToString());

            if (System.IO.File.Exists(fullName))
            {
                return PhysicalFile(fullName, MediaTypeNames.Application.Octet, file.OriginalName);
            }
            else
            {
                ErrorMessage = "File not found on server.";
                return RedirectToPage();
            }
        }

        public IActionResult OnGetImage(Guid id)
        {
            _logger.LogInformation($"my fucking fileid: {id}");
            var file = _context.StoredFiles.FirstOrDefault(f => f.Id == id);
            if (file == null)
            {
                return NotFound();
            }

            var path = Path.Combine(_environment.ContentRootPath, "Uploads", file.Id.ToString());
            _logger.LogInformation($"my fucking path: {path}");

            if (System.IO.File.Exists(path))
            {
                return PhysicalFile(path, file.ContentType);
            }
            else
            {
                return NotFound();
            }
        }

    }

}
