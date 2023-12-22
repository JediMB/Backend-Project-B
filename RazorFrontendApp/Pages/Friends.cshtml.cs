using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services;

namespace RazorFrontendApp.Pages
{
    public class FriendsModel : PageModel
    {
        private IFriendsService _service;
        private readonly ILogger<FriendsModel> _logger;

        public int FriendCount { get; set; }
        public int HomelessFriendCount { get; set; }
        public List<(string name, int cities, int friends)> Countries { get; set; } = new();
        public string ErrorMessage { get; set; } = null;

        public async Task<IActionResult> OnGet()
        {
            try
            {
                var info = await _service.InfoAsync;

                FriendCount = info.Db.nrSeededFriends + info.Db.nrUnseededFriends;
                HomelessFriendCount = FriendCount - info.Db.nrFriendsWithAddress;

                Countries = info.Friends
                    .GroupBy(f => f.Country)
                    .Where(g => g.Key is not null)
                    .Select(g => (
                        g.Key,
                        g.Count(c => c.City is not null),
                        g.Where(c => c.City is null).FirstOrDefault()?.NrFriends ?? 0
                    ))
                    .OrderBy(g => g.Item1)
                    .ToList();

                if (Countries.Count == 0)
                    throw new ArgumentException("No friends with addresses found");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }
        public FriendsModel(IFriendsService service, ILogger<FriendsModel> logger)
        {
            _service = service;
            _logger = logger;
        }
    }
}
