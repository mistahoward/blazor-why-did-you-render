# WhyDidYouRender for Blazor SSR – Architecture Document

## 1. High-Level Goals

**Purpose:**
- Provide developers with insight into when, why, and how their Blazor components render, including initial renders, re-renders, and manual triggers.
- Help identify unnecessary renders to optimize performance and developer experience.

**Scope:**
- Track all renders in Blazor SSR, including those triggered by lifecycle methods (`OnInitialized`, `OnParametersSet`, `OnAfterRender`) and manual `StateHasChanged` calls.
- Focus on server-side rendering (SSR) scenarios, where all rendering logic executes on the server.

**Outcome:**
- Actionable diagnostics and reporting to help developers reduce unnecessary renders and improve app performance.

---

## 2. Blazor Component Lifecycle Analysis

**Relevant Lifecycle Methods:**
- `OnInitialized` / `OnInitializedAsync`: Called when the component is initialized. Useful for tracking the very first render.
- `OnParametersSet` / `OnParametersSetAsync`: Called when parent component parameters are set or updated. Can trigger re-renders.
- `OnAfterRender` / `OnAfterRenderAsync`: Called after the component has rendered. Useful for tracking post-render logic.
- Manual `StateHasChanged` calls: Developers can force a re-render at any time by calling this method.

**SSR Considerations:**
- All rendering occurs on the server, so lifecycle events are executed in the server context.
- The timing and frequency of lifecycle events may differ from client-side Blazor.
- Multiple users and sessions may be active concurrently, so tracking must be session-aware.

---

## 3. Technical Approach for Tracking

**Monkey-Patching Equivalent in Blazor:**
- Direct prototype monkey-patching (as in React) is not possible in C#.
- Use a custom base class (e.g., `TrackedComponentBase` inheriting from `ComponentBase`) that overrides lifecycle methods and `StateHasChanged`.
- Alternatively, use source generators or reflection to inject tracking logic, or attributes for opt-in tracking.

**Tracking Logic:**
- In the base class, override each relevant lifecycle method and `StateHasChanged`.
- Before/after calling the base implementation, log or report the render event, capturing context (component name, method, etc.).
- Ensure minimal performance overhead and no interference with normal component behavior.

---

## 4. Data Collection & Reporting

**Data to Collect:**
- Component name/type (for identification)
- Render type (initial, rerender, manual)
- Triggering method (e.g., `OnInitialized`, `OnAfterRender`, `StateHasChanged`)
- Parameter changes (if possible, compare previous and current values)
- Call stack (optional, for advanced diagnostics)
- Timestamp and session/user context (important for SSR)

**Reporting Mechanisms:**
- Server-side logging (console, file, or structured logs)
- Optional: diagnostics endpoint or developer UI overlay (for local/dev environments)
- Configurable verbosity (info, warning, debug)

---

## 5. Integration Strategy

**Opt-In/Opt-Out:**
- Default: Opt-in via base class or attribute (developers inherit from `TrackedComponentBase` or decorate with `[WhyDidYouRender]`).
- Optionally, provide a global switch to track all components for debugging purposes.

**Non-Intrusive:**
- Ensure tracking logic is excluded or disabled in production builds (e.g., via configuration or compiler directives).
- Minimal performance overhead when enabled; zero overhead when disabled.

---

## 6. SSR-Specific Challenges

**Server Context:**
- All tracking and reporting must work in the server-side pipeline.
- Must handle concurrent users and sessions; include session/user info in logs for context.

**Reporting:**
- Make logs accessible to developers (e.g., via diagnostics endpoint, log files, or real-time dashboards).
- Consider privacy and security for any user/session data included in reports.

---

## 7. Extensibility & Configuration

**Configuration Options:**
- Enable/disable tracking globally or per component.
- Filter out noisy components (by name, namespace, etc.).
- Adjust verbosity (info, warning, debug).
- Configure via `appsettings.json`, environment variables, or code-based options.

**Extensibility:**
- Allow custom hooks or callbacks for advanced users (e.g., to integrate with external monitoring tools).
- Provide extension points for custom data collection or reporting.

---

## 8. Main Components & Implementation Plan

**Core Classes:**
- `TrackedComponentBase`: Inherits from `ComponentBase`, overrides lifecycle methods and `StateHasChanged` to inject tracking logic. Located in the `Tracking` folder for organization.
- `RenderTrackerService`: Handles data collection, reporting, and configuration management. Also located in the `Tracking` folder.
- Optional: `WhyDidYouRenderAttribute`: Attribute for opt-in tracking on a per-component basis.

**Implementation Steps:**
1. Create `TrackedComponentBase` and override relevant lifecycle methods and `StateHasChanged`.
2. Implement `RenderTrackerService` for data collection and reporting.
3. Add configuration options (via `appsettings.json`, environment variables, or code).
4. Integrate with the sample app for testing and validation.
5. Add extensibility hooks and diagnostics endpoint (optional, for advanced scenarios).
6. Document usage, configuration, and integration steps for developers.

---

## Summary

This architecture provides a robust, extensible, and developer-friendly render tracking system for Blazor SSR, inspired by React's Why Did You Render. It leverages Blazor's component model and lifecycle, provides flexible integration and configuration, and is mindful of SSR-specific challenges. The outlined approach ensures minimal performance impact, easy opt-in, and actionable diagnostics for developers. 