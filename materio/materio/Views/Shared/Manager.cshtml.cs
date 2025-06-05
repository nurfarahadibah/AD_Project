using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspnetCoreMvcFull.Views.Shared
{
    [Authorize(Roles = "Manager")]
    public class ManagerModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
