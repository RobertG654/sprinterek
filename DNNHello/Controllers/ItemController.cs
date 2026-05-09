using DNNHello.DNNHello.Components;
using DNNHello.DNNHello.Models;
using DotNetNuke.Web.Mvc.Framework.ActionFilters;
using DotNetNuke.Web.Mvc.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;

namespace DNNHello.DNNHello.Controllers
{
    [DnnHandleError]
    public class ItemController : DnnController
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

        private bool IsAllowedImage(HttpPostedFileBase file)
        {
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            return ext != null && AllowedExtensions.Contains(ext);
        }

        [ModuleAction(ControlKey = "Edit", TitleKey = "AddItem")]
        public ActionResult Index()
        {
            int userId = User.UserID;
            IEnumerable<Item> items;

            if (userId == -1)
            {
                items = Enumerable.Empty<Item>();
            }
            else if (User.IsSuperUser || User.IsInRole("Administrators"))
            {
                items = ItemManager.Instance.GetItems(ModuleContext.ModuleId);
            }
            else
            {
                items = ItemManager.Instance.GetItemsByUser(ModuleContext.ModuleId, userId);
            }

            // Globális galéria MINDIG betöltődik (bejelentkezés nélkül is)
            ViewBag.GlobalItems = ItemManager.Instance.GetGlobalItems();
            ViewBag.IsAdmin = User.IsSuperUser || User.IsInRole("Administrators");
            ViewBag.CurrentUserId = userId;

            return View(items);
        }

        public ActionResult Delete(int itemId)
        {
            var item = ItemManager.Instance.GetItem(itemId, ModuleContext.ModuleId);

            if (item != null)
            {
                bool isOwner = item.CreatedByUserId == User.UserID;
                bool isAdmin = User.IsSuperUser || User.IsInRole("Administrators");

                if (isOwner || isAdmin)
                {
                    if (!string.IsNullOrEmpty(item.ImagePath))
                    {
                        var physicalPath = Server.MapPath("~" + item.ImagePath);
                        if (System.IO.File.Exists(physicalPath))
                        {
                            System.IO.File.Delete(physicalPath);
                        }
                    }

                    ItemManager.Instance.DeleteItem(itemId, ModuleContext.ModuleId);
                }
            }

            return RedirectToDefaultRoute();
        }

        [HttpGet]
        public ActionResult Edit(int itemId = -1)
        {
            if (User.UserID == -1) return RedirectToDefaultRoute();

            if (itemId == -1)
            {
                var item = new Item { ModuleId = ModuleContext.ModuleId, ItemId = -1 };
                return View(item);
            }
            else
            {
                var item = ItemManager.Instance.GetItem(itemId, ModuleContext.ModuleId);
                if (item == null) return RedirectToDefaultRoute();

                bool isOwner = item.CreatedByUserId == User.UserID;
                bool isAdmin = User.IsSuperUser || User.IsInRole("Administrators");
                if (!isOwner && !isAdmin) return RedirectToDefaultRoute();

                return View(item);
            }
        }

        [HttpPost]
        [DotNetNuke.Web.Mvc.Framework.ActionFilters.ValidateAntiForgeryToken]
        public ActionResult Edit(Item item, HttpPostedFileBase file)
        {
            if (User.UserID == -1) return RedirectToDefaultRoute();

            if (file != null && file.ContentLength > 0)
            {
                if (!IsAllowedImage(file))
                {
                    ModelState.AddModelError("file", "Csak képfájlok engedélyezettek (jpg, png, gif, bmp, webp).");
                    return View(item);
                }

                var fileName = Path.GetFileName(file.FileName);
                var folderPath = Server.MapPath("~/Portals/0/Gallery/");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                var uniqueName = DateTime.UtcNow.Ticks + "_" + fileName;
                var path = Path.Combine(folderPath, uniqueName);
                file.SaveAs(path);
                item.ImagePath = "/Portals/0/Gallery/" + uniqueName;
            }

            if (item.ItemId == -1)
            {
                item.ModuleId = ModuleContext.ModuleId;
                if (item.ItemDescription == null) item.ItemDescription = "";
                item.CreatedByUserId = User.UserID;
                item.CreatedOnDate = DateTime.UtcNow;
                item.LastModifiedByUserId = User.UserID;
                item.LastModifiedOnDate = DateTime.UtcNow;
                item.IsGlobal = false;
                item.IsUserApproved = false;

                ItemManager.Instance.CreateItem(item);
            }
            else
            {
                var existingItem = ItemManager.Instance.GetItem(item.ItemId, ModuleContext.ModuleId);
                if (existingItem != null)
                {
                    bool isOwner = existingItem.CreatedByUserId == User.UserID;
                    bool isAdmin = User.IsSuperUser || User.IsInRole("Administrators");

                    if (isOwner || isAdmin)
                    {
                        existingItem.ItemName = item.ItemName;
                        if (!string.IsNullOrEmpty(item.ImagePath)) existingItem.ImagePath = item.ImagePath;
                        existingItem.LastModifiedByUserId = User.UserID;
                        existingItem.LastModifiedOnDate = DateTime.UtcNow;
                        ItemManager.Instance.UpdateItem(existingItem);
                    }
                }
            }

            return RedirectToDefaultRoute();
        }
    }
}