using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;
using upload_download.Data;

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
        public List<string> Files { get; set; } = new List<string>();

        public UploadsModel(ILogger<UploadsModel> logger, IWebHostEnvironment environment, ApplicationDbContext context)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
        }

        public void OnGet()
        {
            Files = _context.StoredFiles.Select(f => f.OriginalName).ToList();
        }

        public IActionResult OnGetDownload(string filename)
        {
            // Retrieve the file based on the original name provided.
            var file = _context.StoredFiles.FirstOrDefault(f => f.OriginalName == filename);

            if (file == null)
            {
                ErrorMessage = "There is no such file.";
                return RedirectToPage();
            }

            // Construct the full path using the file's ID.
            var fullName = Path.Combine(_environment.ContentRootPath, "Uploads", file.Id.ToString());

            if (System.IO.File.Exists(fullName))
            {
                // Return the file to the client with the original filename for download.
                return PhysicalFile(fullName, MediaTypeNames.Application.Octet, file.OriginalName);
            }
            else
            {
                ErrorMessage = "File not found on server.";
                return RedirectToPage();
            }
        }

    }

}
