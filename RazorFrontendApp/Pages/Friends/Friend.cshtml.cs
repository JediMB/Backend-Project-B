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
        public List<IQuote> AvailableQuotes { get; set; }

        [BindProperty]
        public FriendIM Friend { get; set; } = null;

        [BindProperty]
        public AddressIM Address { get; set; } = null;

        [BindProperty]
        public List<QuoteIM> Quotes { get; set; } = null;

        [BindProperty]
        public List<PetIM> Pets { get; set; } = null;

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

                AvailableQuotes = new(await _service.ReadQuotesAsync(null, true, true, null, 0, 1000));
                AvailableQuotes.AddRange(await _service.ReadQuotesAsync(null, false, true, null, 0, 1000));

                if (friend.Quotes?.Count > 0)
                {
                    Quotes = friend.Quotes
                        .Select(q => new QuoteIM(q))
                        .ToList();

                    AvailableQuotes = AvailableQuotes.Where(q =>
                        Quotes.Any(qq => qq.QuoteId == q.QuoteId) is false
                    ).ToList();
                }
                
                if (friend.Pets?.Count > 0)
                {
                    Pets = friend.Pets
                        .Select(p => new PetIM(p))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
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
                _logger.LogError("{Message}", ex.Message);
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

                    Quotes = friend.Quotes.Select(q => new QuoteIM(q)).ToList();

                    Pets = friend.Pets.Select(p => new PetIM(p)).ToList();

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
                _logger.LogError("{Message}", ex.Message);
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
                    ?? throw new Exception("Trying to update a friend that does not exist."))
                {
                    AddressId = null
                };

                var updatedFriend = await _service.UpdateFriendAsync(null, friend)
                    ?? throw new Exception("Failed to update friend.");

                if (updatedFriend.Address is not null)
                    throw new Exception("Failed to remove friend's address.");

                return RedirectToPage("Friend", "updated", new { action = FormMode.edit, id = updatedFriend.FriendId });
            }
            catch (Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
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

                    Quotes = oldFriend.Quotes.Select(q => new QuoteIM(q)).ToList();

                    Pets = oldFriend.Pets.Select(p => new PetIM(p)).ToList();

                    return Page();
                }

                csAddressCUdto newAddress = Address.UpdateModel(new());

                IAddress createdAddress = await _service.CreateAddressAsync(null, newAddress)
                    ?? throw new Exception("Failed to create new address.");

                csFriendCUdto friend = new(await _service.ReadFriendAsync(null, Friend.FriendId, false)
                    ?? throw new Exception("Trying to update a friend that does not exist."))
                {
                    AddressId = createdAddress.AddressId
                };

                IFriend updatedFriend = await _service.UpdateFriendAsync(null, friend)
                    ?? throw new Exception("Failed to update friend.");

                if (updatedFriend.Address is null || updatedFriend.Address.AddressId != createdAddress.AddressId)
                    throw new Exception("Failed to add new address to friend.");

                return RedirectToPage("Friend", "updated", new { action = FormMode.edit, id = updatedFriend.FriendId});
            }
            catch (Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
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

                    Quotes = friend.Quotes.Select(q => new QuoteIM(q)).ToList();

                    Pets = friend.Pets.Select(p => new PetIM(p)).ToList();

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
                _logger.LogError("{Message}", ex.Message);
                return RedirectToPage("Friend", "error", new { action = FormMode.edit, id = Friend.FriendId, msg = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostSubmitQuotes(Guid friendId, int quoteCount)
        {
            if (friendId == Guid.Empty)
                return RedirectToPage("Friend", "error", new { action = FormMode.edit, id = friendId, msg = "Empty friend id." });

            List<string> validationKeys = new();

            int numberOfQuotes = int.Max(Quotes.Count, quoteCount);

            try
            {
                if (numberOfQuotes > 50)
                    throw new ArgumentException("Too many quotes submitted. (Max 50)");

                for (int i = 0; i < numberOfQuotes; i++)
                {
                    validationKeys.Add($"Quotes[{i}].Status");
                    validationKeys.Add($"Quotes[{i}].QuoteId");
                    validationKeys.Add($"Quotes[{i}].Quote");
                    validationKeys.Add($"Quotes[{i}].Author");
                }

                IFriend friend;

                if (IsInvalid(validationKeys.ToArray()))
                {
                    friend = await _service.ReadFriendAsync(null, Friend.FriendId, false)
                        ?? throw new Exception("Trying to update an address that does not exist.");

                    Friend = new(friend);

                    if (friend?.Address is not null)
                        Address = new(friend.Address);

                    Pets = friend.Pets.Select(p => new PetIM(p)).ToList();

                    return Page();
                }

                foreach(var quote in Quotes)
                {
                    switch (quote.Status)
                    {
                        case IMStatus.Inserted:
                            IQuote newQuote = await _service.CreateQuoteAsync(null,
                                new csQuoteCUdto(quote.UpdateModel(new csQuote())) { QuoteId = null }
                            ) ?? throw new Exception("Failed to create new quote.");

                            quote.QuoteId = newQuote.QuoteId;
                            break;

                        case IMStatus.Modified:
                            IQuote updatedQuote = await _service.ReadQuoteAsync(null, quote.QuoteId, true)
                                ?? throw new Exception("Trying to update a quote that does not exist.");

                            updatedQuote = await _service.UpdateQuoteAsync(null,
                                new csQuoteCUdto(quote.UpdateModel(updatedQuote))
                            ) ?? throw new Exception("Failed to update quote");
                            break;
                    }
                }

                csFriendCUdto friendDTO = new(await _service.ReadFriendAsync(null, Friend.FriendId, false)
                    ?? throw new Exception("Trying to update an address that does not exist."))
                {
                    QuotesId = Quotes
                        .Where(q => q.Status != IMStatus.Unknown && q.Status != IMStatus.Deleted)
                        .Select(q => q.QuoteId)
                        .ToList()
                };

                friend = await _service.UpdateFriendAsync(null, friendDTO)
                    ?? throw new Exception("Failed to update friend.");

                return RedirectToPage("Friend", "updated", new { action = FormMode.edit, id = friend.FriendId });
            }
            catch (Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
                return RedirectToPage("Friend", "error", new { action = FormMode.edit, id = friendId, msg = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostSubmitPets(Guid friendId, int petCount)
        {
            if (friendId == Guid.Empty)
                return RedirectToPage("Friend", "error", new { action = FormMode.edit, id = friendId, msg = "Empty friend id." });

            List<string> validationKeys = new();

            int numberOfPets = int.Max(Quotes.Count, petCount);

            try
            {
                if (numberOfPets > 50)
                    throw new ArgumentException("Too many pets submitted. (Max 50)");

                for (int i = 0; i < numberOfPets; i++)
                {
                    validationKeys.Add($"Pets[{i}].Status");
                    validationKeys.Add($"Pets[{i}].PetId");
                    validationKeys.Add($"Pets[{i}].Name");
                    validationKeys.Add($"Pets[{i}].Kind");
                    validationKeys.Add($"Pets[{i}].Mood");
                }

                if (IsInvalid(validationKeys.ToArray()))
                {
                    IFriend friend = await _service.ReadFriendAsync(null, Friend.FriendId, false)
                        ?? throw new Exception("Trying to update an address that does not exist.");

                    Friend = new(friend);

                    if (friend?.Address is not null)
                        Address = new(friend.Address);

                    Quotes = friend.Quotes.Select(q => new QuoteIM(q)).ToList();

                    return Page();
                }

                foreach (var pet in Pets)
                {
                    switch (pet.Status)
                    {
                        case IMStatus.Deleted:
                            IPet deletedPet = await _service.DeletePetAsync(null, pet.PetId)
                                ?? throw new Exception("Failed to delete pet.");
                            break;

                        case IMStatus.Inserted:
                            IPet newPet = pet.UpdateModel(new csPet());
                            newPet.Friend = await _service.ReadFriendAsync(null, friendId, true)
                                ?? throw new Exception("Trying to update a friend that does not exist.");

                            newPet = await _service.CreatePetAsync(null,
                                new csPetCUdto(newPet) { PetId = null }
                            ) ?? throw new Exception("Failed to create new pet.");

                            pet.PetId = newPet.PetId;
                            break;

                        case IMStatus.Modified:
                            IPet updatedPet = await _service.ReadPetAsync(null, pet.PetId, false)
                                ?? throw new Exception("Trying to update a pet that does not exist.");

                            updatedPet = await _service.UpdatePetAsync(null,
                                new csPetCUdto(pet.UpdateModel(updatedPet))
                            ) ?? throw new Exception("Failed to update pet");
                            break;
                    }
                }

                return RedirectToPage("Friend", "updated", new { action = FormMode.edit, id = friendId });
            }
            catch (Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
                return RedirectToPage("Friend", "error", new { action = FormMode.edit, id = friendId, msg = ex.Message });
            }
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
            model.Quote = Quote;
            model.Author = Author;

            return model;
        }
        #endregion
    }

    public class PetIM
    {
        public IMStatus Status { get; set; } = IMStatus.Unknown;

        public Guid PetId { get; set; } = Guid.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Please input a name")]
        [RegularExpression("^[^<>\\/\"@]*$", ErrorMessage = "Name may not contain special characters < > \\ / \" @")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must contain between 1 and 200 characters")]
        public string Name { get; set; }

        [Required]
        public enAnimalMood Mood { get; set; }
        [Required]
        public enAnimalKind Kind { get; set; }

        #region Constructors and updater
        public PetIM() { }

        public PetIM(IPet original)
        {
            Status = IMStatus.Unchanged;
            PetId = original.PetId;
            Name = original.Name;
            Mood = original.Mood;
            Kind = original.Kind;
        }

        public PetIM(PetIM original)
        {
            Status = original.Status;
            PetId = original.PetId;
            Name = original.Name;
            Mood = original.Mood;
            Kind = original.Kind;
        }

        public IPet UpdateModel(IPet model)
        {
            model.Name = Name;
            model.Mood = Mood;
            model.Kind = Kind;

            return model;
        }
        #endregion
    }
    #endregion
}
