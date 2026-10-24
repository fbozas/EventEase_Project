using System.ComponentModel.DataAnnotations;

namespace EventEaseApp.Models;

public sealed class RegistrationInput : IValidatableObject
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must have 2–100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(254, ErrorMessage = "Email address is too long.")]
    public string Email { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(FullName) && FullName.Trim().Length < 2)
            yield return new ValidationResult("Enter at least two non-space characters.", [nameof(FullName)]);
        if (!string.IsNullOrEmpty(Email) && Email != Email.Trim())
            yield return new ValidationResult("Remove spaces around the email address.", [nameof(Email)]);
    }
}
