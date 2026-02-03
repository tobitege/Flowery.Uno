# Agent Rules

## Agent Behavior

### CRITICAL: THE 5-ITERATION RULE (Communication Before Scope Changes)

**THIS IS NON-NEGOTIABLE. VIOLATION OF THIS RULE IS UNACCEPTABLE.**

**The Problem This Solves**: The AI often starts a targeted fix, then mid-execution decides "this is too complex, let me simplify/rewrite this" — and proceeds to rewrite several hundred lines of code the user never asked to touch. This creates massive diffs, introduces regressions, and destroys context.

**The Rule**: During multi-tool inference (not chat with user), if an approach isn't working after **5 consecutive attempts**, the AI MUST:

1. **STOP making unsupervised changes**
2. **Report what was attempted** and why it's not working
3. **ASK the user** before trying a different approach or expanding scope
4. **NEVER silently change direction** - approach/scope changes require user awareness

**Clarification (Scope of This Rule)**: This rule is about **unsupervised approach changes during the AI's own multi-tool execution**. It does **NOT** limit interactive back-and-forth with the user. If the user is engaged and responding, keep working, but still **explicitly communicate** any approach/scope change before doing it.

**Specifically FORBIDDEN behaviors**:

- Silently deciding "this code is messy, let me refactor it while I'm here"
- Expanding from a 10-line fix to a 200-line rewrite **without informing the user first**
- Switching fundamental approaches (e.g., Grid → Canvas, inheritance → composition) mid-execution **without asking**
- "Simplifying" code by rewriting large sections the user didn't request

**What this rule does NOT prevent**:

- Continuing to help when the user is actively engaged in conversation
- Discussing options and alternatives with the user
- Making changes the user explicitly requests or approves
- Asking clarifying questions
- **Fixing root causes in other files** — but TELL the user first (e.g., "The bug in `DaisyButton` is caused by logic in `DaisyControlExtensions`. I'll need to fix it there. Proceed?")

**Reconciling "Fix Root Cause" with "Minimal Changes"**:

These are NOT contradictory. "Fix root cause" means don't apply band-aids. "Minimal changes" means don't refactor unrelated code.

- ✅ **DO**: Fix the root cause, even if it spans multiple files — just mention what you're doing (e.g., "Fixing this in `DaisyButton.cs` and `DaisyControlExtensions.cs`")
- ✅ **DO**: Make the minimal change needed in each file
- ❌ **DON'T**: Refactor unrelated code "while you're there"
- ❌ **DON'T**: Expand a bug fix into an architecture overhaul without discussion

**When to pause and ask** (not for every file, but for significant scope changes):

- The fix grew from ~10 lines to ~200+ lines
- You're about to rewrite a core abstraction or change a fundamental approach
- The "fix" would touch files completely unrelated to the reported issue

**Rationale**: The user asked for X. Deliver X. If X seems hard and Y seems easier, **ASK** before switching to Y.

**What counts as an "attempt"**:

- Each code modification targeting the same issue = 1 attempt
- User feedback/approval resets the counter (user is now involved in the decision)
- Scope expansion without **informing the user** = immediate violation (no 3-attempt grace period)
- If the user is actively engaged in conversation, continue normally; the "3 attempts" threshold only applies to **unsupervised** approach changes.

---

### Subagents

- ALWAYS wait for all subagents to complete before yielding.
- Spawn subagents automatically when:
- Parallelizable work (e.g., install + verify, npm test + typecheck, multiple tasks from plan)
- Long-running or blocking tasks where a worker can run independently.
- Isolation for risky changes or checks

---

### CRITICAL: NEVER REVERT OR SWITCH APPROACHES WITHOUT ASKING

**THIS IS NON-NEGOTIABLE. VIOLATION OF THIS RULE IS UNACCEPTABLE.**

When a solution is not working as expected:

1. **DO NOT revert code** without explicit user approval
2. **DO NOT switch to a different approach** without explicit user approval
3. **DO NOT "try another thing"** - ASK FIRST what the user wants to do
4. **STOP and present options** - Let the user decide the path forward

**Examples of FORBIDDEN behavior:**

- NO: "The PNGs aren't loading, let me switch back to SVGs" (without asking)
- NO: "This approach has issues, I'll try a different one" (without asking)
- NO: "Let me revert this change since it's not working" (without asking)

**Required behavior:**

- YES: "The PNGs aren't loading. Options: A) Switch to SVGs, B) Fix PNG deployment, C) Something else. What do you prefer?"
- YES: "This isn't working as expected. Should I revert or try to fix it?"
- YES: "I see Issue X. Before I change anything, what's your preference?"

**Rationale**: The user may have additional context, may prefer to debug the current approach, or may want to test on other platforms first. Unilateral reversions waste time and destroy progress.

---

### "No Change" Is a Valid Outcome

When the user reports an issue or asks for investigation **without specifying a required change**:

1. **Investigate first** - Understand the current behavior and why it exists
2. **Assess whether a change is needed** - Sometimes the behavior is correct/intentional
3. **If no obvious solution exists, ASK** - Do NOT go on a "coding spree" trying random fixes
4. **Report findings and recommend** - Present options including "no change" if appropriate

**Rationale**: It is better to confirm with the user before making changes than to introduce unnecessary code churn, potential regressions, or deviations from established design patterns. The user may have context that makes "no change" the correct answer.

**Example scenarios where "no change" may be correct**:

- Behavior follows an established design system (e.g., DaisyUI patterns)
- The "issue" is theme-specific and other themes work correctly
- A workaround already exists (e.g., using a different variant/property)
- The behavior is intentional and documented

---

### Output Messaging

- Do not include boilerplate lines like "Tests not run" in responses unless the user explicitly asks about tests or requests test execution status.

---

### Avoid Redundant Full-Code Reproduction

When reporting changes, do NOT paste full code snippets that are already applied unless the user explicitly asks for them. Keep responses concise and reference the file/path only when needed.

---

### CRITICAL: Do NOT merge Uno.Toolkit.* Generic.xaml

**Uno.Toolkit.* packages do not ship `Styles/Generic.xaml`.** Adding `ResourceDictionary Source="ms-appx:///uno.toolkit.*/Styles/Generic.xaml"` will fail to load resources and can crash on startup. ShadowContainer only needs the package reference; do **not** add toolkit Generic.xaml to merged dictionaries or runtime loaders.

### Neumorphic Takeaways

For the distilled, field-tested fixes and integration notes from recent stability work, see `llms-static/neumorphic.md` → **Field-Tested Integration Notes (Session Takeaways)**.

### Pitfalls (Session)

These are compile-time issues that should be avoided up front:

- Do not use `??` with different operand types (e.g., `Border ?? DaisyCard`). Cast to a shared base like `FrameworkElement` first.
- Do not assign `UIElement` to `FrameworkElement` without an explicit cast and a null/type check.
- Do not reference non-existent WinUI/Uno members like `FrameworkElement.IsVisibleChanged`; use supported events or `RegisterPropertyChangedCallback`.
- Do not access internal or private helpers (e.g., `PlatformCompatibility`) from outside their assembly.
- Do not set `null` into non-nullable reference types; update the type or use a nullable value.
- Do not use `x:Bind` paths that are not real properties on the page (e.g., `Localization`); ensure the property exists and follow the required localization binding pattern.

- **Think Before You Code**: Before making any code change, trace through the FULL execution flow. For control/UI changes specifically:
  1. When are properties set? (Construction time vs. after construction)
  2. Do property changes need callbacks to propagate to child elements?
  3. What is the initialization order? (Constructor → property setters → Loaded event)
  4. Are there async/deferred events that fire after synchronous code completes?
  
  **Do NOT make partial fixes and iterate.** Analyze the complete picture first, then implement the full solution in one pass.

- **No Flip-Flopping on Approaches**: When the user asks to try a specific approach, commit to that approach fully and let them test it. Don't second-guess midway and switch to an alternative solution. If the first approach doesn't work, the user will provide feedback and we can discuss alternatives together.

### Tool Usage Selection

> **Note**: Different AI agents (Cursor, Windsurf, Copilot, etc.) have different tool names. These guidelines describe *intent*, not specific tool names. Adapt to your agent's available tools.

- **Prefer Native File Tools Over Terminal**: Use your agent's built-in file/directory/search tools instead of terminal commands (`ls`, `dir`, `find`, `cat`) whenever possible. Native tools provide better structured output and avoid shell escaping issues.

- **Use Terminal ripgrep for Complex Search**: For complex content searching with regex, context lines, or glob filtering, use terminal `rg` (ripgrep). To avoid output truncation in long results, prefix with `clear`:

  ```bash
  clear && rg "pattern" path/to/search
  ```

  Examples:
  - Search for function: `clear && rg "function_name" .`
  - Search in specific files: `clear && rg "pattern" --glob "*.ps1"`
  - Case insensitive: `clear && rg -i "pattern" .`
  - Search for exact string: `clear && rg -F "string" .`

### Search Tool Quirks (Agent-Specific)

Some agents' search tools have bugs with single-file paths. If your search returns 0 results unexpectedly:

1. Try searching a **directory** instead of a single file
2. Use a glob/filter parameter to narrow to the specific file
3. Fall back to terminal `rg` if the native tool misbehaves

**Example of the bug pattern** (tool names vary by agent):

```text
# BAD - Returns 0 results even though "Style" appears 130+ times:
search(pattern="Style", path="d:/github/Project/Themes/File.xaml")
# Result: "No results found"

# GOOD - Same query works when using directory + filter:
search(pattern="Style", path="d:/github/Project/Themes", glob="File.xaml")
# Result: 50+ matches returned correctly
```

**Workaround**: Always use a **directory** as the search path and use a glob/filter to narrow to specific files:

- ❌ `path="path/to/file.cs"` → May return 0 results
- ✅ `path="path/to"`, `glob="file.cs"` → Works reliably

### Git Bash CRLF Garbled Output Issue (Windows)

> **Context**: Most agents provide native file-reading tools that handle CRLF automatically. However, when terminal text tools are unavoidable (e.g., `sed` for in-place regex replacement, `head`/`tail` for quick line extraction), this rule applies.

**RULE: When using `sed`, `cat`, `head`, `tail`, or similar text tools on Windows files, ALWAYS pipe through `| tr -d '\r'` to strip carriage returns. Otherwise output will be garbled.**

Example:

```bash
sed -n '100,110p' file.cs | tr -d '\r'
```

The `\r` (carriage return) in CRLF line endings causes the cursor to jump back to the start of the line, making subsequent text overwrite previous content and producing unreadable output.

**When to use terminal text tools**:

- ✅ Complex regex replacements that native edit tools can't handle
- ✅ Quick line-range extraction when you know exact line numbers
- ❌ Reading entire files (use native file tools)
- ❌ Simple find-and-replace (use native edit tools)

### Git Bash Windows Path Handling (Windows)

**RULE: When using terminal tools (like `rg`, `sed`, `find`) in Git Bash on Windows, AVOID absolute paths with backslashes (e.g., `d:\path\to\file`).**

The shell or tools will often misinterpret backslashes as escape characters, leading to "file not found" errors (e.g., `d:\github` becomes `d:github`).

**Correct usage:**

1. **Use relative paths** with forward slashes: `rg "pattern" folder/file.cs`
2. **Set the Working Directory** (`Cwd`) to the base directory of your search.
3. If you MUST use absolute paths, use forward slashes: `/d/github/Flowery.Uno/...` (Git Bash style) or `d:/github/Flowery.Uno/...`.

### PowerShell Best Practices (Windows)

- **Encoding Discipline**: Always pass `-Encoding utf8` and prefer `-Raw` when you need exact file content; this avoids cp1252 output errors and line-splitting surprises.
- **Python Unicode Output**: If a Python script prints Unicode, use `python -X utf8` or set `$env:PYTHONIOENCODING = "utf-8"` before running it.
- **ASCII-Only Checks**: Scan for both non-ASCII (>0x7F) and control chars (<0x20 excluding tab/CR/LF); a simple `[^\x00-\x7F]` regex is not enough.
- **Hidden Control Chars**: If a patch fails to match a line, suspect invisible control characters and replace by codepoint via script instead of manual edits.

---

## Thorough Refactoring & Coding Standards

To prevent regressions and compilation errors during complex refactors, follow these strict verification steps:

- **Strict Property & Field Sync**: When renaming or deleting a property, field, or method, you MUST perform a file-wide search (using your agent's search tool or terminal `rg`) for all references and update them. Do not rely on memory or "obvious" guesses about where a variable is used.
- **Verify Infrastructure**: Before calling a constructor or accessing a collection/field, verify its definition in the current file. Do NOT assume the existence of convenience constructors (e.g., `new MyItem(id, name)`) or private tracking collections (e.g., `_itemLabels`) without explicit proof from reading the file.
- **Method Signature Uniqueness**: When introducing a new method or overload (e.g., `RegisterItemLabelUpdate`), check the entire file for existing signatures to avoid `CS0111` (duplicate member) errors.
- **Atomic State Integrity**: When replacing a construction pattern (e.g., moving from a `StackPanel` to a custom control), ensure ALL secondary logic attached to the old elements—such as hover transitions, selection colors, and event handlers—is correctly migrated to the new structure.
- **Constructor vs. Property Initializers**: If a class does not have a matching constructor, always use C# property initializers `{ Prop1 = value, Prop2 = value }` instead of assuming positional parameters.

## WinUI Clipboard Usage

### Text Clipboard (Cross-platform)

For text, use WinUI's built-in clipboard:

```csharp
var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
dataPackage.SetText(textContent);
Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
```

### Image Clipboard (Windows-only)

For images on Windows, use WinUI clipboard with bitmap data:

```csharp
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

private async Task SetImageClipboardAsync(byte[] pngBytes)
{
    if (pngBytes == null || pngBytes.Length == 0) return;
    
    var stream = new InMemoryRandomAccessStream();
    await stream.WriteAsync(pngBytes.AsBuffer());
    stream.Seek(0);
    
    var dataPackage = new DataPackage();
    dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));
    Clipboard.SetContent(dataPackage);
}
```

### Cross-Platform Project Setup

When a project needs platform-specific clipboard behavior (e.g. WinForms on Windows) but must remain buildable on other platforms:

1. **Conditional TFM** in `.csproj`:

   ```xml
   <TargetFramework Condition="$([MSBuild]::IsOSPlatform('Windows'))">net8.0-windows</TargetFramework>
   <TargetFramework Condition="!$([MSBuild]::IsOSPlatform('Windows'))">net8.0</TargetFramework>
   ```

2. **Conditional WinForms** in `.csproj`:

   ```xml
   <PropertyGroup Condition="$([MSBuild]::IsOSPlatform('Windows'))">
     <UseWindowsForms>true</UseWindowsForms>
     <DefineConstants>$(DefineConstants);WINDOWS</DefineConstants>
   </PropertyGroup>
   ```

3. **Conditional compilation** in code:

```csharp
#if WINDOWS
if (OperatingSystem.IsWindows())
{
    await SetImageClipboardAsync(pngBytes);
}
else
#endif
{
    // Fallback: save to temp file, copy path to clipboard
    var tempPath = Path.Combine(Path.GetTempPath(), "screenshot.png");
    await File.WriteAllBytesAsync(tempPath, pngBytes);
    
    var dataPackage = new DataPackage();
    dataPackage.SetText(tempPath);
    Clipboard.SetContent(dataPackage);
}
```

**Key points:**

- Use `#if WINDOWS` preprocessor directives to guard Windows-specific code
- Mark Windows-specific methods with `[SupportedOSPlatform("windows")]`
- Always provide a fallback for non-Windows platforms

## Localization & Translation Policy

- **Consult `LOCALIZATION.md` FIRST**: When dealing with ANY localization task (XAML bindings, JSON translation files, adding new strings), you MUST read and follow the patterns in `LOCALIZATION.md`.
- **WinUI/Uno XAML Binding (Required)**: Use `{Binding [KeyName]}` with `DataContext="{x:Bind Localization, Mode=OneWay}"` (or `Source={x:Bind Localization}`) for localization. Do NOT use `{x:Bind Localization[...]}` because the WinUI XamlCompiler rejects indexer syntax (WMC1110). This rule applies to all Uno heads (Windows, Desktop/Skia, Android, iOS, WASM).
- **Desktop/Skia Runtime Switching (Required)**: If the app supports runtime language switching, you MUST force a rebinding after culture changes (e.g., set the `Localization` `DataContext` to `null` and back). Desktop/Skia does not always refresh indexer bindings from `INotifyPropertyChanged`.
- **Do NOT Translate Technical Terms**: Control names (e.g., *Checkbox*, *NumericUpDown*), variant names (e.g., *Primary*, *Ghost*, *Secondary*), size names (e.g., *Extra Small*, *Large*), and code enums/literals MUST remain in their original English form across all translation files (`*.json`). Only descriptive labels, instructional text, and placeholders should be localized.

## Analyzer and ReSharper Hygiene

- "ScrollViewer.VerticalScrollChainingMode" does NOT EXIST!
- **IGNORE CA1822 for XAML-bound members**: The analyzer suggests "Member does not access instance data and can be marked as static" (CA1822), but `x:Bind` in XAML generates code like `this.PropertyName`. Making such members `static` causes CS0176 build errors. **Never apply CA1822 to properties, methods, or event handlers used in `x:Bind` or XAML event bindings.**
- Keep `using` directives minimal; remove any that are unused after edits; check for existing `GlobalUsings.cs`
- Avoid redundant default initializers (e.g., `= false` on `bool`, `= 0` on numeric) unless it changes behavior.
- Prefer concise pattern checks (`is not null`, property patterns) over separate null/type checks that trigger "merge into pattern" suggestions.
- When a null check is only guarding a type test, use a single type pattern (`obj is SomeType foo`) instead of `obj is not null` + type check.
- Use `is { }` when you only need to assert non-null, and prefer property/collection patterns (e.g., `Panel { Children: [var child] }`) instead of manual null/Count/index checks.
- Keep XML doc `<param>` tags in sync with method signatures whenever you add or change parameters.
- Avoid introducing unused helpers; if a helper must exist for future use, add a local suppression directive and a short justification.
- Match nullability on event handlers (`object? sender`, `RoutedEventArgs e`) to avoid nullability warnings.
- Use the narrowest visibility (usually `private`) for helpers unless the API is intended for external use.
- Avoid capturing outer variables in local functions, lambdas, or nested types; pass values explicitly to constructors or method parameters to prevent "captured variable" warnings.
- Omit redundant generic type arguments and let the compiler infer them to avoid "type argument specification is redundant" warnings.
