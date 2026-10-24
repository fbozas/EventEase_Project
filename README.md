# EventEase — Final Capstone (Activities 1–3)

A responsive, standalone **Blazor WebAssembly (.NET 10)** demonstration project built from the **fixed Activity 2** baseline. It includes editable event cards, validation, friendly 404s, large-list rendering optimizations, a registration form, a browser-tab user session, and a per-session attendance tracker.

> **Scope:** This activity specifies a front-end. There is no authentication, server, database, payment, email delivery, global registration count, or production booking guarantee. Registrations are a client-side teaching demo only. **Names and email addresses are never written to browser storage.**

## Requirements and implementation

| Task | Files / behavior |
| --- | --- |
| Activity 1: reusable cards and bindings | `Components/EventCard.razor`, `Models/EventItem.cs` |
| Activity 2: invalid event input | `Services/EventValidation.cs`, `EventCard.razor` |
| Activity 2: unknown routes and unknown IDs | `App.razor` → `Pages/NotFound.razor`; detail/register pages |
| Activity 2: large-list improvements | `Pages/Events.razor`, cached filtering / 24 cards per batch / 2,000-demo mode |
| Activity 3: registration + validation | `Pages/Registration.razor`, `Models/RegistrationInput.cs`, `Services/RegistrationRules.cs` |
| Activity 3: session state | `Services/SessionState.cs` (DI-scoped + browser `sessionStorage`) |
| Activity 3: attendance tracker | `Services/AttendanceLedger.cs`, `Pages/Attendance.razor` |
| Verification | `EventEase.Checks` (C# logic checks + optional benchmark); `.github/workflows/build.yml` |

## Submission checklist (30 points total)

| Coursera criterion | Where to find it |
| --- | --- |
| Public GitHub repository | Publish this project's source files to your own public repository |
| Event Card & two-way binding | `EventEaseApp/Components/EventCard.razor`, `Pages/EventDetails.razor` |
| Routing & debugging | `EventEaseApp/App.razor`, `Pages/NotFound.razor`, `Pages/EventDetails.razor` |
| Validation & performance | `Services/EventValidation.cs`, `Pages/Events.razor`, `PERFORMANCE.md` |
| Registration, session & attendance | `Pages/Registration.razor`, `Services/SessionState.cs`, `Pages/Attendance.razor` |
| Copilot reflection | `REFLECTION.md` (personalize before submitting) |

## Requirements

- **.NET 10 SDK**: https://dotnet.microsoft.com/download/dotnet/10.0
- Git Bash, PowerShell, VS Code integrated terminal or any shell with the SDK on PATH.

```bash
dotnet --version
dotnet restore EventEase.sln
dotnet build EventEase.sln
dotnet run --project EventEaseApp --launch-profile http
```

Then browse to <http://localhost:5162>. Keep the terminal open while the app is running. Stop it using **Ctrl + C**.

If `dotnet` is not recognized, install the .NET 10 SDK and reopen Git Bash. If `EventEase.sln` is not found, `cd` to the extracted folder holding the solution and run the commands again.

The project preserves the **Activity 2 DateTime fix**: the date `<input type="date">` uses `value` plus `@onchange="ChangeDate"`, not `@bind:get` to a `string`, preventing the earlier `CS1503` build failure.

## App pages

- `/` or `/events` — discover events, live search, 2,000-record demo, load more.
- `/events/1` — view and edit an event, live preview, validation. This mock catalog is in memory and resets on refresh.
- `/register/1` — full name + email validation; one registration per event per tab session.
- `/attendance` — list registrations, check in, cancel participation, inspect registered/check-in counters.
- `/events/999`, `/register/999` — helpful event-not-found states.
- `/this-page-does-not-exist` — friendly 404 route.

## User session behavior / privacy

`SessionState` is a DI-scoped service that restores a random **display-only** session identifier and a list of `(eventId, checkedIn)` entries from `window.sessionStorage` once interactivity is available. It notifies subscribed components of state changes; changes are saved after each registration, check-in or cancellation.

- Navigating between routes retains the state without extra requests.
- Refreshing the same tab restores attendance when browser storage is available.
- A different tab or a new browser session is independent; closing the tab typically clears session storage.
- If storage is blocked, the UI still works in memory but refresh resets state.
- The event **catalog** intentionally remains mock/in-memory and always resets after refresh. A demo registration for a removed generated event can be cancelled on the attendance page.
- Email and name are validated and used to display the immediate confirmation, but **not persisted or transmitted**. No real user authentication or multi-user attendee tracking is implied by the session identifier.
- Avoid entering personal data you would not use for a demo; real deployments need a secure backend, privacy notice and consent before collecting attendee information.

## Verify and benchmark

```bash
# Build both projects
dotnet build EventEase.sln

# Core event validation, filtering, ledger, registration validation tests
dotnet run --project EventEase.Checks

# Synthetic selection logic benchmark (not DOM time)
dotnet run --project EventEase.Checks -- --benchmark

# Build production static assets
dotnet publish EventEaseApp/EventEaseApp.csproj -c Release -o publish
```

For interactive test scenarios and browser DevTools performance instructions, see [TESTING.md](TESTING.md) and [PERFORMANCE.md](PERFORMANCE.md).

## Production artifact / deployment

`dotnet publish` generates a **static Blazor WebAssembly** output under `publish/wwwroot`. This folder can be deployed to a static host that supports SPA fallback to `index.html` for routes such as `/events/1` and `/attendance`. A ready `wwwroot/staticwebapp.config.json` is provided for **Azure Static Web Apps** SPA fallback. The GitHub Actions CI workflow compiles, checks, publishes, and uploads a downloadable static site artifact when pushed to GitHub. The workflow **does not automatically publish** to a public URL, and no deployment has been performed from this environment.

For a subpath host (such as GitHub Pages `username.github.io/repo`), adjust `index.html` base href, routing, and fallback handling before publishing; avoid blindly uploading the root-relative build.


## Status

This package was statically checked in the generation environment. A real `dotnet build`, browser workflow test, and runtime benchmark must be run **locally** because the generating environment has no .NET SDK.
