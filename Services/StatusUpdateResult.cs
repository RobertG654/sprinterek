using System.Text;
using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.Services;

/// <summary>
/// Outcome of an <see cref="OrderStatusUpdateService"/> call.
///
/// <para>The service ALWAYS attempts a verification GET after the POST and reports
/// what the server actually persisted, because Hotcakes will sometimes return
/// HTTP 200 OK without changing the underlying record.</para>
///
/// <list type="bullet">
///   <item><see cref="Success"/> is true only when the verification GET shows
///         the new <see cref="OrderStatus.StatusCode"/> on the order.</item>
///   <item><see cref="ErrorMessage"/> is a Hungarian, user-displayable string
///         (null on success).</item>
///   <item><see cref="DebugLog"/> contains the full request/response trace —
///         useful while we're still confirming endpoint behavior.</item>
///   <item><see cref="VerifiedOrder"/> is the order as the server returned it
///         after the update, when the verification GET succeeded.</item>
/// </list>
/// </summary>
public sealed class StatusUpdateResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string DebugLog { get; init; } = "";
    public OrderDetail? VerifiedOrder { get; init; }

    public static StatusUpdateResult Ok(OrderDetail verified, string log) =>
        new() { Success = true, VerifiedOrder = verified, DebugLog = log };

    public static StatusUpdateResult Fail(string error, string log, OrderDetail? verified = null) =>
        new() { Success = false, ErrorMessage = error, DebugLog = log, VerifiedOrder = verified };

    /// <summary>
    /// Helper that returns a copy with extra log lines appended — keeps the
    /// service code free from `var sb = ...; sb.AppendLine(...)` boilerplate.
    /// </summary>
    public StatusUpdateResult WithExtraLog(string extraLog)
    {
        var combined = new StringBuilder(DebugLog);
        if (DebugLog.Length > 0 && !DebugLog.EndsWith('\n')) combined.AppendLine();
        combined.Append(extraLog);
        return new StatusUpdateResult
        {
            Success = Success,
            ErrorMessage = ErrorMessage,
            DebugLog = combined.ToString(),
            VerifiedOrder = VerifiedOrder
        };
    }
}
