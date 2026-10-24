using EventEaseApp.Models;

namespace EventEaseApp.Services;

/// <summary>
/// Activity 2: a single set of validation rules used in both the editable
/// component and the catalog's update boundary (defense in depth).
/// Copilot exercise: ask for edge cases, then independently review every rule.
/// </summary>
public static class EventValidation
{
    public const int MaxNameLength = 100;
    public const int MaxLocationLength = 160;

    public static string? NameError(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Event name is required." :
        value.Trim().Length > MaxNameLength ? $"Event name cannot exceed {MaxNameLength} characters." :
        null;

    public static string? LocationError(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Location is required." :
        value.Trim().Length > MaxLocationLength ? $"Location cannot exceed {MaxLocationLength} characters." :
        null;

    public static string? DateError(DateTime date) =>
        date == default ? "Please choose a valid date." :
        date.Date < DateTime.Today ? "Event date cannot be in the past." :
        null;

    public static bool IsValid(EventItem? item) => item is not null &&
        item.Id > 0 &&
        NameError(item.Name) is null &&
        LocationError(item.Location) is null &&
        DateError(item.Date) is null;
}
