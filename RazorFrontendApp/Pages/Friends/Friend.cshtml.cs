using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        public bool HasNoAddress => !(Address?.Status > IMStatus.Unknown);

        [BindProperty]
        public FriendIM Friend { get; set; } = null;

        [BindProperty]
        public AddressIM Address { get; set; } = null;

        [BindProperty]
        public FormMode Mode { get; set; } = FormMode.View;

        // Validation
        [BindProperty]
        public bool ValidateAddress { get; set; }
        public bool HasValidationErrors => InvalidKeys.Any();
        public IEnumerable<KeyValuePair<string, ModelStateEntry>> InvalidKeys { get; set; }

        public async Task<IActionResult> OnGet(string action, Guid id)
        {
            Mode = action switch
            {
                "add"   => FormMode.Add,
                "edit"  => FormMode.Edit,
                _       => FormMode.View
            };

            if (Mode == FormMode.Add)
                return Page();

            try
            {
                if (id == Guid.Empty)
                    throw new ArgumentException("No friend id given.");

                var friend = await _service.ReadFriendAsync(null, id, false);
                Friend = new(friend);

                if (friend.Address != null)
                {
                    Address = new(friend.Address);
                    ValidateAddress = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessages.Add(ex.Message);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSave()
        {
            string[] validationKeys = null;

            if (ValidateAddress is false)
            {
                validationKeys = new string[]
                {
                    "Friend.FriendId",
                    "Friend.FirstName",
                    "Friend.LastName" ,
                    "Friend.Email",
                    "Friend.Birthday"
                };
                Address = null;
            }
            
            if (IsInvalid(validationKeys))
                return Page();

            if (Address is not null)
            {
                switch(Address.Status)
                {
                    case IMStatus.Unknown:
                        csAddressCUdto newAddress = Address.UpdateModel(new csAddressCUdto());

                        IAddress createdAddress = await _service.CreateAddressAsync(null, newAddress);
                        Address.AddressId = createdAddress.AddressId;
                        Friend.AddressId = createdAddress.AddressId;

                        UpdateMessages.Add($"Added new address: {Address.StreetAddress}, {Address.ZipCode} {Address.City}, {Address.Country}");
                        Address.Status = IMStatus.Inserted;
                        break;

                    case IMStatus.Unchanged:
                    case IMStatus.Inserted:
                    case IMStatus.Modified:
                        csAddressCUdto changedAddress = new(await _service.ReadAddressAsync(null, Address.AddressId, false));
                        changedAddress = Address.UpdateModel(changedAddress);

                        IAddress response = await _service.UpdateAddressAsync(null, changedAddress);

                        if (response is null || response.AddressId != Address.AddressId)
                            throw new Exception($"Something went wrong when updating address {Address.AddressId}");

                        UpdateMessages.Add($"Updated address {Address.AddressId}");
                        Address.Status = IMStatus.Modified;
                        break;
                }
            }

            switch(Friend.Status)
            {
                case IMStatus.Unknown:
                    csFriendCUdto newFriend = Friend.UpdateModel(new csFriendCUdto());

                    await _service.CreateFriendAsync(null, newFriend);

                    UpdateMessages.Add($"Added new friend: {Friend.FirstName} {Friend.LastName} ({Friend.FriendId})");
                    Friend.Status = IMStatus.Inserted;
                    break;

                case IMStatus.Unchanged:
                case IMStatus.Inserted:
                case IMStatus.Modified:
                    csFriendCUdto changedFriend = new(await _service.ReadFriendAsync(null, Friend.FriendId, false));
                    changedFriend = Friend.UpdateModel(changedFriend);

                    IFriend response = await _service.UpdateFriendAsync(null, changedFriend);

                    if (response is null || response.FriendId != Friend.FriendId)
                        throw new Exception($"Something went wrong when updating {Friend.FirstName} {Friend.LastName} ({Friend.FriendId})");

                    UpdateMessages.Add($"Updated friend: {Friend.FirstName} {Friend.LastName} ({Friend.FriendId})");
                    Friend.Status = IMStatus.Modified;
                    break;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUndo(Guid friendId)
        {
            UpdateMessages.Add("Changes discarded");
            return await OnGet("edit", friendId);
        }

        public FriendModel(IFriendsService service, ILogger<FriendModel> logger)
        {
            _service = service;
            _logger = logger;
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
    public enum FormMode { View, Edit, Add }

    #region Input models
    public enum IMStatus { Unknown, Unchanged, Inserted, Modified, Deleted }

    public class FriendIM
    {
        public IMStatus Status { get; set; } = IMStatus.Unknown;
        public Guid FriendId { get; set; } = Guid.NewGuid();

        [Required(AllowEmptyStrings = false, ErrorMessage = "Please input a first name")]
        [RegularExpression("^[^<>\\/\"\'@]*$", ErrorMessage = "First name may not contain special characters < > \\ / \" \' @")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "First name must contain between 1 and 200 characters")]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Please input a last name")]
        [RegularExpression("^[^<>\\/\"\'@]*$", ErrorMessage = "Last name may not contain special characters < > \\ / \" \' @")]
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
            Status = IMStatus.Unchanged;
            FriendId = original.FriendId;
            FirstName = original.FirstName;
            LastName = original.LastName;
            Email = original.Email;
            Birthday = original.Birthday;
            AddressId = original.Address?.AddressId;
        }

        public FriendIM(FriendIM original)
        {
            Status = original.Status;
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
        public IMStatus Status { get; set; } = IMStatus.Unknown;
        public Guid AddressId { get; set; } = Guid.NewGuid();

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
            Status = IMStatus.Unchanged;
            AddressId = original.AddressId;
            StreetAddress = original.StreetAddress;
            ZipCode = original.ZipCode;
            City = original.City;
            Country = original.Country;
        }

        public AddressIM(AddressIM original)
        {
            Status = original.Status;
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
    #endregion
}
