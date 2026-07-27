## Context

The DataBank tools are fully implemented under `DatabankTool/` but lack documentation. The CLI tool (`dv-extract`), the WPF+WebView2 desktop frontend, and the ASP.NET Core Web API all have no README or developer-facing docs. The only usage information for the CLI is inline `--help` text in Program.cs. The PROJECT_CONTEXT.md incorrectly lists Tool 2 as "Not started" even though it's complete. Developers need comprehensive documentation to understand the tools' capabilities, usage, and architecture.

## Goals / Non-Goals

**Goals:**
- Create `DatabankTool/README.md` as an entry point for the entire DatabankTool directory.
- Create a comprehensive README.md for the DataBank CLI tool at `DatabankTool/DataBank.Cli/README.md`.
- Create documentation for the Desktop app at `DatabankTool/DataBank.Desktop/README.md`.
- Document all CLI flags, supported formats, output schema, and workflows.
- Explain architecture, parser details, locale detection, coverage analysis, and extension points for the CLI.
- Document the Desktop app's build process, run instructions, and WPF+WebView2 architecture.
- Note that API documentation is provided via Swagger/OpenAPI within `DatabankTool/DataBank.Api/`.
- Update PROJECT_CONTEXT.md to reflect accurate implementation status.

**Non-Goals:**
- Changing any code or tool behavior.
- Adding new features or parsers.
- Creating Swagger/OpenAPI documentation (handled by the API project itself).
- Writing detailed API endpoint documentation (covered by Swagger in the API project).

## Decisions

### Documentation location — DatabankTool directory structure
**Decision:** Place documentation across the DatabankTool directory:
- `DatabankTool/README.md` — top-level overview of all sub-projects
- `DatabankTool/DataBank.Cli/README.md` — CLI tool documentation
- `DatabankTool/DataBank.Desktop/README.md` — Desktop app documentation
- `DatabankTool/DataBank.Api/` — API docs via Swagger (no separate README needed)

**Rationale:** Documentation lives close to the code it describes. The top-level README provides a navigation entry point. Each sub-project README is self-contained.
**Alternatives considered:** Single monolithic docs file, but that would be hard to maintain and navigate.

### Documentation structure — CLI README
**Decision:** Use a single README.md with sections following the spec requirements.
**Rationale:** Keeps all CLI documentation in one place, easy to maintain. For a CLI tool, a single README is typical.
**Alternatives considered:** Multiple files (e.g., USAGE.md, ARCHITECTURE.md), but that adds unnecessary complexity.

### Desktop documentation scope
**Decision:** Focus on build/run instructions, architecture overview, and development setup. Do not document internal WPF implementation details.
**Rationale:** The primary audience is developers who want to build, run, or contribute to the Desktop app. Internal implementation details belong in code comments.
**Alternatives considered:** Full internal architecture docs, but that would be too detailed for a README.

### API documentation strategy
**Decision:** Rely on Swagger/OpenAPI generated within the API project. Add a brief note in the top-level README pointing to the API's Swagger endpoint.
**Rationale:** Swagger is the standard for REST API documentation and is auto-generated from code annotations. Duplicating it in a README would be redundant and quickly become outdated.
**Alternatives considered:** Writing API docs manually in a README, but that defeats the purpose of having Swagger.

### Update PROJECT_CONTEXT.md
**Decision:** Modify the existing PROJECT_CONTEXT.md in place.
**Rationale:** This is the single source of truth for project status. Updating it corrects misinformation.
**Alternatives considered:** Creating a separate status document, but that would fragment the truth.

### Documentation style
**Decision:** Write for developers who are new to the tools but familiar with localization concepts and .NET development.
**Rationale:** Target audience is developers who need to use or extend DataBank tools.
**Alternatives considered:** Writing for end-users (translators), but the tools are developer-facing.

### Example data
**Decision:** Use realistic examples based on actual file formats (resx, rc, fhx, ahc).
**Rationale:** Helps developers understand real-world usage.
**Alternatives considered:** Using generic placeholders, but that would be less helpful.

## Risks / Trade-offs

**Risk:** Documentation may become outdated as tools evolve.
→ **Mitigation:** Keep documentation close to code (same directory), update as part of code changes.

**Risk:** Over-documenting may make READMEs too long.
→ **Mitigation:** Use clear section headings, concise language, and examples to convey information efficiently.

**Risk:** Incorrect documentation could mislead developers.
→ **Mitigation:** Verify all information against actual implementation. Test examples before finalizing.

**Risk:** Updating PROJECT_CONTEXT.md may conflict with other updates.
→ **Mitigation:** Make minimal, focused changes to status and description only.

**Risk:** Desktop app docs may drift if the UI changes frequently.
→ **Mitigation:** Keep Desktop README focused on stable aspects (build, run, architecture) rather than UI details.

**Trade-off:** Swagger vs. manual API docs. Swagger is auto-generated and always current, but requires running the API server to view. Acceptable since API docs are primarily for developers who will be running the service anyway.
