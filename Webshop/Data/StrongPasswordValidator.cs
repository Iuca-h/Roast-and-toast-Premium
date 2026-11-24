
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;

namespace Webshop.Data;

public class StrongPasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(IdentityResult.Failed(
                new IdentityError
                {
                    Description = "Password cannot be empty."
                }));
        }

        // Common weak passwords
        string weakPasswordPattern =
            @"password|qwerty|admin|welcome|letmein|iloveyou";

        if (Regex.IsMatch(password, weakPasswordPattern,
            RegexOptions.IgnoreCase))
        {
            return Task.FromResult(IdentityResult.Failed(
                new IdentityError
                {
                    Description = "Password contains a common weak pattern."
                }));
        }

        // Easy sequences of numbers, letters or keyboard characters
        string sequencePattern =
            @"012345|123456|234567|345678|456789|" +
            @"abcdef|bcdefg|cdefgh|qwerty";

        if (Regex.IsMatch(password, sequencePattern,
            RegexOptions.IgnoreCase))
        {
            return Task.FromResult(IdentityResult.Failed(
                new IdentityError
                {
                    Description = "Password contains an easy sequence."
                }));
        }

        // 5 or more repeated characters
        string repeatedPattern = @"(.)\1{4,}";

        if (Regex.IsMatch(password, repeatedPattern))
        {
            return Task.FromResult(IdentityResult.Failed(
                new IdentityError
                {
                    Description = "Password contains too many repeated characters."
                }));
        }

        // Birth date: DDMMYYYY, DD-MM-YYYY, DD.MM.YYYY, DD/MM/YYYY
        // Also checks YYYYMMDD and YYYY-MM-DD formats
        string birthDatePattern =
            @"(?:" +
                @"(?:0[1-9]|[12]\d|3[01])[-/.]?" +
                @"(?:0[1-9]|1[0-2])[-/.]?" +
                @"(?:19|20)\d{2}" +
            @"|" +
                @"(?:19|20)\d{2}[-/.]?" +
                @"(?:0[1-9]|1[0-2])[-/.]?" +
                @"(?:0[1-9]|[12]\d|3[01])" +
            @")";

        if (Regex.IsMatch(password, birthDatePattern))
        {
            return Task.FromResult(IdentityResult.Failed(
                new IdentityError
                {
                    Description = "Password cannot contain a birth date."
                }));
        }

        // Username
        if (!string.IsNullOrEmpty(user.UserName) &&
            Regex.IsMatch(password,
                Regex.Escape(user.UserName),
                RegexOptions.IgnoreCase))
        {
            return Task.FromResult(IdentityResult.Failed(
                new IdentityError
                {
                    Description = "Password cannot contain your username."
                }));
        }

        // Email name
        if (!string.IsNullOrEmpty(user.Email))
        {
            var emailName = user.Email.Split('@')[0];

            if (emailName.Length >= 3 &&
                Regex.IsMatch(password,
                    Regex.Escape(emailName),
                    RegexOptions.IgnoreCase))
            {
                return Task.FromResult(IdentityResult.Failed(
                    new IdentityError
                    {
                        Description = "Password cannot contain your email name."
                    }));
            }
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
