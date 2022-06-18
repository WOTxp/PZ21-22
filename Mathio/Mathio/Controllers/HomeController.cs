using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;



namespace Mathio.Controllers;

public class HomeController : Controller
{
    
    public IActionResult Index()
    {
        var token = HttpContext.Session.GetString("_UserToken");
        ViewBag.Layout = string.IsNullOrEmpty(token) ? "_Layout2" : "_Layout";
        
        return View();
    }

    public IActionResult Privacy()
    {
        var token = HttpContext.Session.GetString("_UserToken");
        ViewBag.Layout = string.IsNullOrEmpty(token) ? "_Layout2" : "_Layout";
        return View();
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}