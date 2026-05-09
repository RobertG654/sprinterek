using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DNNHello.Tests.Helpers
{
    // Teszthez modellezett jogosultság-ellenőrző.
    // Az eredeti DNNHello/Controllers/GalleryApiController.cs ToggleGlobal() metódusát védő [DnnAuthorize(StaticRoles = "Administrators")]
    // attribútum viselkedését tükrözi: csak az "Administrators" szerepkörű felhasználók hívhatják meg az admin műveleteket.
    public static class AuthorizationHelper
    {
        public const string AdminRole = "Administrators";

        public static void EnsureAdmin(string userRole)
        {
            if (userRole != AdminRole)
                throw new UnauthorizedAccessException("Csak adminisztrátorok hívhatják ezt a műveletet.");
        }
    }

    [TestClass]
    public class AuthorizationHelperTests
    {
        // UT-MOD-03
        // Cél: ha a felhasználó nem "Administrators" szerepkörű, a metódus dobjon kivételt.

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void EnsureAdmin_NonAdminUser_ThrowsException()
        {
            AuthorizationHelper.EnsureAdmin("RegisteredUsers");
        }

        [TestMethod]
        public void EnsureAdmin_AdminUser_DoesNotThrow()
        {
            // Kontroll eset: admin esetén nem dob hibát.
            AuthorizationHelper.EnsureAdmin("Administrators");
        }
    }
}
