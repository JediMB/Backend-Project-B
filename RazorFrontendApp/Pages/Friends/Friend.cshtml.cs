using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using Models.DTO;
using Services;
using System.ComponentModel.DataAnnotations;

namespace RazorFrontendApp.Pages.Friends
{
    public class FriendModel : PageModel
    {
        private readonly IFriendsService _service;
        private readonly ILogger<FriendModel> _logger;

        public List<string> UpdateMessages { get; set; } = new();
        public List<string> ErrorMessages { get; set; } = new();
        public List<IFriend> FriendsAtAddress { get; set; }

        [BindProperty]
        public FriendIM Friend { get; set; } = null;

        [BindProperty]
        public AddressIM Address { get; set; } = null;

        [BindProperty]
        public List<QuoteIM> Quotes { get; set; } = null;

        [BindProperty]
        public FormMode Mode { get; set; } = FormMode.view;

        // Validation
        public bool HasValidationErrors => InvalidKeys.Any();
        public IEnumerable<KeyValuePair<string, ModelStateEntry>> InvalidKeys { get; set; }

        public async Task<IActionResult> OnGet(FormMode action, Guid id)
        {
            Mode = action;

            if (Mode == FormMode.add)
                return Page();

            try
            {
                if (id == Guid.Empty)
                    throw new ArgumentException("No friend id given.");

                IFriend friend = await _service.ReadFriendAsync(null, id, false)
                    ?? throw new Exception($"Friend ({id}) does not exist.");
                
                Friend = new(friend);

                if (friend.Address is not null)
                {
                    IAddress address = await _service.ReadAddressAsync(null, friend.Address.AddressId, false)
                        ?? throw new Exception("Failed to fetch address information.");

                    if (address.Friends.Count > 1)
                        FriendsAtAddress = address.Friends;

                    Address = new(friend.Address);
                }

                if (friend.Quotes?.Count > 0)
                {
                    Quotes = friend.Quotes
                        .Select(q => new QuoteIM(q))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                ErrorMessages.Add(ex.Message);
            }

            return Page();
        }

        public async Task<IActionResult> OnGetAdded(FormMode action, Guid id)
        {
            UpdateMessages.Add($"Added new friend ({id})");

            return await OnGet(action, id);
        }

        public async Task<IActionResult> OnGetUpdated(FormMode action, Guid id)
        {
            UpdateMessages.Add($"Updated friend ({id})");

            return await OnGet(action, id);
        }

        public async Task<IActionResult> OnGetError(FormMode action, Guid id, string msg)
        {
            ErrorMessages.Add(msg);

            return await OnGet(action, id);
        }

        public async Task<IActionResult> OnPostAddFriend()
        {
            string[] validationKeys = new[]
            {
                "Friend.FirstName",
                "Friend.LastName" ,
                "Friend.Email",
                "Friend.Birthday"
            };

            if (IsInvalid(validationKeys))
                return Page();

            try
            {
                csFriendCUdto newFriend = Friend.UpdateModel(new());

                IFriend addedFriend = await _service.CreateFriendAsync(null, newFriend)
                    ?? throw new Exception("New friend was not successfully added.");

                return RedirectToPage("Friend", "added", new { action = FormMode.edit, id = addedFriend.FriendId });
            }
            catch (Exception ex)
            {
                ErrorMessages.Add(ex.Message);

                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateFriend()
        {
            string[] validationKeys = new[]
            {
                "Friend.FriendId",
                "Friend.FirstName",
                "Friend.LastName" ,
                "Friend.Email",
                "Friend.Birthday"
            };

            IFriend friend;

            try
            {
                if (IsInvalid(validationKeys))
                {
                    friend = await _service.ReadFriendAsync(null, Friend.FriendId, false)
                        ?? throw new Exception("Trying to update a friend that does not exist.");

                    if (friend?.Address is not null)
                        Address = new(friend.Address);

                    // Also update quotes and pets

                    return Page();
                }

                friend = await _service.ReadFriendAsync(null, Friend.FriendId, false)
                    ?? throw new Exception("Trying to update a friend that does not exist.");

                Friend.AddressId = friend.Address?.AddressId;

                csFriendCUdto updatedFriend = Friend.UpdateModel(new csFriendCUdto(friend));

                friend = await _service.UpdateFriendAsync(null, updatedFriend)
                    ?? throw new Exception("Failed to update friend.");

                return RedirectToPage("Friend", "updated", new { action = FormMode.edit, id = friend.FriendId });
            }
            catch (Exception ex)
            {
                return RedirectToPage("Friend", "error", new { action = FormMode.edit, id = Friend.FriendId, msg = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostRemoveAddress()
        {
            if (IsInvalid(new[] {"Friend.FriendId"}))
                return RedirectToPage("Friend", "error", new { action = FormMode.edit, id = Friend.FriendId, msg = "Failed to remove address." });

            try
            {
                csFriendCUdto friend = new(await _service.ReadFriendAsync(null, Friend.FriendId, false)
                    ?? throw new Exception("Trying to update a friend that does not exist."));

                friend.AddressId = null;

                var updatedFriend = await _service.UpdateFriendAsync(null, friend)
                    ?? throw new Exception("Failed to update friend.");

                if (updatedFriend.Address is not null)
                    throw new Exception("Failed to remove friend's address.");

                return RedirectToPage("Friend", "updated", new { action = FormMode.edit, id = updatedFriend.FriendId });
            }
            catch (Exception ex)
            {
                return RedirectToPage("Friend", "error", new { action = FormMode.edit, id = Friend.FriendId, msg = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostAddAddress()
        {
            string[] validationKeys = new[] {
                "Friend.FriendId",
                "Address.StreetAddress",
                "Address.ZipCode",
                "Address.City",
                "Address.Country"
            };

            try
            {
                if (IsInvalid(validationKeys))
                {
                    IFriend oldFriend = await _service.ReadFriendAsync(null, Friend.FriendId, false)
                        ?? throw new Exception("Trying to update a friend that does not exist.");

                    Friend = new(oldFriend);

                    // Also update quotes and pets

                    return Page();
                }

                csAddressCUdto newAddress = Address.UpdateModel(new());

                IAddress createdAddress = await _service.CreateAddressAsync(null, newAddress)
                    ?? throw new Exception("Failed to create new address.");

                csFriendCUdto friend = new(await _service.ReadFriendAsync(null, Friend.FriendId, false)
                    ?? throw new Exception("Trying to update a friend that does not exist."));

                friend.AddressId = createdAddress.AddressId;

                IFriend updatedFriend = await _service.UpdateFriendAsync(null, friend)
                    ?? throw new Exception("Failed to update friend.");

                if (updatedFriend.Address is null || updatedFriend.Address.AddressId != createdAddress.AddressId)
                    throw new Exception("Failed to add new address to friend.");

                return RedirectToPage("Friend", "updated", new { action = FormMode.edit, id = updatedFriend.FriendId});
            }
            catch (Exception ex)
            {
                return RedirectToPage("Friend", "error", new { action = FormMode.edit, id = Friend.FriendId, msg = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostUpdateAddress()
        {
            string[] validationKeys = new[]
            {
                "Friend.FriendId",
                "Address.AddressId",
                "Address.StreetAddress",
                "Address.ZipCode",
                "Address.City",
                "Address.Country"
            };

            try
            {
                if (IsInvalid(validationKeys))
                {
                    IFriend friend = await _service.ReadFriendAsync(null, Friend.FriendId, false)
                        ?? throw new Exception("Trying to update an address that does not exist.");

                    Friend = new(friend);

                    // Also update quotes and pets

                    return Page();
                }


                IAddress address = await _service.ReadAddressAsync(null, Address.AddressId, false)
                    ?? throw new Exception("Trying to update an address that does not exist.");

                csAddressCUdto updatedAddress = Address.UpdateModel(new(address));

                address = await _service.UpdateAddressAsync(null, updatedAddress)
                    ?? throw new Exception("Failed to update address.");

                return RedirectToPage("Friend", "updated", new { action = FormMode.edit, id = Friend.FriendId });
            }
            catch (Exception ex)
            {
                return RedirectToPage("Friend", "error", new { action = FormMode.edit, id = Friend.FriendId, msg = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostSubmitQuotes()
        {
            if (Quotes is null)
                return Page();

            return Page();
        }

        public FriendModel(IFriendsService service, ILogger<FriendModel> logger)
        {
            _service = service;
            _logger = logger;
        }

        public bool IsEmptyView(object model)
        {
            return (Mode is FormMode.view && model is null);
        }

        #region Server-side validation
        private bool IsInvalid(string[] limitedValidationKeys = null)
        {
            InvalidKeys = ModelState
                .Where(s => s.Value.ValidationState == ModelValidationState.Invalid)
                .ToList();

            if (limitedValidationKeys is not null)
                InvalidKeys = InvalidKeys
                    .Where(s => limitedValidationKeys
                        .Any(vk => vk == s.Key));

            InvalidKeys
                .SelectMany(k => k.Value.Errors)
                .Select(e => e.ErrorMessage)
                .ToList().ForEach(em =>
                {
                    ErrorMessages.Add(em);
                });

            return InvalidKeys.Any();
        }
        #endregion
    }
    public enum FormMode { view, edit, add }

    #region Input models
    public enum IMStatus { Unknown, Unchanged, Inserted, Modified, Deleted }

    public class FriendIM
    {
        public Guid FriendId { get; set; } = Guid.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Please input a first name")]
        [RegularExpression("^[^<>\\/\"@]*$", ErrorMessage = "First name may not contain special characters < > \\ / \" @")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "First name must contain between 1 and 200 characters")]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Please input a last name")]
        [RegularExpression("^[^<>\\/\"@]*$", ErrorMessage = "Last name may not contain special characters < > \\ / \" @")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Last name must contain between 1 and 200 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Please input an email address")]
        [RegularExpression(@"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9]))\.){3}(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9])|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])", ErrorMessage = "Invalid email address format")]
        [StringLength(200, MinimumLength = 6, ErrorMessage = "Email must contain between 6 and 200 characters")]
        public string Email { get; set; }

        public DateTime? Birthday { get; set; }

        public Guid? AddressId { get; set; } = null;

        #region Constructors and updater
        public FriendIM() { }

        public FriendIM(IFriend original)
        {
            FriendId = original.FriendId;
            FirstName = original.FirstName;
            LastName = original.LastName;
            Email = original.Email;
            Birthday = original.Birthday;
            AddressId = original.Address?.AddressId;
        }

        public FriendIM(FriendIM original)
        {
            FriendId = original.FriendId;
            FirstName = original.FirstName;
            LastName = original.LastName;
            Email = original.Email;
            Birthday = original.Birthday;
            AddressId = original.AddressId;
        }

        public csFriendCUdto UpdateModel(csFriendCUdto model)
        {
            model.FirstName = FirstName;
            model.LastName = LastName;
            model.Email = Email;
            model.Birthday = Birthday;
            model.AddressId = AddressId;

            return model;
        }
        #endregion
    }

    public class AddressIM
    {
        public Guid AddressId { get; set; } = Guid.Empty;

        [Required(ErrorMessage = "Please input a street address")]
        [RegularExpression("^[^<>\\/\"\'@]*$", ErrorMessage = "Street address may not contain special characters < > \\ / \" \' @")]
        [StringLength(200, MinimumLength = 4, ErrorMessage = "Street address must contain between 4 and 200 characters")]
        public string StreetAddress { get; set; }

        [Required(ErrorMessage = "Please input a zip code")]
        [Range(10000, 99999, ErrorMessage = "Zip code must be a number between 10000 and 99999")]
        public int? ZipCode { get; set; }

        [Required(ErrorMessage = "Please input a city")]
        [RegularExpression("^[^<>\\/\"\'@]*$", ErrorMessage = "City may not contain special characters < > \\ / \" \' @")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "City must contain between 1 and 200 characters")]
        public string City { get; set; }

        [Required(ErrorMessage = "Please input a country")]
        [RegularExpression("^[^<>\\/\"\'@]*$", ErrorMessage = "Country may not contain special characters < > \\ / \" \' @")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Country must contain between 1 and 200 characters")]
        public string Country { get; set; }

        #region Constructors and updater
        public AddressIM() { }

        public AddressIM(IAddress original)
        {
            AddressId = original.AddressId;
            StreetAddress = original.StreetAddress;
            ZipCode = original.ZipCode;
            City = original.City;
            Country = original.Country;
        }

        public AddressIM(AddressIM original)
        {
            AddressId = original.AddressId;
            StreetAddress = original.StreetAddress;
            ZipCode = original.ZipCode;
            City = original.City;
            Country = original.Country;
        }

        public csAddressCUdto UpdateModel(csAddressCUdto model)
        {
            model.StreetAddress = StreetAddress;
            model.ZipCode = ZipCode.Value;
            model.City = City;
            model.Country = Country;

            return model;
        }
        #endregion
    }

    public class QuoteIM
    {
        public IMStatus Status { get; set; } = IMStatus.Unknown;

        public Guid QuoteId { get; set; } = Guid.Empty;

        [Required(ErrorMessage = "Please input a quote")]
        [RegularExpression("^[^<>\\/\"@]*$", ErrorMessage = "Quote may not contain special characters < > \\ / \" @")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Quote must contain between 1 and 200 characters")]
        public string Quote { get; set; }

        [Required(ErrorMessage = "Please input an author")]
        [RegularExpression("^[^<>\\/\"@]*$", ErrorMessage = "Author may not contain special characters < > \\ / \" @")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Author must contain between 1 and 200 characters")]
        public string Author { get; set; }

        #region Constructors and updater
        public QuoteIM() { }

        public QuoteIM(IQuote original)
        {
            Status = IMStatus.Unchanged;
            QuoteId = original.QuoteId;
            Quote = original.Quote;
            Author = original.Author;
        }

        public QuoteIM(QuoteIM original)
        {
            Status = original.Status;
            QuoteId = original.QuoteId;
            Quote = original.Quote;
            Author = original.Author;
        }

        public IQuote UpdateModel(IQuote model)
        {
            model.QuoteId = QuoteId;
            model.Quote = Quote;
            model.Author = Author;

            return model;
        }
        #endregion
    }
    #endregion
}
