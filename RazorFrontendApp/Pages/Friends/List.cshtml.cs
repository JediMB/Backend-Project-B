using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using Services;

namespace RazorFrontendApp.Pages.Friends
{
    public class ListModel : PageModel
    {
        private IFriendsService _service;
        private ILogger<ListModel> _logger;

        public string Filter { get; set; } = null;

        public string HeaderSuffix { get; set; } = null;
        public List<IFriend> Friends { get; set; } = new();
        public string UpdateMessage { get; set; } = null;
        public string ErrorMessage { get; set; } = null;

        public List<SelectListItem> PageSizes { get; set; } = new();

        public int PageSize { get; set; } = 10;
        public int CurrentPage { get; set; } = 1;
        public int TotalFriends { get; set; } = 0;
        public int TotalPages { get; set; } = 1;
        public int DisplayedPages { get; set; } = 4;

        public async Task<IActionResult> OnGet()
        {
            try
            {
                Filter = Request.Query["filter"];

                string country = Request.Query["country"];
                string city = Request.Query["city"];

                bool noAddress = false;
                if (bool.TryParse(Request.Query["noAddress"], out bool _noAddress))
                    noAddress = _noAddress;
                
                if (int.TryParse(Request.Query["pageSize"], out int _pageSize))
                    PageSize = (_pageSize < 1) ? 1 : _pageSize;

                PageSizes.Add(new SelectListItem($"{PageSize}", $"{PageSize}", true));
                for (int i = 5; i <= 40; i*=2)
                {
                    if (i != PageSize)
                        PageSizes.Add(new SelectListItem($"{i}", $"{i}"));
                }
                PageSizes.Sort((a, b) => int.Parse(a.Value).CompareTo(int.Parse(b.Value)));
                
                if (int.TryParse(Request.Query["page"], out int _currentPage))
                    CurrentPage = (_currentPage < 1) ? 1 : _currentPage;
                
                if (noAddress is false && country is null && city is null)
                {
                    TotalFriends = await _service.CountFriendsAsync(null, Filter);

                    TotalPages = (int)Math.Ceiling(Decimal.Divide(TotalFriends, PageSize));
                    CurrentPage = int.Max(int.Min(CurrentPage, TotalPages), 1);

                    Friends = await _service.ReadFriendsAsync(null, false, Filter, CurrentPage-1, PageSize);
                    return Page();
                }

                country = (country?.Trim() ?? "");
                city = (city?.Trim() ?? "");

                if (noAddress)
                    HeaderSuffix = "without addresses";
                else
                    HeaderSuffix = $" in {city}{(city != "" && country != "" ? ", " : null)}{country}";

                TotalFriends = await _service.CountFriendsByLocationAsync(null, noAddress, country, city, Filter);

                TotalPages = (int)Math.Ceiling(Decimal.Divide(TotalFriends, PageSize));
                CurrentPage = int.Max(int.Min(CurrentPage, TotalPages), 1);

                Friends = await _service.ReadFriendsByLocationAsync(null, noAddress, country, city, Filter, CurrentPage-1, PageSize);

                if (Friends.Count == 0)
                    throw new ArgumentException("No friends found");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            try
            {
                string friendId = Request.Form["delete-id"];

                Guid deleteFriendGuid = Guid.Parse(friendId);

                IFriend deletedFriend = await _service.DeleteFriendAsync(null, deleteFriendGuid)
                    ?? throw new ArgumentException("Failed to delete friend. Friend not found");

                UpdateMessage = $"Deleted {deletedFriend.FirstName} {deletedFriend.LastName} ({deletedFriend.FriendId})";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return await OnGet();
        }

        public ListModel(IFriendsService service, ILogger<ListModel> logger)
        {
            _service = service;
            _logger = logger;
        }
    }
}
