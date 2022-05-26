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
            var userDoc = await _db.Collection("Users").Document(user.LocalId).GetSnapshotAsync();
            var userModel = userDoc.ConvertTo<UserModel>();
            userModel.Email = user.Email;
            return View(userModel);
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
                CreateUserWithEmailAndPasswordAsync(model.Email, model.Password, sendVerificationEmail:true);
            var user = new UserModel
            {
                Id = fbAuth.User.LocalId,
                Type = "user",
                UserName = model.UserName,
                Points = 0
            };
            
            var usersRef = _db.Collection("Users");
            await usersRef.Document(user.Id).SetAsync(user);

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
    public async Task<IActionResult> SignIn(SignInViewModel model, string? returnUrl)
    {
        if (!ModelState.IsValid) return View();
        if (returnUrl != null) ViewBag.ReturnUrl = returnUrl;
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
                if (returnUrl != null) return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
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
    public async Task<IActionResult> Settings()
    {
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            TempData["msg"] = "Zaloguj się aby kontynuować";
            return RedirectToAction("SignIn", routeValues:new{returnUrl="/Profile/Settings"});
        }
        try
        {
            var user = await _auth.GetUserAsync(token);
            var doc = await _db.Collection("Users").Document(user.LocalId).GetSnapshotAsync();
            var userModel = doc.ConvertTo<UserModel>();

            return View("Settings/Index", userModel);
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
    [ValidateAntiForgeryToken]
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
    //POST: /Profile/Settings/UpdateDisplayName
    [Route("Profile/Settings/UpdateDisplayName")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDisplayName(ChangeUserNameModel settings)
    {
        var errors = new Dictionary<string, List<string>> {{"auth", new List<string>()}};
        //Authorize
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            errors["auth"].Add("Zaloguj się aby kontynuować");
            return Json(new {success = false, errors, data = settings.UserName});
        }
        User user;
        try
        {
            user = await _auth.GetUserAsync(token);
        }
        catch (FirebaseAuthException e)
        {
            errors["auth"].Add(e.Reason == AuthErrorReason.InvalidIDToken
                ? "Nieprawidłowy token uwierzytelniający! Zaloguj się aby kontynuować"
                : "Błąd. Zaloguj się aby kontynuować");
            return Json(new {success = false, errors, data = settings.UserName});
        }

        errors.Remove("auth");
        errors.Add("UserName", new List<string>());
        //Check Model State
        if (!ModelState.IsValid)
        {
            foreach (var (_, value) in ModelState)
            {
                foreach (var error in value.Errors)
                {
                    errors["UserName"].Add(error.ErrorMessage);
                }
            }
            return Json(new {success = false, errors, data = settings.UserName});
        }
        //Try update
        try
        {
            await _db.Collection("Users").Document(user.LocalId).UpdateAsync("UserName", settings.UserName);
            return Json(new {success = true, data = settings.UserName});
        }
        catch (Grpc.Core.RpcException e)
        {
            errors["UserName"].Add(e.StatusCode == Grpc.Core.StatusCode.NotFound
                ? "Błąd aktualizacji bazy danych. Nie znaleziono wpisu."
                : "Błąd aktualizacji bazy danych");
            
            return Json(new {success = false, errors, data = settings.UserName});
        }
    }
    //POST: /Profile/Settings/UpdateFirstName
    [Route("Profile/Settings/UpdateFirstName")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFirstName([Bind("FirstName")]ChangeUserInfoModel settings)
    {
        var errors = new Dictionary<string, List<string>> {{"auth", new List<string>()}};
        //Authorize
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            errors["auth"].Add("Zaloguj się aby kontynuować");
            return Json(new {success = false, errors, data = settings.FirstName});
        }
        User user;
        try
        {
            user = await _auth.GetUserAsync(token);
        }
        catch (FirebaseAuthException e)
        {
            errors["auth"].Add(e.Reason == AuthErrorReason.InvalidIDToken
                ? "Nieprawidłowy token uwierzytelniający! Zaloguj się aby kontynuować"
                : "Błąd. Zaloguj się aby kontynuować");
            return Json(new {success = false, errors, data = settings.FirstName});
        }

        errors.Remove("auth");
        errors.Add("FirstName", new List<string>());
        //Check Model State
        if (!ModelState.IsValid)
        {
            foreach (var (_, value) in ModelState)
            {
                foreach (var error in value.Errors)
                {
                    errors["FirstName"].Add(error.ErrorMessage);
                }
            }
            return Json(new {success = false, errors, data = settings.FirstName});
        }
        //Try update
        try
        {
            await _db.Collection("Users").Document(user.LocalId).UpdateAsync("FirstName", settings.FirstName);
            return Json(new {success = true, data = settings.FirstName});
        }
        catch (Grpc.Core.RpcException e)
        {
            errors["FirstName"].Add(e.StatusCode == Grpc.Core.StatusCode.NotFound
                ? "Błąd aktualizacji bazy danych. Nie znaleziono wpisu."
                : "Błąd aktualizacji bazy danych");
            
            return Json(new {success = false, errors, data = settings.FirstName});
        }
    }
    //POST: /Profile/Settings/UpdateLastName
    [Route("Profile/Settings/UpdateLastName")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLastName([Bind("LastName")]ChangeUserInfoModel settings)
    {
        var errors = new Dictionary<string, List<string>> {{"auth", new List<string>()}};
        //Authorize
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            errors["auth"].Add("Zaloguj się aby kontynuować");
            return Json(new {success = false, errors, data = settings.LastName});
        }
        User user;
        try
        {
            user = await _auth.GetUserAsync(token);
        }
        catch (FirebaseAuthException e)
        {
            errors["auth"].Add(e.Reason == AuthErrorReason.InvalidIDToken
                ? "Nieprawidłowy token uwierzytelniający! Zaloguj się aby kontynuować"
                : "Błąd. Zaloguj się aby kontynuować");
            return Json(new {success = false, errors, data = settings.LastName});
        }
        errors.Remove("auth");
        errors.Add("LastName", new List<string>());
        //Check Model State
        if (!ModelState.IsValid)
        {
            foreach (var (_, value) in ModelState)
            {
                foreach (var error in value.Errors)
                {
                    errors["LastName"].Add(error.ErrorMessage);
                }
            }
            return Json(new {success = false, errors, data = settings.LastName});
        }
        //Try update
        try
        {
            await _db.Collection("Users").Document(user.LocalId).UpdateAsync("LastName", settings.LastName);
            return Json(new {success = true, data = settings.LastName});
        }
        catch (Grpc.Core.RpcException e)
        {
            errors["LastName"].Add(e.StatusCode == Grpc.Core.StatusCode.NotFound
                ? "Błąd aktualizacji bazy danych. Nie znaleziono wpisu."
                : "Błąd aktualizacji bazy danych");
            
            return Json(new {success = false, errors, data = settings.LastName});
        }
    }
    //POST: /Profile/Settings/UpdateDescription
    [Route("Profile/Settings/UpdateDescription")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDescription([Bind("Description")]ChangeUserInfoModel settings)
    {
        var errors = new Dictionary<string, List<string>> {{"auth", new List<string>()}};
        //Authorize
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            errors["auth"].Add("Zaloguj się aby kontynuować");
            return Json(new {success = false, errors, data = settings.Description});
        }
        User user;
        try
        {
            user = await _auth.GetUserAsync(token);
        }
        catch (FirebaseAuthException e)
        {
            errors["auth"].Add(e.Reason == AuthErrorReason.InvalidIDToken
                ? "Nieprawidłowy token uwierzytelniający! Zaloguj się aby kontynuować"
                : "Błąd. Zaloguj się aby kontynuować");
            return Json(new {success = false, errors, data = settings.Description});
        }

        errors.Remove("auth");
        errors.Add("Description", new List<string>());
        //Check Model State
        if (!ModelState.IsValid)
        {
            foreach (var (_, value) in ModelState)
            {
                foreach (var error in value.Errors)
                {
                    errors["Description"].Add(error.ErrorMessage);
                }
            }
            return Json(new {success = false, errors, data = settings.Description});
        }
        //Try update
        try
        {
            await _db.Collection("Users").Document(user.LocalId).UpdateAsync("Description", settings.Description);
            return Json(new {success = true, data = settings.Description});
        }
        catch (Grpc.Core.RpcException e)
        {
            errors["Description"].Add(e.StatusCode == Grpc.Core.StatusCode.NotFound
                ? "Błąd aktualizacji bazy danych. Nie znaleziono wpisu."
                : "Błąd aktualizacji bazy danych");
            
            return Json(new {success = false, errors, data = settings.Description});
        }
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}