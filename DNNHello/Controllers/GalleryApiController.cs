using DotNetNuke.Web.Api;
using System;
using System.IO;
using System.Linq;
using System.Web;
using DNNHello.DNNHello.Models;
using DNNHello.DNNHello.Components;

namespace DNNHello.DNNHello.Controllers
{
    public class GalleryApiController : DnnApiController
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

        // ── Feltöltés ──
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

                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (ext == null || !AllowedExtensions.Contains(ext))
                {
                    return BadRequest("Csak képfájlok engedélyezettek (jpg, png, gif, bmp, webp).");
                }

                int moduleId = int.Parse(moduleIdStr);
                int userId = UserInfo.UserID;

                var fileName = Path.GetFileName(file.FileName);
                var folderPath = HttpContext.Current.Server.MapPath("~/Portals/0/Gallery/");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                var uniqueName = DateTime.UtcNow.Ticks + "_" + fileName;
                var path = Path.Combine(folderPath, uniqueName);
                file.SaveAs(path);

                var item = new Item
                {
                    ModuleId = moduleId,
                    ItemName = itemName,
                    ItemDescription = "",
                    ImagePath = "/Portals/0/Gallery/" + uniqueName,
                    CreatedByUserId = userId,
                    CreatedOnDate = DateTime.UtcNow,
                    LastModifiedByUserId = userId,
                    LastModifiedOnDate = DateTime.UtcNow,
                    IsGlobal = false,
                    IsUserApproved = false
                };

                ItemManager.Instance.CreateItem(item);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ── Admin csillagozás (IsGlobal toggle) ──
        [System.Web.Http.HttpPost]
        [DnnAuthorize(StaticRoles = "Administrators")]
        public System.Web.Http.IHttpActionResult ToggleGlobal(int itemId)
        {
            try
            {
                var item = ItemManager.Instance.GetItemById(itemId);
                if (item == null)
                    return NotFound();

                item.IsGlobal = !item.IsGlobal;
                ItemManager.Instance.UpdateItem(item);

                return Ok(new { success = true, isGlobal = item.IsGlobal });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ── User engedélyezés (IsUserApproved toggle) ──
        // MÓDOSÍTVA: A kép tulajdonosa állíthatja (user VAGY admin a sajátjánál)
        [System.Web.Http.HttpPost]
        [DnnAuthorize]
        public System.Web.Http.IHttpActionResult ToggleUserApproval(int itemId)
        {
            try
            {
                var item = ItemManager.Instance.GetItemById(itemId);
                if (item == null)
                    return NotFound();

                // A kép tulajdonosa állíthatja (legyen az admin vagy sima user)
                bool isOwner = item.CreatedByUserId == UserInfo.UserID;

                if (!isOwner)
                    return Unauthorized();

                item.IsUserApproved = !item.IsUserApproved;
                ItemManager.Instance.UpdateItem(item);

                return Ok(new { success = true, isUserApproved = item.IsUserApproved });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}