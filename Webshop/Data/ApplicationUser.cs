using Microsoft.AspNetCore.Identity;

namespace Webshop.Data;

public class ApplicationUser : IdentityUser
{
    public bool IsVerified { get; set; } = false;

    // Personal information
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    // Saved shipping information
    public string? Country { get; set; }

    public string? City { get; set; }

    public string? StreetAddress { get; set; }

    public string? PostalCode { get; set; }

    public string? AdditionalInformation { get; set; }
}
