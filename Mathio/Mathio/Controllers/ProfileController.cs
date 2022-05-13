using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Firebase.Auth;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FirebaseAuth = FirebaseAdmin.Auth.FirebaseAuth;
using FirebaseAuthException = Firebase.Auth.FirebaseAuthException;
using Google.Cloud.Firestore;


namespace Mathio.Controllers;

public class ProfileController : Controller
{
    private FirebaseAuthProvider _auth;
    private FirestoreDb _db;

    public ProfileController()
    {
        _auth = new FirebaseAuthProvider(
            new FirebaseConfig("AIzaSyAFjhO8zLz4S-nUoZyEtXZbzawQ0oor78k"));
        if (FirebaseAuth.DefaultInstance==null)
        {
            FirebaseApp.Create();
        }
        _db = FirestoreDb.Create("pz202122-cf12f");
    }
    // GET
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    public IActionResult Settings()
    {
        string? token = HttpContext.Session.GetString("_UserToken");
        if (!String.IsNullOrEmpty(token))
        {
            try
            {
                var user = _auth.GetUserAsync(token).Result;
                UserModel userModel = new UserModel()
                {
                    Email = user.Email,
                    UserName = user.DisplayName
                };
                return View("Settings/Index", userModel);
            }
            catch (Exception)
            {
                TempData["returnUrl"] = "Settings";
                return RedirectToAction("SignIn");
            }
        }

        TempData["returnUrl"] = "Settings";
        return RedirectToAction("SignIn");
    }
    [HttpPost]
    public async Task<IActionResult> Register(UserModel userModel)
    {
        try
        {
            //create the user
            var fbAuth = await _auth.
                CreateUserWithEmailAndPasswordAsync(userModel.Email, userModel.Password, userModel.UserName,
                true);

            userModel.ID = fbAuth.User.LocalId;
            userModel.Type = "user";
            userModel.Points = 0;
            CollectionReference usersRef = _db.Collection("Users");
            await usersRef.Document(userModel.ID).SetAsync(userModel);

            TempData["msg"] = "Pomyślnie zarejestrowano";
            return RedirectToAction("SignIn");
        }
        catch(FirebaseAuthException e){
            switch (e.Reason)
            {
                case AuthErrorReason.EmailExists:
                    ViewBag.Reason = "Konto z tym adresem e-mail już istnieje";
                    break;
                case AuthErrorReason.WeakPassword:
                    ViewBag.Reason = "Hasło nie spełnia wymagań bezpieczeństwa";
                    break;
                default:
                    ViewBag.Reason = e.Reason;
                    break;
            }

            return View();
        }
    }
    
    public IActionResult SignIn()
    {
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> SignIn(UserModel userModel, string? returnUrl)
    {
        try
        {
            //log in the new user
            var fbAuth = await _auth
                .SignInWithEmailAndPasswordAsync(userModel.Email, userModel.Password);
            await fbAuth.RefreshUserDetails();
            string token = fbAuth.FirebaseToken;
            //saving the token in a session variable
            if (token != null && fbAuth.User.IsEmailVerified)
            {
                HttpContext.Session.SetString("_UserToken", token);
                if (!String.IsNullOrEmpty(returnUrl))
                    return RedirectToAction(returnUrl);
                
                return RedirectToAction("Index");
            }
            else if (!fbAuth.User.IsEmailVerified)
            {
                TempData["returnUrl"] = returnUrl;
                ViewBag.Reason = "Email nie zweryfikowany!";
                return View();
            }
            else
            {
                TempData["returnUrl"] = returnUrl;
                ViewBag.Reason = "Nieudane logowanie!";
                return View();
            }
        }
        catch (FirebaseAuthException e)
        {
            switch (e.Reason)
            {
                case AuthErrorReason.WrongPassword:
                    ViewBag.Reason = "Nieprawidłowe hasło";
                    break;
                case AuthErrorReason.UnknownEmailAddress:
                    ViewBag.Reason = "Nieznany adres e-mail";
                    break;
                default:
                    ViewBag.Reason = e.Reason;
                    break;
            }
            TempData["returnUrl"] = returnUrl;
            return View();
        }
    }
    [HttpPost]
    public IActionResult LogOut(string msg="Pomyślnie wylogowano"){
        HttpContext.Session.Remove("_UserToken");
        TempData["msg"] = msg;
        return RedirectToAction("SignIn");
    }
    
    [Route("Profile/Settings/ChangePassword")]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel data)
    {
        Console.WriteLine("Haslo");
        Console.WriteLine(data.OldPassword);
        Console.WriteLine("Nowe haslo:");
        Console.WriteLine(data.NewPassword);
        if (data.OldPassword == null)
        {
            ViewBag.Reason = "Nie podano aktualnego hasła";
            return Settings();
        }

        if (data.NewPassword == null)
        {
            ViewBag.Reason = "Nie podano nowego hasła";
            return Settings();
        }
        try
        {
            string? token = HttpContext.Session.GetString("_UserToken");
            Console.WriteLine("token:");
            Console.WriteLine(token);
            var user = await _auth.GetUserAsync(token);
            string uid = user.LocalId;
            Console.Write("UID:");
            Console.WriteLine(uid);
            await _auth.SignInWithEmailAndPasswordAsync(user.Email, data.OldPassword);
            
            UserRecordArgs args = new UserRecordArgs()
            {
                Uid = uid,
                Password = data.NewPassword,
            };
            await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);
            string msg = "Pomyślnie zmieniono hasło. Zaloguj się ponownie";
            return LogOut(msg);

        }
        catch (FirebaseAuthException e)
        {
            switch (e.Reason)
            {
                case AuthErrorReason.WrongPassword:
                    ViewBag.Reason = "Nieprawidłowe hasło";
                    break;
                case AuthErrorReason.MissingPassword:
                    ViewBag.Reason = "Nie podano aktualnego hasła";
                    break;
                default:
                    ViewBag.Reason = e.Reason;
                    break;
            }
        }
        catch (ArgumentException)
        {
            ViewBag.Reason = "Nowe hasło nie spełnia wymagań bezpieczeństwa";
        }
        catch (Exception e)
        {
            ViewBag.Reason = e.Message;
        }
        
        
        return Settings();
    }
    [Route("Profile/Settings/UpdateEmail")]
    [HttpPost]
    public async Task<IActionResult> UpdateEmail(string email)
    {
        Console.WriteLine("Email:");
        Console.WriteLine(email);
        string? token = HttpContext.Session.GetString("_UserToken");
        string uid = _auth.GetUserAsync(token).Result.LocalId;
        UserRecordArgs args = new UserRecordArgs()
        {
            Uid = uid,
            Email = email,
        };
        await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);
        
        return Settings();
    }
    [Route("Profile/Settings/UpdateDisplayName")]
    [HttpPost]
    public async Task<IActionResult> UpdateDisplayName(string dName)
    {
        Console.WriteLine("DisplayName:");
        Console.WriteLine(dName);
        string? token = HttpContext.Session.GetString("_UserToken");
        string uid = _auth.GetUserAsync(token).Result.LocalId;
        UserRecordArgs args = new UserRecordArgs()
        {
            Uid = uid,
            DisplayName = dName,
        };
        await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);
        return Settings();
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}