﻿using WebGoatCore.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebGoatCore.ViewModels;

namespace WebGoatCore.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly CustomerRepository _customerRepository;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, CustomerRepository customerRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _customerRepository = customerRepository;
        }

        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            await _signInManager.SignOutAsync();
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser(model.Username)
                {
                    Email = model.Email
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    _customerRepository.CreateCustomer(model.CompanyName, model.Username, model.Address, model.City, model.Region, model.PostalCode, model.Country);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        public IActionResult MyAccount() => View();

        public IActionResult ViewAccountInfo()
        {
            var customer = _customerRepository.GetCustomerByUsername(_userManager.GetUserName(User));
            if (customer == null)
            {
                return View(new ViewAccountInfoViewModel()
                {
                    ErrorMessage = "We don't recognize your customer Id. Please log in and try again."
                });
            }

            return View(new ViewAccountInfoViewModel()
            {
                Customer = customer
            });
        }

        [HttpGet]
        public IActionResult ChangeAccountInfo()
        {
            var customer = _customerRepository.GetCustomerByUsername(_userManager.GetUserName(User));
            if (customer == null)
            {
                ModelState.AddModelError(string.Empty, "We don't recognize your customer Id. Please log in and try again.");
                return View(new ChangeAccountInfoViewModel());
            }

            return View(new ChangeAccountInfoViewModel()
            {
                CompanyName = customer.CompanyName,
                ContactTitle = customer.ContactTitle,
                Address = customer.Address,
                City = customer.City,
                Region = customer.Region,
                PostalCode = customer.PostalCode,
                Country = customer.Country,
            });
        }

        [HttpPost]
        public IActionResult ChangeAccountInfo(ChangeAccountInfoViewModel model)
        {
            if (ModelState.IsValid)
            {
                var customer = _customerRepository.GetCustomerByUsername(_userManager.GetUserName(User));
                customer.CompanyName = model.CompanyName ?? customer.CompanyName;
                customer.ContactTitle = model.ContactTitle ?? customer.ContactTitle;
                customer.Address = model.Address ?? customer.Address;
                customer.City = model.City ?? customer.City;
                customer.Region = model.Region ?? customer.Region;
                customer.PostalCode = model.PostalCode ?? customer.PostalCode;
                customer.Country = model.Country ?? customer.Country;
                _customerRepository.SaveCustomer(customer);

                model.UpdatedSucessfully = true;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _userManager.ChangePasswordAsync(await _userManager.GetUserAsync(User), model.OldPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    return View("ChangePasswordSuccess");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
    }
}