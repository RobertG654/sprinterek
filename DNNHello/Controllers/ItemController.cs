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
        // 1. LISTÁZÁS
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

            // Globális galéria képek
            var globalItems = Enumerable.Empty<Item>();
            if (userId != -1)
            {
                globalItems = ItemManager.Instance.GetGlobalItems();
            }
            ViewBag.GlobalItems = globalItems;
            ViewBag.IsAdmin = User.IsSuperUser || User.IsInRole("Administrators");
            ViewBag.CurrentUserId = userId;

            return View(items);
        }

        // 2. TÖRLÉS
        public ActionResult Delete(int itemId)
        {
            var item = ItemManager.Instance.GetItem(itemId, ModuleContext.ModuleId);

            if (item != null)
            {
                bool isOwner = item.CreatedByUserId == User.UserID;
                bool isAdmin = User.IsSuperUser || User.IsInRole("Administrators");

                if (isOwner || isAdmin)
                {
                    ItemManager.Instance.DeleteItem(itemId, ModuleContext.ModuleId);
                }
            }

            return RedirectToDefaultRoute();
        }

        // 3. EDIT GET
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

        // 4. EDIT POST
        [HttpPost]
        [DotNetNuke.Web.Mvc.Framework.ActionFilters.ValidateAntiForgeryToken]
        public ActionResult Edit(Item item, HttpPostedFileBase file)
        {
            if (User.UserID == -1) return RedirectToDefaultRoute();

            if (file != null && file.ContentLength > 0)
            {
                var fileName = Path.GetFileName(file.FileName);
                var folderPath = Server.MapPath("~/Portals/0/Gallery/");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                var path = Path.Combine(folderPath, fileName);
                file.SaveAs(path);
                item.ImagePath = "/Portals/0/Gallery/" + fileName;
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