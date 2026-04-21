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
        [System.Web.Http.HttpPost]
        [DnnAuthorize]
        public System.Web.Http.IHttpActionResult ToggleUserApproval(int itemId)
        {
            try
            {
                var item = ItemManager.Instance.GetItemById(itemId);
                if (item == null)
                    return NotFound();

                // Csak a saját képét engedélyezheti, admin NEM állíthatja
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