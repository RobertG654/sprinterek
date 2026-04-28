using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DNNHello.Tests.Models
{
    // Teszthez modellezett tulajdonosság-ellenőrző.
    // Az eredeti DNNHello/Controllers/GalleryApiController.cs ToggleUserApproval() metódusában lévő tulajdonosi check viselkedését tükrözi:
    //   bool isOwner = item.CreatedByUserId == UserInfo.UserID;
    //   if (!isOwner) return Unauthorized();
    public static class OwnershipChecker
    {
        public static bool IsOwner(int itemCreatedByUserId, int currentUserId)
            => itemCreatedByUserId == currentUserId;
    }

    [TestClass]
    public class OwnershipCheckerTests
    {
        // UT-MOD-04
        // Cél: csak a kép tulajdonosa hívhatja meg a műveletet.

        [TestMethod]
        public void IsOwner_SameUserId_ReturnsTrue()
        {
            // A kép tulajdonosa (UserId=42) próbálja hívni
            bool result = OwnershipChecker.IsOwner(itemCreatedByUserId: 42, currentUserId: 42);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsOwner_DifferentUserId_ReturnsFalse()
        {
            // Idegen felhasználó (UserId=99) próbálja egy másik
            // felhasználó (UserId=42) képét módosítani
            bool result = OwnershipChecker.IsOwner(itemCreatedByUserId: 42, currentUserId: 99);

            Assert.IsFalse(result);
        }
    }
}