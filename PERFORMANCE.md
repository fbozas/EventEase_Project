# Performance / optimization notes

## What was retained from Activity 2

- Compute `EventSearch.Filter` only on query, demo load, or demo clear; don't repeat sorting/filtering on every render.
- Render 24 event cards initially. Each **Load more** adds 24 cards with stable `@key` values.
- `EventCatalog` indexes events by ID to avoid linear searches during route detail lookup.
- Session attendance updates use a keyed dictionary (`AttendanceLedger`), avoiding repeated list scans for the registered/check-in status.
- No extra client libraries: built-in ASP.NET Core Blazor and `System.Text.Json`.

## Local measurements to take

1. With browser DevTools → Performance, record load/interaction timing for 4 original events.
2. Click **Load 2,000 demo events** (2,004 total). Only 24 should be instantiated initially. Record frames / scripting / time to interaction.
3. Click **Load more** once (48 cards). Record the difference.
4. Run `dotnet run --project EventEase.Checks -- --benchmark` for a synthetic, non-browser comparison of repeated selection logic.

| Measurement | Baseline | Optimized | Notes |
| --- | --- | --- | --- |
| Synthetic C# selection time | _Measure locally_ | _Measure locally_ | C# console runner, excludes DOM |
| Browser initial 2,004-event display | _Measure locally_ | _Measure locally_ | Use DevTools, initial optimized DOM 24 cards |
| Browser one Load More (48 visible) | _Measure locally_ | _Measure locally_ | Use DevTools |

Do not claim a speedup until measurements have been recorded on the same machine/browser and roughly equivalent dataset. The synthetic benchmark compares code paths and is **not** an end-to-end browser measurement.
