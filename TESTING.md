# Activity 3 — Manual + automated tests

Run locally from repo root: `dotnet build EventEase.sln` and `dotnet run --project EventEase.Checks`, then `dotnet run --project EventEaseApp --launch-profile http`.

## Manual browser acceptance checklist

1. **Event list:** `/events` displays four starter cards and search works; open an event and return. Click **Load 2,000 demo events** and check the summary shows 2,004 records while only the first 24 components display. Use **Load more**; search again; restore four events.
2. **Event edit:** `/events/1`: clear name → inline error; clear location → error; clear date → error, Save disabled. Valid name/location/date edit updates preview and Save succeeds. F5 should reset edits to original mock data (by design).
3. **Invalid navigation:** `/events/999` and `/register/999` show 'Event not found'. A random route (`/does-not-exist`) shows friendly 404. Direct navigation and refresh of client routes require host SPA fallback.
4. **Invalid registration:** `/register/1` with blank/whitespace name, one-character name, malformed email: error message shown and registration does not complete. Valid `Alex Morgan`, `alex@example.org` → confirmation.
5. **Idempotency:** Navigate to `/register/1` again; don't see a new registration form. The existing registration is shown. `/attendance` lists event 1 and registered count 1, not 2.
6. **Check in / cancel:** `/attendance` → Check in → checked-in count 1. Check-in button disappears. Cancel registration → both counts 0 and the list becomes empty. Register again if desired.
7. **Session continuity:** Register for event 1; visit other routes; reload within the **same browser tab**; attendance persists if browser session storage is permitted. A separate tab/window should not be treated as globally shared server state. The displayed short session ID remains the same after refresh in this tab.
8. **Privacy:** Browser DevTools → Application → Session Storage → `eventease.session.v1`. Confirm it contains only random session identifier, event IDs and boolean check-in flags, **no names or emails**. `localStorage` is not used for this tracker.
9. **Disabled storage:** If storage is blocked by the browser, registration should still work until refresh and text on attendance page should warn it is in-memory only.
10. **Responsive:** On small viewport width (e.g. 375px) verify registration, attendance buttons, header nav and event cards remain usable without horizontal overflow.

## Performance

Use Browser DevTools Performance tab. Test 4 events, 2,004 events initially showing 24 cards, and after one Load More click (48 cards). Capture rendering times with DevTools; the filter/sort ms displayed by the app **excludes browser rendering**. Use `dotnet run --project EventEase.Checks -- --benchmark` for an isolated repeatable C# selection benchmark. Do not invent any times.

## Release preparation

- `dotnet build EventEase.sln -c Release`
- `dotnet run --project EventEase.Checks -c Release`
- `dotnet publish EventEaseApp/EventEaseApp.csproj -c Release -o publish`
- Deploy `publish/wwwroot` to a static host with SPA routing fallback.
- Run a keyboard navigation check and check browser console for errors.
- Confirm `.github/workflows/build.yml` is green after pushing to GitHub.

## Limitations

The demo is entirely browser-side, does not authenticate users, does not transmit email or names, and does not aggregate registrations from multiple users. A production-ready multi-user ticketing app requires server-side validation and authorization, a persistent datastore, privacy and security review, and backend-integrated attendance totals.
