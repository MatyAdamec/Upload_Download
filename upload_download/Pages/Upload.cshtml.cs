using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.AspNetCore.Authorization;
using System.Drawing.Imaging;
using upload_download.Data;
using upload_download.Models;

namespace upload_download.Pages
{
    [Authorize]
    public class UploadModel : PageModel
    {
        private IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private int _squareSize;
        private int _sameAspectRatioHeigth;

        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }

        public ICollection<IFormFile> Upload { get; set; }

        public UploadModel(IWebHostEnvironment environment, ApplicationDbContext context, IConfiguration configuration)
        {
            _environment = environment;
            _context = context;
            _configuration = configuration;
            if (Int32.TryParse(_configuration["Thumbnails:SquareSize"], out _squareSize) == false) _squareSize = 64; // získej data z konfigurave nebo použij 64
            if (Int32.TryParse(_configuration["Thumbnails:SameAspectRatioHeigth"], out _sameAspectRatioHeigth) == false) _sameAspectRatioHeigth = 128;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).First().Value;
            int successfulProcessing = 0;
            int failedProcessing = 0;
            foreach (var uploadedFile in Upload)
            {
                var fileRecord = new StoredFile
                {
                    UploaderId = userId,
                    UploadedAt = DateTime.Now,
                    OriginalName = uploadedFile.FileName,
                    ContentType = uploadedFile.ContentType
                };
                if (uploadedFile.ContentType.StartsWith("image")) // je soubor obrázek?
                {
                    fileRecord.Thumbnails = new List<Thumbnail>();
                    MemoryStream ims = new MemoryStream(); // proud pro pøíchozí obrázek
                    MemoryStream oms1 = new MemoryStream(); // proud pro ètvercový náhled
                    MemoryStream oms2 = new MemoryStream(); // proud pro obdélníkový náhled
                    uploadedFile.CopyTo(ims); // vlož obsah do vstupního proudu
                    IImageFormat format; // zde si uložíme formát obrázku (JPEG, GIF, ...), budeme ho potøebovat pøi ukládání
                    using (SixLabors.ImageSharp.Image image = Image.Load(ims.ToArray())) // vytvoøíme ètvercový náhled
                    {
                        int largestSize = Math.Max(image.Height, image.Width); // jaká je orientace obrázku?
                        if (image.Width > image.Height) // podle orientace zmìníme velikost obrázku
                        {
                            image.Mutate(x => x.Resize(0, _squareSize));
                        }
                        else
                        {
                            image.Mutate(x => x.Resize(_squareSize, 0));
                        }
                        image.Mutate(x => x.Crop(new Rectangle((image.Width - _squareSize) / 2, (image.Height - _squareSize) / 2, _squareSize, _squareSize)));
                        // obrázek oøízneme na ètverec
                        image.SaveAsJpeg(oms1); // vložíme ho do výstupního proudu
                        fileRecord.Thumbnails.Add(new Thumbnail { Type = ThumbnailType.Square, Blob = oms1.ToArray() }); // a uložíme do databáze jako pole bytù
                    }
                    using (Image image = Image.Load(ims.ToArray())) // obdélníkový náhled zaèíná zde
                    {
                        image.Mutate(x => x.Resize(0, _sameAspectRatioHeigth)); // staèí jen zmìnit jeho velikost
                        image.SaveAsJpeg(oms2); // a pøes proud ho uložit do databáze
                        fileRecord.Thumbnails.Add(new Thumbnail { Type = ThumbnailType.SameAspectRatio, Blob = oms2.ToArray() });
                    }
                }

                try
                {
                    _context.StoredFiles.Add(fileRecord);
                    await _context.SaveChangesAsync();

                    var file = Path.Combine(_environment.ContentRootPath, "Uploads", fileRecord.Id.ToString());
                    using (var fileStream = new FileStream(file, FileMode.Create))
                    {
                        await uploadedFile.CopyToAsync(fileStream);
                    };
                    successfulProcessing++;
                }
                catch
                {
                    failedProcessing++;
                }
            }
            if (failedProcessing == 0)
            {
                SuccessMessage = "All files has been uploaded successfuly.";
            }
            else
            {
                ErrorMessage = "There were <b>" + failedProcessing + "</b> errors during uploading and processing of files.";
            }
            return RedirectToPage("/Index");
        }

    }
}
