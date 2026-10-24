"""Dependency-free source/contract checks; these do NOT replace dotnet build or browser tests."""
from pathlib import Path
import json
import re
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / 'EventEaseApp'
def read(rel): return (ROOT / rel).read_text(encoding='utf-8')

class FinalContractTests(unittest.TestCase):
    def test_solution_structure(self):
        for f in ('EventEase.sln', 'EventEaseApp/App.razor', 'EventEaseApp/Pages/Registration.razor',
                  'EventEaseApp/Pages/Attendance.razor', 'EventEase.Checks/Program.cs'):
            self.assertTrue((ROOT / f).is_file(), f)

    def test_csproj_xml_and_framework(self):
        for f in ('EventEaseApp/EventEaseApp.csproj', 'EventEase.Checks/EventEase.Checks.csproj'):
            root = ET.parse(ROOT / f).getroot()
            self.assertEqual(root.find('./PropertyGroup/TargetFramework').text, 'net10.0')

    def test_no_problematic_date_binding_regression(self):
        src = read('EventEaseApp/Components/EventCard.razor')
        self.assertIn('type="date"',src)
        self.assertIn('@onchange="ChangeDate"',src)
        self.assertIn('DateTime.TryParseExact',src)
        self.assertNotIn('@bind:get="DateInput"',src)
        self.assertNotIn('@bind:set="ChangeDate"',src)

    def test_registration_form_validation(self):
        s = read('EventEaseApp/Pages/Registration.razor')
        for fragment in ('<EditForm ', '<DataAnnotationsValidator />', 'OnValidSubmit="SubmitRegistration"',
                         'OnInvalidSubmit="HandleInvalidSubmit"', '<ValidationMessage', 'Session.RegisterAsync'):
            self.assertIn(fragment, s)
        model = read('EventEaseApp/Models/RegistrationInput.cs')
        for fragment in ('[Required(', '[EmailAddress(', 'IValidatableObject', 'FullName', 'Email'):
            self.assertIn(fragment,model)

    def test_registration_never_persists_pii(self):
        s=read('EventEaseApp/Services/SessionState.cs')
        snapshot=read('EventEaseApp/Models/SessionSnapshot.cs')
        self.assertNotRegex(snapshot, r'\b(FullName|Email)\b')
        self.assertNotIn('localStorage',s)
        self.assertIn('sessionStorage.setItem',s)
        self.assertIn('sessionStorage.getItem',s)
        self.assertNotIn('registration.FullName',s)
        self.assertNotIn('registration.Email',s)

    def test_registration_idempotence_and_session_update(self):
        src=read('EventEaseApp/Services/AttendanceLedger.cs')
        for fragment in ('TryAdd(eventId, false)', 'CheckIn(int eventId)', 'Cancel(int eventId)', 'Restore(', 'MaximumEntries'):
            self.assertIn(fragment,src)
        session=read('EventEaseApp/Services/SessionState.cs')
        for fragment in ('Changed?.Invoke()', 'RestoreAsync()', 'await SaveAsync()', 'IsReady = true'):
            self.assertIn(fragment,session)

    def test_session_scoped_and_header(self):
        self.assertIn('AddScoped<SessionState>()',read('EventEaseApp/Program.cs'))
        layout=read('EventEaseApp/Layout/MainLayout.razor')
        self.assertIn('Session.InitializeAsync()',layout)
        self.assertIn('Session.Changed += OnSessionChanged',layout)
        self.assertIn('Session.Changed -= OnSessionChanged',layout)
        self.assertIn('href="/attendance"',layout)

    def test_attendance_page(self):
        page=read('EventEaseApp/Pages/Attendance.razor')
        for fragment in ('@page "/attendance"', 'Check in', 'Cancel registration', 'Session.RegisteredCount', 'Session.CheckedInCount'):
            self.assertIn(fragment,page)

    def test_routing_404(self):
        self.assertIn('NotFoundPage=', read('EventEaseApp/App.razor'))
        self.assertIn('@page "/not-found"',read('EventEaseApp/Pages/NotFound.razor'))
        self.assertIn('Event not found',read('EventEaseApp/Pages/EventDetails.razor'))
        self.assertIn('Event not found',read('EventEaseApp/Pages/Registration.razor'))

    def test_large_list_optimizations(self):
        s=read('EventEaseApp/Pages/Events.razor')
        for fragment in ('PageSize = 24', 'filteredEvents.Take(visibleCount)', '@key="item.Id"',
                         'Load 2,000 demo events', 'EventSearch.Filter'):
            self.assertIn(fragment,s)

    def test_browser_storage_json_hydration(self):
        s=read('EventEaseApp/Services/SessionState.cs')
        self.assertIn('JsonSerializer.Deserialize<SessionSnapshot>',s)
        self.assertIn('catch (JsonException)',s)
        self.assertIn('catch (JSException)',s)
        snapshot=read('EventEaseApp/Models/SessionSnapshot.cs')
        self.assertIn('List<AttendanceEntry>? Entries',snapshot)

    def test_build_ci_and_static_deploy(self):
        s=read('.github/workflows/build.yml')
        self.assertIn('dotnet build EventEase.sln',s)
        self.assertIn('EventEase.Checks',s)
        self.assertIn('dotnet publish',s)
        config=json.loads(read('EventEaseApp/wwwroot/staticwebapp.config.json'))
        self.assertEqual(config['navigationFallback']['rewrite'],'/index.html')

    def test_no_external_extra_dependencies(self):
        s=read('EventEaseApp/EventEaseApp.csproj')
        pkgs=re.findall(r'<PackageReference Include="([^"]+)"',s)
        self.assertEqual(pkgs,[
            'Microsoft.AspNetCore.Components.WebAssembly',
            'Microsoft.AspNetCore.Components.WebAssembly.DevServer'])

    def test_docs(self):
        for name in ('README.md','TESTING.md','PERFORMANCE.md','REFLECTION.md'):
            self.assertTrue((ROOT/name).stat().st_size > 200)

if __name__ == '__main__': unittest.main()
