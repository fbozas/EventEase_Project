using EventEaseApp.Models;

namespace EventEaseApp.Services;

/// <summary>
/// A dictionary makes detail lookup and updates O(1); a stable list supports
/// ordered browsing and the optional large-data demo.
/// </summary>
public sealed class EventCatalog
{
    private const int DemoIdStart = 10_000;
    private readonly List<EventItem> _events =
    [
        new(1, "Future of Work Summit", new DateTime(2027, 3, 18), "Athens · Technopolis",
            "A full day of ideas, conversations and practical workshops shaping modern teams."),
        new(2, "Design & Digital Expo", new DateTime(2027, 4, 9), "Thessaloniki · HELEXPO",
            "Explore emerging digital experiences with creators, designers and innovators."),
        new(3, "Community Makers Meetup", new DateTime(2027, 5, 22), "Athens · Impact Hub",
            "A welcoming gathering for organizers, founders and community builders."),
        new(4, "Summer Networking Social", new DateTime(2027, 6, 12), "Piraeus · Marina Zeas",
            "Connect with peers and collaborators at our relaxed summer evening event.")
    ];
    private readonly Dictionary<int, int> _indexById;

    public EventCatalog() => _indexById = _events
        .Select((item, index) => (item.Id, index))
        .ToDictionary(pair => pair.Id, pair => pair.index);

    public IReadOnlyList<EventItem> GetAll() => _events;

    public EventItem? FindById(int id) => _indexById.TryGetValue(id, out var index)
        ? _events[index] : null;

    public bool Update(EventItem value)
    {
        // Never trust UI-only validation: reject invalid inputs at the data boundary.
        if (!EventValidation.IsValid(value) || !_indexById.TryGetValue(value.Id, out var index))
            return false;
        _events[index] = value with { Name = value.Name.Trim(), Location = value.Location.Trim() };
        return true;
    }

    public bool HasDemoEvents => _indexById.ContainsKey(DemoIdStart);

    public void LoadDemoEvents(int count = 2_000)
    {
        if (HasDemoEvents || count < 1 || count > 10_000) return;
        for (var i = 0; i < count; i++)
        {
            var item = new EventItem(
                DemoIdStart + i,
                $"Demo Event {i + 1:0000}",
                new DateTime(2027, 1, 1).AddDays(i % 365),
                $"Demo City {(i % 50) + 1}",
                "Generated sample event for list performance testing.");
            _indexById[item.Id] = _events.Count;
            _events.Add(item);
        }
    }

    public void ClearDemoEvents()
    {
        _events.RemoveAll(item => item.Id >= DemoIdStart);
        _indexById.Clear();
        for (var i = 0; i < _events.Count; i++)
            _indexById[_events[i].Id] = i;
    }
}
