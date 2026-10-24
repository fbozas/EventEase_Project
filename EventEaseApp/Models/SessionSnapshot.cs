namespace EventEaseApp.Models;

// Public DTO for System.Text.Json. Contains NO personal attendee information.
public sealed class SessionSnapshot
{
    public string? SessionId { get; set; }
    public List<AttendanceEntry>? Entries { get; set; }
}
