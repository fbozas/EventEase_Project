using System.Text.Json;
using EventEaseApp.Models;
using Microsoft.JSInterop;

namespace EventEaseApp.Services;

/// <summary>
/// IMPORTANT: stores only a random demo session label and event IDs/check-in flags.
/// It deliberately does NOT persist names, email addresses, or credentials.
/// Not a login system, server-side booking store, or global attendance tracker.
/// </summary>
public sealed class SessionState(IJSRuntime js)
{
    private const string StorageKey = "eventease.session.v1";
    private readonly AttendanceLedger _ledger = new();
    private Task? _initialization;
    private string _sessionId = NewSessionId();

    public event Action? Changed;
    public bool IsReady { get; private set; }
    public bool StorageAvailable { get; private set; } = true;
    public string SessionId => _sessionId;
    public int RegisteredCount => _ledger.RegisteredCount;
    public int CheckedInCount => _ledger.CheckedInCount;
    public IReadOnlyList<AttendanceEntry> Entries => _ledger.Entries;
    public bool IsRegistered(int eventId) => _ledger.IsRegistered(eventId);
    public bool IsCheckedIn(int eventId) => _ledger.IsCheckedIn(eventId);

    public Task InitializeAsync() => _initialization ??= RestoreAsync();

    private async Task RestoreAsync()
    {
        try
        {
            var json = await js.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(json) && json.Length < 64_000)
            {
                var saved = JsonSerializer.Deserialize<SessionSnapshot>(json);
                if (saved is not null)
                {
                    if (!string.IsNullOrWhiteSpace(saved.SessionId) &&
                        saved.SessionId.Length is >= 6 and <= 24 &&
                        saved.SessionId.All(char.IsLetterOrDigit))
                        _sessionId = saved.SessionId;
                    _ledger.Restore(saved.Entries);
                }
            }
        }
        catch (JSException)
        {
            // Private browsing / browser policy may disable storage.
            StorageAvailable = false;
        }
        catch (JsonException)
        {
            // Corrupted or obsolete browser data: start with a clean session.
            _ledger.Restore(null);
        }
        finally
        {
            IsReady = true;
            Changed?.Invoke();
        }
    }

    public async Task<bool> RegisterAsync(int eventId)
    {
        await InitializeAsync();
        if (!_ledger.Register(eventId)) return false;
        await SaveAsync();
        Changed?.Invoke();
        return true;
    }

    public async Task<bool> CheckInAsync(int eventId)
    {
        await InitializeAsync();
        if (!_ledger.CheckIn(eventId)) return false;
        await SaveAsync();
        Changed?.Invoke();
        return true;
    }

    public async Task<bool> CancelAsync(int eventId)
    {
        await InitializeAsync();
        if (!_ledger.Cancel(eventId)) return false;
        await SaveAsync();
        Changed?.Invoke();
        return true;
    }

    private async Task SaveAsync()
    {
        if (!StorageAvailable) return;
        try
        {
            var saved = new SessionSnapshot { SessionId = _sessionId, Entries = _ledger.Entries.ToList() };
            await js.InvokeVoidAsync("sessionStorage.setItem", StorageKey, JsonSerializer.Serialize(saved));
        }
        catch (JSException)
        {
            StorageAvailable = false; // State is still retained in memory until refresh.
        }
    }

    private static string NewSessionId() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

}
