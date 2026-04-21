using DotNetNuke.Web.Api;
using System;
using System.IO;
using System.Web;
using DNNHello.DNNHello.Models;
using DNNHello.DNNHello.Components;

namespace DNNHello.DNNHello.Controllers
{
    public class GalleryApiController : DnnApiController
    {
        [System.Web.Http.HttpPost]
        [DnnAuthorize]
        public System.Web.Http.IHttpActionResult Upload()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                var itemName = httpRequest.Form["ItemName"];
                var moduleIdStr = httpRequest.Form["ModuleId"];
                var file = httpRequest.Files.Count > 0 ? httpRequest.Files[0] : null;

                if (file == null || file.ContentLength == 0 || string.IsNullOrEmpty(itemName) || string.IsNullOrEmpty(moduleIdStr))
                {
                    return BadRequest("Hiányzó adatok.");
                }

                int moduleId = int.Parse(moduleIdStr);
                int userId = UserInfo.UserID;

                // Fájl mentése
                var fileName = Path.GetFileName(file.FileName);
                var folderPath = HttpContext.Current.Server.MapPath("~/Portals/0/Gallery/");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                // Egyedi fájlnév ütközés elkerülésére
                var uniqueName = DateTime.UtcNow.Ticks + "_" + fileName;
                var path = Path.Combine(folderPath, uniqueName);
                file.SaveAs(path);

                // Adatbázisba mentés
                var item = new Item
                {
                    ModuleId = moduleId,
                    ItemName = itemName,
                    ItemDescription = "",
                    ImagePath = "/Portals/0/Gallery/" + uniqueName,
                    CreatedByUserId = userId,
                    CreatedOnDate = DateTime.UtcNow,
                    LastModifiedByUserId = userId,
                    LastModifiedOnDate = DateTime.UtcNow
                };

                ItemManager.Instance.CreateItem(item);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}