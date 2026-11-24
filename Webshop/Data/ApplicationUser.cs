using Microsoft.AspNetCore.Identity;

namespace Webshop.Data;

public class ApplicationUser : IdentityUser
{
    public bool IsVerified { get; set; } = false;
}