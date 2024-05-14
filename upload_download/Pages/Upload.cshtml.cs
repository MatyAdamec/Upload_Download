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
            if (Int32.TryParse(_configuration["Thumbnails:SquareSize"], out _squareSize) == false) _squareSize = 64; // z�skej data z konfigurave nebo pou�ij 64
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
                if (uploadedFile.ContentType.StartsWith("image")) // je soubor obr�zek?
                {
                    fileRecord.Thumbnails = new List<Thumbnail>();
                    MemoryStream ims = new MemoryStream(); // proud pro p��choz� obr�zek
                    MemoryStream oms1 = new MemoryStream(); // proud pro �tvercov� n�hled
                    MemoryStream oms2 = new MemoryStream(); // proud pro obd�ln�kov� n�hled
                    uploadedFile.CopyTo(ims); // vlo� obsah do vstupn�ho proudu
                    IImageFormat format; // zde si ulo��me form�t obr�zku (JPEG, GIF, ...), budeme ho pot�ebovat p�i ukl�d�n�
                    using (SixLabors.ImageSharp.Image image = Image.Load(ims.ToArray())) // vytvo��me �tvercov� n�hled
                    {
                        int largestSize = Math.Max(image.Height, image.Width); // jak� je orientace obr�zku?
                        if (image.Width > image.Height) // podle orientace zm�n�me velikost obr�zku
                        {
                            image.Mutate(x => x.Resize(0, _squareSize));
                        }
                        else
                        {
                            image.Mutate(x => x.Resize(_squareSize, 0));
                        }
                        image.Mutate(x => x.Crop(new Rectangle((image.Width - _squareSize) / 2, (image.Height - _squareSize) / 2, _squareSize, _squareSize)));
                        // obr�zek o��zneme na �tverec
                        image.SaveAsJpeg(oms1); // vlo��me ho do v�stupn�ho proudu
                        fileRecord.Thumbnails.Add(new Thumbnail { Type = ThumbnailType.Square, Blob = oms1.ToArray() }); // a ulo��me do datab�ze jako pole byt�
                    }
                    using (Image image = Image.Load(ims.ToArray())) // obd�ln�kov� n�hled za��n� zde
                    {
                        image.Mutate(x => x.Resize(0, _sameAspectRatioHeigth)); // sta�� jen zm�nit jeho velikost
                        image.SaveAsJpeg(oms2); // a p�es proud ho ulo�it do datab�ze
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
