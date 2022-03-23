using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;

namespace Mathio.Controllers;

public class ProfileController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    public IActionResult SignIn()
    {
        return View();
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}