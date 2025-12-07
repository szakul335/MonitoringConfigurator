using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringConfigurator.Data;
using MonitoringConfigurator.Models;
using System.Linq;
using System.Threading.Tasks;

namespace MonitoringConfigurator.Controllers
{
    public class InfoController : Controller
    {
        private readonly AppDbContext _ctx;
        private readonly UserManager<IdentityUser> _userManager;

        public InfoController(AppDbContext ctx, UserManager<IdentityUser> userManager)
        {
            _ctx = ctx;
            _userManager = userManager;
        }

        [HttpGet, AllowAnonymous]
        public IActionResult Contact() => View();

        [HttpGet, AllowAnonymous]
        public IActionResult Faq() => View();

        [HttpGet, AllowAnonymous]
        public IActionResult About() => View();

        [HttpGet, AllowAnonymous]
        public IActionResult Guides() => View();


        // Wsparcie – formularz
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Support()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var sent = await _ctx.Contacts
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .Take(200)
                .ToListAsync();

            var vm = new SupportViewModel { Recipient = "Admin", Sent = sent };
            return View(vm);
        }

        // Wsparcie – wysyłka
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Support(SupportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // również załaduj historię wysłanych, żeby tabela się wyświetliła
                var userForInvalid = await _userManager.GetUserAsync(User);
                model.Sent = await _ctx.Contacts
                    .Where(c => c.UserId == userForInvalid.Id)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(200)
                    .ToListAsync();
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            var prefix = model.Recipient == "Operator" ? "[Operator] " : "[Admin] ";
            var contact = new Contact
            {
                UserId = user.Id,
                Subject = prefix + model.Subject,
                Message = model.Message
            };
            _ctx.Contacts.Add(contact);
            await _ctx.SaveChangesAsync();

            ViewBag.Success = "Wiadomość została wysłana.";

            // Odtwórz model z odświeżoną listą wysłanych
            var sent = await _ctx.Contacts
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .Take(200)
                .ToListAsync();

            var vm = new SupportViewModel { Recipient = model.Recipient, Sent = sent };
            ModelState.Clear();
            return View(vm);
        }


        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Opinions()
        {
            var lastOpinions = await _ctx.Contacts
                .Where(c => c.Subject == "Opinia o aplikacji")
                .OrderByDescending(c => c.CreatedAt)
                .Take(50)
                .ToListAsync();

            var vm = new OpinionViewModel
            {
                ExistingOpinions = lastOpinions
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Opinions(OpinionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ExistingOpinions = await _ctx.Contacts
                    .Where(c => c.Subject == "Opinia o aplikacji")
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            var opinion = new Contact
            {
                UserId = user?.Id,
                Subject = "Opinia o aplikacji",
                Message = model.Message
            };

            _ctx.Contacts.Add(opinion);
            await _ctx.SaveChangesAsync();

            TempData["Msg"] = "Dziękujemy za opinię!";

            return RedirectToAction(nameof(Opinions));
        }

        

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOpinion(int id)
        {
            var opinion = await _ctx.Contacts
                .Where(c => c.Subject == "Opinia o aplikacji" && c.Id == id)
                .FirstOrDefaultAsync();

            if (opinion == null)
            {
                return NotFound();
            }

            _ctx.Contacts.Remove(opinion);
            await _ctx.SaveChangesAsync();

            TempData["Msg"] = "Opinia została usunięta.";

            return RedirectToAction(nameof(Opinions));
        }

[Authorize]
        [HttpGet]
        public IActionResult Profile() => View();
    }
}
