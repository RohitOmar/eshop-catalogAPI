using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CatalogAPI.Helpers;
using CatalogAPI.Infrastructure;
using CatalogAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace CatalogAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAll")]
    [Authorize]
    public class CatalogController : ControllerBase
    {
        private CatalogContract db;
        IConfiguration configuration;
        public CatalogController(CatalogContract db,IConfiguration configuration)
        {
            this.db = db;
            this.configuration = configuration;
        }
        [HttpGet("", Name = "GetProducts")]
        [AllowAnonymous]
        public async Task<ActionResult<List<CatalogItem>>> GetProducts()
        {
            var result = await this.db.Catalog.FindAsync<CatalogItem>(FilterDefinition<CatalogItem>.Empty);
            return result.ToList();
        }

        [HttpPost("", Name = "AddProduct")]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Authorize(Roles ="admin")]
        public ActionResult<CatalogItem> AddProduct(CatalogItem item)
        {
            TryValidateModel(item);
            if (ModelState.IsValid)
            {
                this.db.Catalog.InsertOne(item);
                return Created("", item);
            }
            else
            {
                return BadRequest(ModelState);
            }

        }

        [HttpGet("{id}", Name = "FindById")]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [AllowAnonymous]
        public async Task<ActionResult<CatalogItem>> FindProductById(string id)
        {
            var builder = Builders<CatalogItem>.Filter;
            var filter = builder.Eq("Id", id);
            var result = await db.Catalog.FindAsync(filter);
            var item=result.FirstOrDefault();
            if (item == null)
            {
                return NotFound();
            }
            else
            { return Ok(item); }
        }

        [HttpPost("product")]
        [Authorize(Roles ="admin")]
        public ActionResult<CatalogItem> AddProductWithImage()
        {
            //var imageName = SaveImageToLocal(Request.Form.Files[0]);
            var imageName = SaveImageToCloud(Request.Form.Files[0]).GetAwaiter().GetResult();

            var catalogItem = new CatalogItem()
            {
                Name = Request.Form["name"],
                Price = Double.Parse(Request.Form["price"]),
                Quantity = Int32.Parse(Request.Form["quantity"]),
                ReorderLevel = Int32.Parse(Request.Form["reorderLevel"]),
                ManufacturingDate = DateTime.Parse(Request.Form["manufacturingDate"]),
                Vendors = new List<Vendor>(),
                ImageUrl = imageName
            };

            db.Catalog.InsertOne(catalogItem);
            BackupToTableAsync(catalogItem).GetAwaiter().GetResult();

           return catalogItem;
        }

        [NonAction]
        private string SaveImageToLocal(IFormFile image)
        {
            var imageName = $"{Guid.NewGuid()}_{image.FileName}";

            var dirName = Path.Combine(Directory.GetCurrentDirectory(), "Images");
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            var filepath = Path.Combine(dirName, imageName);
            using (FileStream fs = new FileStream(filepath, FileMode.Create))
            {
                image.CopyTo(fs);
            }
            return $"/Images/{imageName}";
        }

        [NonAction]
        private async Task<string> SaveImageToCloud(IFormFile image)
        {
            var imageName = $"{Guid.NewGuid()}_{image.FileName}";
            var tempFile = Path.GetTempFileName();
            using (FileStream fs = new FileStream(tempFile, FileMode.Create))
            {
                await image.CopyToAsync(fs);
            }
            var imageFile = Path.Combine(Path.GetDirectoryName(tempFile), imageName);
            System.IO.File.Move(tempFile, imageFile);
            StorageAccountHelper storageHelper = new StorageAccountHelper();
            storageHelper.StorageConnectionString = configuration.GetConnectionString("StorageConnection");
            var fileUri = await storageHelper.UploadFileToBlobAsync(imageFile, "eshopimages");
            return fileUri;

        }

        [NonAction]
        async Task<CatalogEntity> BackupToTableAsync(CatalogItem item)
        {
            StorageAccountHelper storagehelper = new StorageAccountHelper();
            //storagehelper.StorageConnectionString = configuration.GetConnectionString("StorageConnection");
            storagehelper.TableConnectionString = configuration.GetConnectionString("TableConnection");
            return await storagehelper.SaveToTableAsync(item);

        }
    }
}
