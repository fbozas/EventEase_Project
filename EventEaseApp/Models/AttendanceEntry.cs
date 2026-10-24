namespace EventEaseApp.Models;

// Tracks one browser-tab session's participation, not a global attendance count.
public sealed record AttendanceEntry(int EventId, bool CheckedIn);
