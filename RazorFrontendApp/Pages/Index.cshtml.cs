using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services;
using Models.DTO;

namespace RazorFrontendApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public void OnGet() {

        }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }
    }
}
