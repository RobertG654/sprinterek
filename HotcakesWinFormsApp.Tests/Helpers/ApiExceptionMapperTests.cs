using System;
using System.Net;
using System.Net.Http;
using HotcakesWinFormsApp.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HotcakesWinFormsApp.Tests.Helpers;

[TestClass]
public class ApiExceptionMapperTests
{
    // UT-KLIENS-05
    // Cél: a HTTP státuszkódokra és kivételekre megfelelő magyar nyelvű, kulcsszavakat tartalmazó hibaüzenetet adjon.

    [TestMethod]
    public void MapHttpStatus_401_ContainsApiKeyMessage()
    {
        var msg = ApiExceptionMapper.MapHttpStatus(401, "Unauthorized");
        StringAssert.Contains(msg, "API kulcs");
    }

    [TestMethod]
    public void MapHttpStatus_403_ContainsApiKeyMessage()
    {
        // 403-at ugyanúgy kell kezelni mint a 401-et (lásd: switch case)
        var msg = ApiExceptionMapper.MapHttpStatus(403, "Forbidden");
        StringAssert.Contains(msg, "API kulcs");
    }

    [TestMethod]
    public void MapHttpStatus_404_ContainsEndpointMessage()
    {
        var msg = ApiExceptionMapper.MapHttpStatus(404, "Not Found");
        StringAssert.Contains(msg, "végpont");
    }

    [TestMethod]
    public void MapHttpStatus_500_ContainsServerErrorMessage()
    {
        var msg = ApiExceptionMapper.MapHttpStatus(500, "Internal Server Error");
        StringAssert.Contains(msg, "szerver");
    }

    [TestMethod]
    public void MapTimeout_ContainsTimeoutMessage()
    {
        var msg = ApiExceptionMapper.MapTimeout();
        StringAssert.Contains(msg, "nem válaszol időben");
    }

    [TestMethod]
    public void MapHttpRequest_NoStatusCode_ContainsConnectionMessage()
    {
        // HttpRequestException StatusCode nélkül → hálózati hiba
        var ex = new HttpRequestException("Connection refused");
        var msg = ApiExceptionMapper.MapHttpRequest(ex);

        StringAssert.Contains(msg, "Nem sikerült kapcsolódni");
    }
}