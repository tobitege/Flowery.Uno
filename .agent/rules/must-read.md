---
activation: always
description: Critical development rules that must always be followed
---

# Must Read

## ⚠️ Git Bash CRLF Rule

When using `sed`, `cat`, `head`, `tail` on Windows files, **ALWAYS** pipe through `| tr -d '\r'`:

```bash
sed -n '100,110p' file.cs | tr -d '\r'
```

Otherwise output will be garbled due to CRLF line endings.

## "No Change" Is Valid

When investigating issues without a specified fix, ASK before coding. Do not go on a "coding spree" - the behavior may be intentional.

## 3-Iteration Rule Scope

The 3-iteration rule is about **unsupervised approach changes during the AI's own multi-tool execution**. It does **not** limit interactive back-and-forth with the user. When the user is engaged, continue normally, but still **ask before changing approach/scope**.

## grep_search File Path Bug

`grep_search` returns 0 results when `SearchPath` is a single file. **Workaround**: Use directory + `Includes`:

- ❌ `SearchPath="path/to/file.cs"` → Returns 0 results
- ✅ `SearchPath="path/to"`, `Includes=["file.cs"]` → Works

## Terminal Search

Prefer terminal `rg` (ripgrep) over internal `grep_search`:

```bash
clear && rg "pattern" path/to/search
```

## Code Generation

- **No Trailing Whitespace**: Never generate lines with trailing spaces or tabs

## Refactoring Standards

- **Property/Field Sync**: When renaming/deleting, search file-wide for ALL references
- **Verify Infrastructure**: Check constructors/fields exist before using them
- **Constructor Fallback**: Use property initializers `{ Prop = value }` if no matching constructor
