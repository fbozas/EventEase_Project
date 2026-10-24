using System.Diagnostics;
using EventEaseApp.Models;
using EventEaseApp.Services;

int count = 0;
int failures = 0;
void Check(string name, bool pass)
{
    count++;
    if (!pass) failures++;
    Console.WriteLine($"{(pass ? "PASS" : "FAIL")} {name}");
}

var nextMonth = DateTime.Today.AddMonths(1);
var valid = new EventItem(9, "Good title", nextMonth, "Athens", "Test");
Check("valid event passes", EventValidation.IsValid(valid));
Check("blank name rejected", !EventValidation.IsValid(valid with { Name = "  " }));
Check("blank location rejected", !EventValidation.IsValid(valid with { Location = "\t" }));
Check("missing date rejected", !EventValidation.IsValid(valid with { Date = default }));
Check("past date rejected", !EventValidation.IsValid(valid with { Date = DateTime.Today.AddDays(-1) }));
Check("overlong name rejected", !EventValidation.IsValid(valid with { Name = new string('x', 101) }));
Check("overlong location rejected", !EventValidation.IsValid(valid with { Location = new string('y', 161) }));
Check("invalid id rejected", !EventValidation.IsValid(valid with { Id = 0 }));

var catalog = new EventCatalog();
Check("base events present", catalog.GetAll().Count == 4);
Check("unknown ID returns null", catalog.FindById(999) is null);
Check("catalog refuses invalid update", !catalog.Update(new EventItem(1, "", nextMonth, "Athens", "")));
Check("invalid update preserved old event", catalog.FindById(1)?.Name == "Future of Work Summit");
var amended = catalog.FindById(1)! with { Name = "  Updated title  ", Date = nextMonth };
Check("valid update succeeds", catalog.Update(amended));
Check("normalizes whitespace", catalog.FindById(1)?.Name == "Updated title");

var matches = EventSearch.Filter(catalog.GetAll(), "  thESSaloniki  ");
Check("case-insensitive search", matches.Count == 1 && matches[0].Id == 2);
Check("empty search returns all", EventSearch.Filter(catalog.GetAll(), " ").Count == 4);
Check("unmatched search returns empty list", EventSearch.Filter(catalog.GetAll(), "not a place").Count == 0);
var sorted = EventSearch.Filter(catalog.GetAll(), null);
Check("sort order stable", sorted.SequenceEqual(sorted.OrderBy(e => e.Date).ThenBy(e => e.Id)));

catalog.LoadDemoEvents(2_000);
Check("load 2000 demo events", catalog.GetAll().Count == 2_004);
Check("demo ID navigable", catalog.FindById(10_000)?.Name == "Demo Event 0001");
catalog.LoadDemoEvents(2_000);
Check("demo load is idempotent", catalog.GetAll().Count == 2_004);
catalog.ClearDemoEvents();
Check("demo clear restores four", catalog.GetAll().Count == 4);
Check("demo ID removed", catalog.FindById(10_000) is null);
Check("new catalog resets in-memory edits", new EventCatalog().FindById(1)?.Name == "Future of Work Summit");

// Activity 3: valid/invalid forms and attendance state transitions.
Check("valid registration", RegistrationRules.IsValid(new RegistrationInput { FullName = "Alex Morgan", Email = "alex@example.org" }));
Check("missing name rejected", !RegistrationRules.IsValid(new RegistrationInput { FullName = "", Email = "alex@example.org" }));
Check("whitespace name rejected", !RegistrationRules.IsValid(new RegistrationInput { FullName = "  ", Email = "alex@example.org" }));
Check("invalid email rejected", !RegistrationRules.IsValid(new RegistrationInput { FullName = "Alex Morgan", Email = "not-email" }));
Check("oversized email rejected", !RegistrationRules.IsValid(new RegistrationInput { FullName = "Alex Morgan", Email = new string('z', 250) + "@example.org" }));

var ledger = new AttendanceLedger();
Check("empty participation state", ledger.RegisteredCount == 0 && ledger.CheckedInCount == 0);
Check("invalid event id ignored", !ledger.Register(0));
Check("first registration works", ledger.Register(1));
Check("duplicate registration ignored", !ledger.Register(1) && ledger.RegisteredCount == 1);
Check("check-in requires registration", !ledger.CheckIn(99));
Check("check-in works", ledger.CheckIn(1) && ledger.IsCheckedIn(1) && ledger.CheckedInCount == 1);
Check("duplicate check-in ignored", !ledger.CheckIn(1) && ledger.CheckedInCount == 1);
Check("can register second event", ledger.Register(2));
Check("restore retains distinct status", ledger.Entries.Count == 2);
var otherLedger = new AttendanceLedger();
otherLedger.Restore([new AttendanceEntry(1, true), new AttendanceEntry(1, false), new AttendanceEntry(-1, true), new AttendanceEntry(2, false)]);
Check("restore skips duplicate and invalid ids", otherLedger.RegisteredCount == 2);
Check("restore preserves checked in", otherLedger.IsCheckedIn(1) && !otherLedger.IsCheckedIn(2));
var snapshot = new SessionSnapshot { SessionId = "ABC12345", Entries = otherLedger.Entries.ToList() };
var snapshotJson = System.Text.Json.JsonSerializer.Serialize(snapshot);
var parsedSnapshot = System.Text.Json.JsonSerializer.Deserialize<SessionSnapshot>(snapshotJson);
Check("JSON session snapshot round-trips", parsedSnapshot?.SessionId == "ABC12345" && parsedSnapshot?.Entries?.Count == 2);
Check("snapshot JSON contains no attendee PII", !snapshotJson.Contains("Email", StringComparison.OrdinalIgnoreCase) && !snapshotJson.Contains("FullName", StringComparison.OrdinalIgnoreCase));
Check("cancel removes registration", ledger.Cancel(1) && !ledger.IsRegistered(1) && ledger.CheckedInCount == 0);
Check("cannot cancel twice", !ledger.Cancel(1));

Console.WriteLine($"\n{count - failures}/{count} .NET logic checks passed.");
if (failures > 0) return 1;
if (args.Contains("--benchmark", StringComparer.OrdinalIgnoreCase))
    RunBenchmark();
return 0;

static void RunBenchmark()
{
    // Synthetic comparison of repeated render-time selection, not actual DOM timing.
    // Both code paths use exactly the same deterministic 2000+ dataset and query.
    var catalog = new EventCatalog();
    catalog.LoadDemoEvents(2_000);
    var all = catalog.GetAll();
    const string query = "Demo";
    const int renders = 250;
    const int pageSize = 24;

    static List<EventItem> OldFilter(IReadOnlyList<EventItem> data, string query) =>
        data.Where(e => e.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         e.Location.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.Date).ToList();

    var oldResult = OldFilter(all, query);
    var newResult = EventSearch.Filter(all, query);
    if (!oldResult.Select(e => e.Id).SequenceEqual(newResult.Select(e => e.Id)))
        throw new InvalidOperationException("Benchmark queries returned different event IDs.");

    // Warm up JIT and collections before measurement.
    for (int n = 0; n < 10; n++) { _ = OldFilter(all, query); _ = EventSearch.Filter(all, query); }

    int checksum = 0;
    var watch = Stopwatch.StartNew();
    long baselineAlloc = GC.GetAllocatedBytesForCurrentThread();
    for (var i = 0; i < renders; i++)
        checksum += OldFilter(all, query).Count;
    watch.Stop();
    double baselineMs = watch.Elapsed.TotalMilliseconds;
    baselineAlloc = GC.GetAllocatedBytesForCurrentThread() - baselineAlloc;

    watch.Restart();
    long optimizedAlloc = GC.GetAllocatedBytesForCurrentThread();
    var cached = EventSearch.Filter(all, query);
    for (var i = 0; i < renders; i++)
        checksum += cached.Take(pageSize).Count();
    watch.Stop();
    double optimizedMs = watch.Elapsed.TotalMilliseconds;
    optimizedAlloc = GC.GetAllocatedBytesForCurrentThread() - optimizedAlloc;

    Console.WriteLine($"\nSynthetic selection benchmark: {all.Count} events, {renders} repeated UI selections");
    Console.WriteLine($"Activity 1 style: {baselineMs:F3} ms; allocated {baselineAlloc:N0} bytes");
    Console.WriteLine($"Activity 2 cached + paged: {optimizedMs:F3} ms; allocated {optimizedAlloc:N0} bytes");
    Console.WriteLine($"Ratio for selection work only: {(optimizedMs > 0 ? baselineMs / optimizedMs : double.NaN):F1}x");
    Console.WriteLine("Note: isolated selection/allocations only; NOT a browser render or end-to-end benchmark.");
    GC.KeepAlive(checksum);
}
