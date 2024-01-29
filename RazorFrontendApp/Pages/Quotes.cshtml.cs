using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using Models.DTO;
using Services;
using System.ComponentModel.DataAnnotations;

namespace RazorFrontendApp.Pages
{
    public class QuotesModel : PageModel
    {
        private readonly IFriendsService _service;
        private readonly ILogger<QuotesModel> _logger;

        public List<string> UpdateMessages { get; set; } = new();
        public List<string> ErrorMessages { get; set; } = new();

        [BindProperty]
        public QuoteIM Quote { get; set; }
        public List<QuoteIM> Quotes { get; set; }

        [BindProperty]
        public bool Seeded { get; set; } = true;
        [BindProperty]
        public string Filter { get; set; } = null;
        
        public List<SelectListItem> PageSizes { get; set; } = new();

        [BindProperty]
        public int PageSize { get; set; } = 10;
        [BindProperty]
        public int CurrentPage { get; set; } = 1;
        public int TotalQuotes { get; set; } = 0;
        public int TotalPages { get; set; } = 1;
        public int DisplayedPages { get; set; } = 4;

        public async Task<IActionResult> OnGet(bool seeded, string filter, int pageSize)
        {
            try
            {
                Seeded = seeded;
                Filter = filter;
                PageSize = pageSize < 1 ? PageSize : pageSize;
                CurrentPage = 1;

                if (int.TryParse(Request.Query["page"], out int _currentPage))
                    CurrentPage = (_currentPage < 1) ? 1 : _currentPage;

                HashSet<int> pageSizes = new() { 5, 10, 20, 40, PageSize };

                PageSizes = pageSizes
                    .Select(s => new SelectListItem(s.ToString(), s.ToString()))
                    .ToList();

                TotalQuotes = await _service.CountQuotesAsync(null, Seeded, Filter, CurrentPage - 1, PageSize);
                
                TotalPages = (int)Math.Ceiling(Decimal.Divide(TotalQuotes, PageSize));
                CurrentPage = int.Max(int.Min(CurrentPage, TotalPages), 1);

                Quotes = (await _service.ReadQuotesAsync(null, Seeded, true, Filter, CurrentPage - 1, PageSize))?
                    .Select(q => new QuoteIM(q)).ToList()
                    ?? throw new Exception("Failed to retrieve quotes.");
            }
            catch (Exception ex)
            {
                ErrorMessages.Add(ex.Message);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSave()
        {
            try
            {
                if (Quote is null)
                    throw new Exception("");

                if (Quote.QuoteId == Guid.Empty)
                {
                    IQuote newQuote = (await _service.CreateQuoteAsync(null,
                        new csQuoteCUdto(
                            Quote.UpdateModel(new csQuote())
                        ) { QuoteId = null }
                        )) ?? throw new Exception("Failed to create new quote.");

                    return RedirectToPage("Quotes",
                        new
                        {
                            seeded = $"{Seeded}".ToLower(),
                            filter = Filter,
                            pageSize = PageSize,
                            page = CurrentPage
                        });
                }

                IQuote updatedQuote = await _service.ReadQuoteAsync(null, Quote.QuoteId, true)
                    ?? throw new Exception("Quote does not exist.");

                updatedQuote = (await _service.UpdateQuoteAsync(null,
                    new csQuoteCUdto(Quote.UpdateModel(updatedQuote))))
                    ?? throw new Exception("Failed to update quote.");
            }
            catch (Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
                ErrorMessages.Add(ex.Message);
            }

            return RedirectToPage("Quotes",
                new {
                    seeded = $"{Seeded}".ToLower(),
                    filter = Filter,
                    pageSize = PageSize,
                    page = CurrentPage
                });
        }

        public QuotesModel(IFriendsService service, ILogger<QuotesModel> logger)
        {
            _service = service;
            _logger = logger;
        }
    }

    #region Input models
    public enum IMStatus { Unknown, Unchanged, Inserted, Modified, Deleted }

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
    #endregion
}
