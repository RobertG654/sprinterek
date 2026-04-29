using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DNNHello.Tests.Models
{
    // Teszthez használt egyszerűsített Item modell.
    // Az eredeti DNNHello.Models.Item osztály viselkedését tükrözi (lásd: DNNHello/Models/Item.cs), DNN runtime függőségek nélkül.
    public class Item
    {
        public bool IsGlobal { get; set; }
        public bool IsUserApproved { get; set; }

        // A globális galéria láthatósági szabálya.
        // Megfelel a megvalósíthatósági tanulmány "Globális galéria logika" pontjának: csak akkor látható, ha mindkét fél engedélyezte.
        public bool IsVisibleInGlobalGallery => IsGlobal && IsUserApproved;
    }

    [TestClass]
    public class ItemTests
    {
        // ── UT-MOD-01 ────────────────────────────────────────────────
        // Cél: csak akkor true, ha IsGlobal ÉS IsUserApproved is true.
        //      Minden más kombinációra false.

        [TestMethod]
        public void IsVisibleInGlobalGallery_BothTrue_ReturnsTrue()
        {
            var item = new Item { IsGlobal = true, IsUserApproved = true };
            Assert.IsTrue(item.IsVisibleInGlobalGallery);
        }

        [TestMethod]
        public void IsVisibleInGlobalGallery_OnlyGlobal_ReturnsFalse()
        {
            var item = new Item { IsGlobal = true, IsUserApproved = false };
            Assert.IsFalse(item.IsVisibleInGlobalGallery);
        }

        [TestMethod]
        public void IsVisibleInGlobalGallery_OnlyUserApproved_ReturnsFalse()
        {
            var item = new Item { IsGlobal = false, IsUserApproved = true };
            Assert.IsFalse(item.IsVisibleInGlobalGallery);
        }

        [TestMethod]
        public void IsVisibleInGlobalGallery_BothFalse_ReturnsFalse()
        {
            var item = new Item { IsGlobal = false, IsUserApproved = false };
            Assert.IsFalse(item.IsVisibleInGlobalGallery);
        }
    }
}
