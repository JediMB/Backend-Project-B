using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using Services;

namespace RazorFrontendApp.Pages.Friends.Friend
{
    public class AddressModel : PageModel
    {
        private readonly IFriendsService _service;
        private readonly ILogger<AddressModel> _logger;

        public List<string> UpdateMessages { get; set; } = new();
        public List<string> ErrorMessages { get; set; } = new();

        public IFriend Friend { get; set; }
        public List<IAddress> Addresses { get; set; } = new();
        public IAddress Address { get; set; }

        [BindProperty]
        public Guid FriendId { get; set; }
        [BindProperty]
        public Guid SelectedAddressId { get; set; }


        public string Filter { get; set; } = null;
        public List<SelectListItem> PageSizes { get; set; } = new();

        public int PageSize { get; set; } = 10;
        public int CurrentPage { get; set; } = 1;
        public int TotalAddresses { get; set; } = 0;
        public int TotalPages { get; set; } = 1;
        public int DisplayedPages { get; set; } = 4;

        public async Task<IActionResult> OnGet(Guid friendId)
        {
            if (friendId == Guid.Empty)
                return Page();

            FriendId = friendId;

            try
            {
                Friend = await _service.ReadFriendAsync(null, FriendId, false)
                    ?? throw new Exception($"Friend not found ({FriendId}).");
                
                Address = Friend?.Address;
                Guid exceptionId = Address?.AddressId ?? Guid.Empty;


                Filter = Request.Query["filter"];

                if (int.TryParse(Request.Query["pageSize"], out int _pageSize))
                    PageSize = (_pageSize < 1) ? 1 : _pageSize;

                PageSizes.Add(new SelectListItem($"{PageSize}", $"{PageSize}", true));
                for (int i = 5; i <= 40; i *= 2)
                {
                    if (i != PageSize)
                        PageSizes.Add(new SelectListItem($"{i}", $"{i}"));
                }
                PageSizes.Sort((a, b) => int.Parse(a.Value).CompareTo(int.Parse(b.Value)));

                if (int.TryParse(Request.Query["page"], out int _currentPage))
                    CurrentPage = (_currentPage < 1) ? 1 : _currentPage;

                TotalAddresses = await _service.CountAddressesAsync(null, Filter, exceptionId);
                TotalPages = (int)Math.Ceiling(Decimal.Divide(TotalAddresses, PageSize));
                CurrentPage = int.Max(int.Min(CurrentPage, TotalPages), 1);

                Addresses = await _service.ReadAddressesAsync(null, true, Filter, exceptionId, CurrentPage - 1, PageSize);
            }
            catch (Exception ex)
            {
                ErrorMessages.Add(ex.Message);
            }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            try
            {
                if (FriendId == Guid.Empty)
                    throw new Exception("No FriendId");

                if (SelectedAddressId == Guid.Empty)
                    throw new Exception("No AddressId");

                var friend = await _service.ReadFriendAsync(null, FriendId, false)
                    ?? throw new Exception($"Friend not found ({FriendId})");

                friend = await _service.UpdateFriendAsync(null, new Models.DTO.csFriendCUdto(friend) { AddressId = SelectedAddressId })
                    ?? throw new Exception($"Could not update friend's ({FriendId}) address ({SelectedAddressId})");
            }
            catch(Exception ex)
            {
                ErrorMessages.Add(ex.Message);

                return Page();
            }

            return RedirectToPage("/Friends/Friend", new { action = FormMode.Edit, id = FriendId });
        }

        public AddressModel(IFriendsService service, ILogger<AddressModel> logger)
        {
            _service = service;
            _logger = logger;
        }
    }
    public enum FormMode { View, Edit, Add }
}
