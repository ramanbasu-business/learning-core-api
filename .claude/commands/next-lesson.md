---
description: Resume the EF Core course - create the next lesson, or review a Fixed.cs the owner pasted
argument-hint: "[lesson number | review]"
---

Resume work on this repository using the instructions in CLAUDE.md.

1. Read CLAUDE.md fully (goal, working agreement, lesson anatomy, roadmap, Progress).
2. Inspect `lessons/` to confirm what actually exists (the Progress table may be behind).
3. Arguments: $ARGUMENTS
   - If it is a lesson number, create that lesson.
   - If it is `review` (or the owner pasted a `Fixed.cs` / output), review their solution first: correctness, same result as Naive,
     why it is faster, alternatives and pitfalls. Do not rewrite their file unless asked.
   - If empty, create the next lesson marked NEXT in the Progress table.
4. Follow the guided-mode rules: README + Naive.cs + Program.cs + csproj + a `Fixed.cs` stub; the owner writes the fix.
5. Add the project to `learning-core-api.slnx`, update the root README roadmap row, and update the Progress table in CLAUDE.md.
6. Remember you cannot compile here; keep code simple and say clearly what the owner should run.
