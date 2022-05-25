using System.Diagnostics;
using Firebase.Auth;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Google.Cloud.Firestore;
using FirebaseAuthException = Firebase.Auth.FirebaseAuthException;

namespace Mathio.Controllers;

public class ProfileController : Controller
{
    private readonly FirebaseAuthProvider _auth;
    private readonly FirestoreDb _db;

    public ProfileController()
    {
        _auth = new FirebaseAuthProvider(
            new FirebaseConfig("AIzaSyAFjhO8zLz4S-nUoZyEtXZbzawQ0oor78k"));

        _db = FirestoreDb.Create("pz202122-cf12f");
    }
    //GET: /Profile
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            TempData["msg"] = "Zaloguj się aby kontynuować";
            return RedirectToAction("SignIn", routeValues: new {returnUrl = "/Profile"});
        }

        try
        {
            var user = await _auth.GetUserAsync(token);
            return View(user);
        }
        catch (FirebaseAuthException e)
        {
            if (e.Reason == AuthErrorReason.InvalidIDToken)
            {
                TempData["msg"] = "Nieprawidłowy token uwierzytelniający! Zaloguj się aby kontynuować";
            }
            return RedirectToAction("SignIn", routeValues: new {returnUrl = "/Profile"});
        }
    }
    //GET: /Profile/Register
    public IActionResult Register()
    {
        return View();
    }
    //POST: /Profile/Register
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View();
        try
        {
            //create the user
            var fbAuth = await _auth.
                CreateUserWithEmailAndPasswordAsync(model.Email, model.Password, model.UserName,
                    true);
            
            /*userModel.ID = fbAuth.User.LocalId;
            userModel.Type = "user";
            userModel.Points = 0;
            CollectionReference usersRef = _db.Collection("Users");
            await usersRef.Document(userModel.ID).SetAsync(userModel);*/

            TempData["msg"] = "Pomyślnie zarejestrowano";
            return RedirectToAction("SignIn");
        }
        catch(FirebaseAuthException e){
            switch (e.Reason)
            {
                case AuthErrorReason.EmailExists:
                    ModelState.AddModelError("Email", "Konto z tym adresem e-mail już istnieje"); 
                    break;
                case AuthErrorReason.WeakPassword:
                    ModelState.AddModelError("Password", "Hasło nie spełnia wymagań bezpieczeństwa");
                    break;
                default:
                    ViewBag.Reason = e.Reason;
                    break;
            }

            return View();
        }
    }
    //GET: /Profile/SignIn
    public IActionResult SignIn(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            return View();
        }
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }
    //POST: /Profile/SignIn
    [HttpPost]
    public async Task<IActionResult> SignIn(SignInViewModel model)
    {
        if (!ModelState.IsValid) return View();
        try
        {
            //log in the new user
            var fbAuth = await _auth
                .SignInWithEmailAndPasswordAsync(model.Email, model.Password);
            await fbAuth.RefreshUserDetails();
            var token = fbAuth.FirebaseToken;
            //saving the token in a session variable
            if (token != null && fbAuth.User.IsEmailVerified)
            {
                HttpContext.Session.SetString("_UserToken", token);
                return RedirectToAction("Index");
            }
            else if (!fbAuth.User.IsEmailVerified)
            {
                ModelState.AddModelError("Email", "Email nie zweryfikowany!");
                return View();
            }
            else
            {
                ViewBag.Reason = "Błąd logowania!";
                return View();
            }
        }
        catch (FirebaseAuthException e)
        {
            switch (e.Reason)
            {
                case AuthErrorReason.WrongPassword:
                    ModelState.AddModelError("Password", "Nieprawidłowe hasło");
                    break;
                case AuthErrorReason.UnknownEmailAddress:
                    ModelState.AddModelError("Email", "Nieznany adres e-mail");
                    break;
                default:
                    ViewBag.Reason = e.Reason;
                    break;
            }
            return View();
        }
    }
    //GET: /Profile/LogOut
    [HttpGet]
    public IActionResult LogOut(){
        HttpContext.Session.Remove("_UserToken");
        TempData["msg"] = "Pomyślnie wylogowano";
        return RedirectToAction("SignIn");
    }
    //GET: /Profile/Settings
    [Route("Profile/Settings")]
    public IActionResult Settings()
    {
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            TempData["msg"] = "Zaloguj się aby kontynuować";
            return RedirectToAction("SignIn", routeValues:new{returnUrl="/Profile/Settings"});
        }
        try
        {
            var user = _auth.GetUserAsync(token).Result;
            return View("Settings/Index", user);
        }
        catch (FirebaseAuthException e)
        {
            if (e.Reason == AuthErrorReason.InvalidIDToken)
            {
                TempData["msg"] = "Nieprawidłowy token uwierzytelniający! Zaloguj się aby kontynuować";
            }
            return RedirectToAction("SignIn", routeValues:new{returnUrl="/Profile/Settings"});
        }
    }
    //GET: /Profile/Settings/ChangePassword
    [Route("Profile/Settings/ChangePassword")]
    [HttpGet]
    public async Task<IActionResult> ChangePassword()
    {
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            TempData["msg"] = "Zaloguj się aby zmienić hasło";
            return RedirectToAction("SignIn", routeValues: new{returnUrl="/Profile/Settings/ChangePassword"});
        }
        
        try
        {
            await _auth.GetUserAsync(token);
            return View("Settings/ChangePassword");
        }
        catch (FirebaseAuthException e)
        {
            if (e.Reason == AuthErrorReason.InvalidIDToken)
            {
                TempData["msg"] = "Nieprawidłowy token uwierzytelniający! Zaloguj się aby zmienić hasło";
            }
            return RedirectToAction("SignIn", routeValues: new{returnUrl="/Profile/Settings/ChangePassword"});
        }
    }
    //POST: /Profile/Settings/ChangePassword
    [Route("Profile/Settings/ChangePassword")]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel data)
    {
        if (!ModelState.IsValid) return View("Settings/ChangePassword");
        try
        {
            var token = HttpContext.Session.GetString("_UserToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["msg"] = "Zaloguj się aby zmienić hasło";
                return RedirectToAction("SignIn", routeValues: new{returnUrl="/Profile/Settings/ChangePassword"});
            }
            var loggedUser = await  _auth.GetUserAsync(token);
            var authLink = await _auth.SignInWithEmailAndPasswordAsync(loggedUser.Email, data.OldPassword);
            HttpContext.Session.SetString("_UserToken", authLink.FirebaseToken);
            authLink = await _auth.ChangeUserPassword(token, data.NewPassword);
            HttpContext.Session.SetString("_UserToken", authLink.FirebaseToken);
            ViewBag.Success = "Pomyślnie zmieniono hasło.";
            return View("Settings/ChangePassword");

        }
        catch (FirebaseAuthException e)
        {
            switch (e.Reason)
            {
                case AuthErrorReason.WrongPassword:
                    ModelState.AddModelError("OldPassword", "Nieprawidłowe hasło");
                    break;
                case AuthErrorReason.MissingPassword:
                    ModelState.AddModelError("OldPassword", "Nie podano aktualnego hasła");
                    break;
                case AuthErrorReason.WeakPassword:
                    ModelState.AddModelError("NewPassword", "Hasło nie spełnia wymagań bezpieczeństwa");
                    break;
                case AuthErrorReason.InvalidIDToken:
                    TempData["msg"] = "Nieprawidłowy token uwierzytelniający! Zaloguj się aby zmienić hasło";
                    return RedirectToAction("SignIn", routeValues: new{returnUrl="/Profile/Settings/ChangePassword"});
                default:
                    ViewBag.Reason = e.Reason;
                    break;
            }
        }
        catch (Exception e)
        {
            ViewBag.Reason = e.Message;
        }
        
        return View("Settings/ChangePassword");
    }
    [Route("Profile/Settings/UpdateEmail")]
    [HttpPost]
    public async Task<IActionResult> UpdateEmail(string email)
    {
        Console.WriteLine("Email:");
        Console.WriteLine(email);
        string? token = HttpContext.Session.GetString("_UserToken");
        return Settings();
    }
    [Route("Profile/Settings/UpdateDisplayName")]
    [HttpPost]
    public async Task<IActionResult> UpdateDisplayName(string dName)
    {
        Console.WriteLine("DisplayName:");
        Console.WriteLine(dName);
        string? token = HttpContext.Session.GetString("_UserToken");
        return Settings();
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}