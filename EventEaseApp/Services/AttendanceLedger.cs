using EventEaseApp.Models;

namespace EventEaseApp.Services;

/// <summary>
/// Pure in-memory state transitions, separated from browser storage for easy testing.
/// A person may register once per event, check in once, or cancel participation.
/// This is a single-tab demo, not a cross-user attendance service.
/// </summary>
public sealed class AttendanceLedger
{
    public const int MaximumEntries = 500;
    private readonly Dictionary<int, bool> _entries = new();

    public int RegisteredCount => _entries.Count;
    public int CheckedInCount => _entries.Values.Count(value => value);
    public IReadOnlyList<AttendanceEntry> Entries => _entries
        .OrderBy(pair => pair.Key)
        .Select(pair => new AttendanceEntry(pair.Key, pair.Value))
        .ToArray();

    public bool IsRegistered(int eventId) => _entries.ContainsKey(eventId);
    public bool IsCheckedIn(int eventId) => _entries.TryGetValue(eventId, out var value) && value;

    public bool Register(int eventId) =>
        eventId > 0 && _entries.Count < MaximumEntries && _entries.TryAdd(eventId, false);

    public bool CheckIn(int eventId)
    {
        if (!_entries.TryGetValue(eventId, out var checkedIn) || checkedIn)
            return false;
        _entries[eventId] = true;
        return true;
    }

    public bool Cancel(int eventId) => _entries.Remove(eventId);

    public void Restore(IEnumerable<AttendanceEntry>? saved)
    {
        _entries.Clear();
        if (saved is null) return;
        foreach (var entry in saved.Take(MaximumEntries))
        {
            if (entry is not null && entry.EventId > 0)
                _entries.TryAdd(entry.EventId, entry.CheckedIn);
        }
    }
}
