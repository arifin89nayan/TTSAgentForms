# TTS Script Generation Agent — WinForms Designer version

Same app and 13-step flow as before, but the UI is now built as standard
**Visual Studio WinForms designer files**: every screen is a `UserControl`
split into three files so you can open and edit it in the visual designer.

## What's different from the code-only version

Each screen now has the classic trio:

```
ScriptInputScreen.cs            ← your logic + event handlers (partial class)
ScriptInputScreen.Designer.cs   ← InitializeComponent() — the visual layout
ScriptInputScreen.resx          ← resource file the designer needs
```

The same applies to `MainForm` (the shell) and all eight screens:
Dashboard, ScriptInput, ExtractionResult, HumanReview, DictionaryManager,
TtsList, AzureTts, VoiceCheck.

Every class has a **parameterless constructor** so the Visual Studio designer
can instantiate and render it:

```csharp
public ScriptInputScreen() { InitializeComponent(); }                       // designer uses this
public ScriptInputScreen(AppState s, Action<string> nav) : this() { ... }   // app uses this
```

## How to open a screen in the designer

1. Open `TTSAgent.csproj` in Visual Studio 2022.
2. In Solution Explorer, expand `UI\Screens`.
3. Double-click any `*.cs` (e.g. `ScriptInputScreen.cs`) — or right-click →
   **View Designer**. The form opens on the design surface; drag controls,
   change properties, add new ones. Saving updates the `.Designer.cs`.

> Tip: controls are named by role — `txtProject`, `cmbVoice`, `btnExtract`,
> `dgvTerms`, `lblStatus`, etc. — so the Properties window and event wiring
> read clearly.

## Build & run

1. Fill in `appsettings.json` (`LlmApiKey`, `AzureSpeechKey`,
   `AzureSpeechRegion`) — or set the matching environment variables. Set the
   file's **Copy to Output Directory** to "Copy if newer".
2. Press F5. The shell opens on **Script Input**.
3. Import `sample_fixed_list.csv` on the **Dictionary** screen so there's a
   Fixed List to match against, then follow the flow: Script Input → Kanji
   Review → Human Review → TTS Script → Azure TTS → Voice Check.

The UI and all offline parts (dictionary, list merge, SSML preview) run with no
keys; keys are only needed for the live extraction, audio synthesis, and the
STT quality check.

## Notes

- Layout (positions, sizes, fonts, button colors) lives in the `.Designer.cs`
  files. A little runtime styling that's awkward in the designer — DataGridView
  header colors and the left-nav buttons — is applied in code (`Theme.cs` /
  `MainForm.cs`). This is normal and keeps the designer files clean.
- Services and models (`Services\`, `Models\`, `Core\`) are unchanged and
  UI-independent.
- I could not compile this in my environment (no Windows/.NET there), so build
  it once in Visual Studio; the files are structured for a clean build, but if
  anything needs a tweak it'll surface on first compile.
