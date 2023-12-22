using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services;
using System.Globalization;

namespace RazorFrontendApp.Pages.Friends
{
    public class CountryModel : PageModel
    {
        private IFriendsService _service;
        private readonly ILogger<CountryModel> _logger;

        public string Country = null;
        public List<(string name, int friends, int pets)> Cities { get; set; } = new();
        public string ErrorMessage { get; set; } = null;

        public async Task<IActionResult> OnGet()
        {
            try
            {
                Country = Request.Query["name"];

                if (Country == null)
                    throw new ArgumentException("No country specified.");

                var info = await _service.InfoAsync;

                Cities = info.Friends
                    .Where(f => f.Country == (Country != "Unknown" ? Country : null) && f.City != null)
                    .Select(f => (
                        f.City,
                        f.NrFriends,
                        info.Pets.FirstOrDefault(p => p.City == f.City && p.Country == f.Country)?.NrPets ?? 0
                    ))
                    .OrderBy(f => f.Item1, StringComparer.Create(CultureInfo.GetCultureInfo("sv-se"), true))
                    .ToList();

                if (Cities.Count == 0)
                    throw new ArgumentException("Invalid country name");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        public CountryModel(IFriendsService service, ILogger<CountryModel> logger)
        {
            _service = service;
            _logger = logger;
        }
    }
}
