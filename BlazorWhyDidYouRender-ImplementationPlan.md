# Blazor WhyDidYouRender – Implementation Plan

This document outlines a phase-by-phase implementation plan for building the WhyDidYouRender extension for Blazor SSR. Each phase contains a checklist of actionable tasks to track progress.

---

## Phase 1: Project Setup & Scaffolding
- Set up the core library project for the extension
- Set up a sample/test Blazor SSR app (if not already present)
- Add initial documentation and architecture files

- [✔] Create/initialize the core extension project
- [✔] Ensure sample app is ready for integration
- [✔] Add architecture and implementation plan docs

---

## Phase 2: Core Tracking Infrastructure
- Implement the base class and/or attribute for tracking
- Set up the service for logging and reporting
- Ensure hooks for all relevant lifecycle methods and StateHasChanged

- [✔] Create `TrackedComponentBase` inheriting from `ComponentBase`
- [✔] Override lifecycle methods (`OnInitialized`, `OnParametersSet`, `OnAfterRender`)
- [✔] Override `StateHasChanged` to track manual renders
- [✔] Implement `RenderTrackerService` for data collection and reporting
- [✔] Add basic logging (console or file)
- [✔] Integrate tracking components into sample app for testing
- [✔] Create demo components (Counter, Home with parent-child, Weather with async)
- [✔] Verify tracking works in browser console

---

## Phase 3: Data Collection & Reporting
- Enhance data collection (component name, render type, trigger, etc.)
- Add support for parameter change detection (if feasible)
- Improve reporting (structured logs, diagnostics endpoint, etc.)

- [✔] Capture component name/type in logs
- [✔] Log render type and triggering method
- [✔] Detect and log parameter changes
- [✔] Add timestamps and session/user context to logs
- [✔] Add render performance metrics (duration tracking)
- [✔] Implement structured logging with RenderEvent record
- [ ] Implement diagnostics endpoint or advanced reporting (optional)

---

## Phase 4: Configuration & Extensibility
- Add configuration options for enabling/disabling tracking, filtering, and verbosity
- Allow opt-in/opt-out via base class, attribute, or global config
- Provide extension points for custom hooks or reporting

- [✔] Add configuration via `appsettings.json` or environment variables
- [✔] Implement component filtering (by name, namespace, etc.)
- [✔] Support adjustable verbosity levels (Minimal, Normal, Verbose)
- [✔] Add browser devtools console logging option
- [✔] Create service collection extensions for easy setup
- [✔] Implement wildcard pattern matching for filtering
- [✔] Add output destination control (Console, Browser, Both)
- [ ] Allow custom hooks/callbacks for advanced users

---

## Phase 5: SSR-Specific Enhancements
- Ensure tracking works correctly in SSR scenarios
- Handle concurrent users/sessions in logs and reporting
- Address privacy/security for any user/session data

- [ ] Test tracking in SSR pipeline
- [ ] Include session/user info in logs (where appropriate)
- [ ] Review privacy/security of collected data

---

## Phase 6: Documentation & Developer Experience
- Write clear usage documentation and integration guides
- Add samples and best practices
- Polish developer experience (DX)

- [ ] Document usage and configuration
- [ ] Provide integration steps for new/existing apps
- [ ] Add sample code and best practices
- [ ] Review and improve DX

---

## Phase 7: Testing & Validation
- Add unit and integration tests
- Validate in real-world scenarios
- Fix bugs and polish

- [ ] Write unit tests for core logic
- [ ] Add integration tests with sample app
- [ ] Validate in real SSR scenarios
- [ ] Fix bugs and finalize

---

## Phase 8: Release & Feedback
- Prepare for release (NuGet, GitHub, etc.)
- Gather feedback and iterate

- [ ] Prepare release notes and changelog
- [ ] Publish package and/or source
- [ ] Gather user feedback
- [ ] Plan for future improvements 