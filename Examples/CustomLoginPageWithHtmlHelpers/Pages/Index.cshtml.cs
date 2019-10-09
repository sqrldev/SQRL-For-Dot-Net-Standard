using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CustomLoginPageWithHtmlHelpers.Pages
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            return Challenge("SQRL");
        }

    }
}