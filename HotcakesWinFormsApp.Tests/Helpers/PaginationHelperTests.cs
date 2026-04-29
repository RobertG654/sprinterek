using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotcakesWinFormsApp.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HotcakesWinFormsApp.Tests.Helpers;

[TestClass]
public class PaginationHelperTests
{
    // UT-KLIENS-02
    // Cél: a 2. oldal kezdő rekordja a 21. legyen (offset=20), ha a lapméret 20.

    [TestMethod]
    public void GetOffset_SecondPage_WithPageSize20_Returns20()
    {
        int offset = PaginationHelper.GetOffset(pageNumber: 2, pageSize: 20);

        Assert.AreEqual(20, offset);
    }

    [TestMethod]
    public void GetOffset_FirstPage_Returns0()
    {
        // Az első oldalon nincs offset – ez a "control" eset.
        int offset = PaginationHelper.GetOffset(pageNumber: 1, pageSize: 20);

        Assert.AreEqual(0, offset);
    }
}