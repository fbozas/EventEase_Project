using EventEaseApp.Models;

namespace EventEaseApp.Services;

/// <summary>
/// Compute the filtered/sorted list only when input data or query changes;
/// Events.razor keeps the result in state instead of recomputing on every render.
/// </summary>
public static class EventSearch
{
    public static List<EventItem> Filter(IReadOnlyList<EventItem> events, string? query)
    {
        var term = query?.Trim() ?? string.Empty;
        IEnumerable<EventItem> results = events;

        if (term.Length > 0)
        {
            results = results.Where(item =>
                item.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Location.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return results.OrderBy(item => item.Date).ThenBy(item => item.Id).ToList();
    }
}
