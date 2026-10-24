using System.ComponentModel.DataAnnotations;
using EventEaseApp.Models;

namespace EventEaseApp.Services;

/// <summary>Second validation check at submission, beyond the EditForm UI validator.</summary>
public static class RegistrationRules
{
    public static bool IsValid(RegistrationInput? input)
    {
        if (input is null) return false;
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(input, new ValidationContext(input), results, true);
    }
}
