using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Webshop.Data;

namespace Webshop.Pages;

[Authorize(Roles = RoleSeeder.AdminRole)]
public class IndexModel : PageModel
{
    public void OnGet()
    {

    }
}
