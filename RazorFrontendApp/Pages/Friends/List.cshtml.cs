using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using Services;

namespace RazorFrontendApp.Pages.Friends
{
    public class ListModel : PageModel
    {
        private IFriendsService _service;
        private ILogger<ListModel> _logger;

        public string Country { get; set; } = null;
        public string City { get; set; } = null;
        public string Filter { get; set; } = null;
        public bool NoAddress { get; set; } = false;

        public string HeaderSuffix { get; set; } = null;
        public List<IFriend> Friends { get; set; } = new();
        public string UpdateMessage { get; set; } = null;
        public string ErrorMessage { get; set; } = null;

        public int PageSize { get; set; } = 10;
        public int CurrentPage { get; set; } = 1;
        public int TotalFriends { get; set; } = 0;
        public int TotalPages { get; set; } = 1;
        public int DisplayedPages { get; set; } = 3;

        public async Task<IActionResult> OnGet()
        {
            try
            {
                Country = Request.Query["country"];
                City = Request.Query["city"];
                Filter = Request.Query["filter"];

                if (bool.TryParse(Request.Query["noAddress"], out bool noAddress))
                    NoAddress = noAddress;
                if (int.TryParse(Request.Query["pageSize"], out int pageSize))
                    PageSize = (pageSize < 1) ? 1 : pageSize;
                if (int.TryParse(Request.Query["page"], out int currentPage))
                    CurrentPage = (currentPage < 1) ? 1 : currentPage;
                
                if (NoAddress is false && Country is null && City is null)
                {
                    TotalFriends = await _service.CountFriendsAsync(null, Filter);

                    TotalPages = (int)Math.Ceiling(Decimal.Divide(TotalFriends, PageSize));

                    Friends = await _service.ReadFriendsAsync(null, false, Filter, CurrentPage-1, PageSize);
                    return Page();
                }

                Country = (Country?.Trim() ?? "");
                City = (City?.Trim() ?? "");

                if (NoAddress)
                    HeaderSuffix = "without addresses";
                else
                    HeaderSuffix = $" in {City}{(City != "" && Country != "" ? ", " : null)}{Country}";

                TotalFriends = await _service.CountFriendsByLocationAsync(null, NoAddress, Country, City, Filter);

                TotalPages = (int)Math.Ceiling(Decimal.Divide(TotalFriends, PageSize));

                Friends = await _service.ReadFriendsByLocationAsync(null, NoAddress, Country, City, Filter, CurrentPage-1, PageSize);

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
