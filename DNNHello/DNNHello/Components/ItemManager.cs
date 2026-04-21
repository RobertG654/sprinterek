using System.Collections.Generic;
using DotNetNuke.Data;
using DNNHello.DNNHello.Models;

namespace DNNHello.DNNHello.Components
{
    public class ItemManager
    {
        public static ItemManager Instance = new ItemManager();

        // ── Létrehozás ──────────────────────────────────────────────────────
        public void CreateItem(Item i)
        {
            using (IDataContext db = DataContext.Instance())
            {
                var rep = db.GetRepository<Item>();
                rep.Insert(i);
            }
        }

        // Régi név megtartva, hogy ne törjön más kód
        public void AddItem(Item i) => CreateItem(i);

        // ── Egy elem lekérése ────────────────────────────────────────────────
        public Item GetItem(int itemId, int moduleId)
        {
            using (IDataContext db = DataContext.Instance())
            {
                var rep = db.GetRepository<Item>();
                return rep.GetById(itemId, moduleId);
            }
        }

        // ── Összes elem lekérése (modul szerint) ────────────────────────────
        public IEnumerable<Item> GetItems(int moduleId)
        {
            using (IDataContext db = DataContext.Instance())
            {
                var rep = db.GetRepository<Item>();
                return rep.Get(moduleId);
            }
        }

        // ── Elemek lekérése felhasználó szerint (ÚJ) ────────────────────────
        public IEnumerable<Item> GetItemsByUser(int moduleId, int userId)
        {
            using (IDataContext db = DataContext.Instance())
            {
                var rep = db.GetRepository<Item>();
                return rep.Find("WHERE ModuleId = @0 AND CreatedByUserId = @1", moduleId, userId);
            }
        }

        // ── Frissítés ────────────────────────────────────────────────────────
        public void UpdateItem(Item i)
        {
            using (IDataContext db = DataContext.Instance())
            {
                var rep = db.GetRepository<Item>();
                rep.Update(i);
            }
        }

        // ── Törlés ───────────────────────────────────────────────────────────
        public void DeleteItem(int itemId, int moduleId)
        {
            using (IDataContext db = DataContext.Instance())
            {
                var rep = db.GetRepository<Item>();
                var i = rep.GetById(itemId, moduleId);
                if (i != null) rep.Delete(i);
            }
        }
    }
}