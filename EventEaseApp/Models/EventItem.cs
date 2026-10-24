namespace EventEaseApp.Models;

// Immutable model: editing an EventCard creates a new value and notifies its parent.
// This keeps component state changes predictable and testable.
public sealed record EventItem(
    int Id,
    string Name,
    DateTime Date,
    string Location,
    string Description);
