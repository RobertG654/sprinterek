using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotcakesWinFormsApp.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HotcakesWinFormsApp.Tests.Models;

[TestClass]
public class HotcakesOrderStatusTests
{
    // UT-KLIENS-04
    // Cél: a "Számlázva" gomb által célzott státusz a Hotcakes által elvárt Complete státuszkódra váltson.
    // A Hotcakes Commerce alapértelmezés szerint a "Complete" státusz GUID-ja: 09D7305D-BD95-48d2-A025-16ADC827582A, neve: "Complete".
    // Ha ez a konstans véletlenül elromlik (pl. átírják), a számla utáni státuszváltás csendben hibás állapotba teszi a rendelést.

    [TestMethod]
    public void Complete_HasExpectedStatusCodeAndName()
    {
        var complete = HotcakesOrderStatus.Complete;

        Assert.AreEqual("09D7305D-BD95-48d2-A025-16ADC827582A", complete.StatusCode);
        Assert.AreEqual("Complete", complete.StatusName);
    }
}
