# Code Style Guide

## Context

Global code style rules for Agent OS projects.

<conditional-block context-check="general-formatting">
IF this General Formatting section already read in current context:
  SKIP: Re-reading this section
  NOTE: "Using General Formatting rules already in context"
ELSE:
  READ: The following formatting rules

## General Formatting

> **⚠️ IMPORTANT**: Language-specific style guides (e.g., dotnet-style.md, javascript-style.md) **OVERRIDE** these general rules. Always follow the language-specific guide for your current task. The rules below apply only when no language-specific guide exists.

### Indentation

- Use language-idiomatic indentation (2 spaces for JS/TS, 4 spaces for C#, etc.)
- See language-specific guides for exact standards
- Maintain consistent indentation throughout files
- Align nested structures for readability

### Naming Conventions

- **Follow language-specific conventions** (see language guides below)
- General fallback when no specific guide exists:
  - **Methods and Variables**: Use snake_case (e.g., `user_profile`, `calculate_total`)
  - **Classes and Modules**: Use PascalCase (e.g., `UserProfile`, `PaymentProcessor`)
  - **Constants**: Use UPPER_SNAKE_CASE (e.g., `MAX_RETRY_COUNT`)
- **.NET/C# projects**: Use PascalCase for methods/properties, camelCase for private fields (see dotnet-style.md)
- **JavaScript/TypeScript projects**: Use camelCase for methods/variables (see javascript-style.md)

### String Formatting

- **Follow language-specific conventions** (see language guides below)
- General fallback:
  - Use single quotes for strings: `'Hello World'`
  - Use double quotes only when interpolation is needed
  - Use template literals for multi-line strings or complex interpolation
- **.NET/C# projects**: Use double quotes for all strings (see dotnet-style.md)

### Code Comments

- Add brief comments above non-obvious business logic
- Document complex algorithms or calculations
- Explain the "why" behind implementation choices
- Never remove existing comments unless removing the associated code
- Update comments when modifying code to maintain accuracy
- Keep comments concise and relevant
</conditional-block>

<conditional-block task-condition="html-css-tailwind" context-check="html-css-style">
IF current task involves writing or updating HTML, CSS, or TailwindCSS:
  IF html-style.md AND css-style.md already in context:
    SKIP: Re-reading these files
    NOTE: "Using HTML/CSS style guides already in context"
  ELSE:
    <context_fetcher_strategy>
      IF current agent is Claude Code AND context-fetcher agent exists:
        USE: @agent:context-fetcher
        REQUEST: "Get HTML formatting rules from code-style/html-style.md"
        REQUEST: "Get CSS and TailwindCSS rules from code-style/css-style.md"
        PROCESS: Returned style rules
      ELSE:
        READ the following style guides (only if not already in context):
        - @.agent-os/standards/code-style/html-style.md (if not in context)
        - @.agent-os/standards/code-style/css-style.md (if not in context)
    </context_fetcher_strategy>
ELSE:
  SKIP: HTML/CSS style guides not relevant to current task
</conditional-block>

<conditional-block task-condition="javascript" context-check="javascript-style">
IF current task involves writing or updating JavaScript:
  IF javascript-style.md already in context:
    SKIP: Re-reading this file
    NOTE: "Using JavaScript style guide already in context"
  ELSE:
    <context_fetcher_strategy>
      IF current agent is Claude Code AND context-fetcher agent exists:
        USE: @agent:context-fetcher
        REQUEST: "Get JavaScript style rules from code-style/javascript-style.md"
        PROCESS: Returned style rules
      ELSE:
        READ: @.agent-os/standards/code-style/javascript-style.md
    </context_fetcher_strategy>
ELSE:
  SKIP: JavaScript style guide not relevant to current task
</conditional-block>

<conditional-block task-condition="dotnet-csharp" context-check="dotnet-style">
IF current task involves writing or updating .NET/C# code:
  IF dotnet-style.md already in context:
    SKIP: Re-reading this file
    NOTE: "Using .NET/C# style guide already in context"
  ELSE:
    <context_fetcher_strategy>
      IF current agent is Claude Code AND context-fetcher agent exists:
        USE: @agent:context-fetcher
        REQUEST: "Get .NET/C# style rules from code-style/dotnet-style.md"
        PROCESS: Returned style rules
      ELSE:
        READ: @.agent-os/standards/code-style/dotnet-style.md
    </context_fetcher_strategy>
ELSE:
  SKIP: .NET/C# style guide not relevant to current task
</conditional-block>
