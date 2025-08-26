using Gauniv.WebServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Admin")]
public class ConnectionAdminController : Controller
{
    private readonly ConnectionTrackingService _connectionTracking;

    public ConnectionAdminController(ConnectionTrackingService connectionTracking)
    {
        _connectionTracking = connectionTracking;
    }

    public IActionResult Index()
    {
        var connections = _connectionTracking.GetAllConnections();
        return View(connections);
    }
}