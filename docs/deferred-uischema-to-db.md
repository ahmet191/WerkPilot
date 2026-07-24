# Deferred: move UiSchema from code to the database

**Status:** deferred (flagged in P3, not implemented). This is the last big
piece of "self-service configuration" — the one intentionally left for its own
plan because it is the riskiest.

## What exists today

`UiSchemaController` (~583 lines) serves, in code, the schema that drives the
entire dynamic UI: for each entity type (`tickets`, `assets`, `departments`, …)
it returns the grid columns, form fields, filterability flags, and data-source
endpoints that `DynamicPage` / `DynamicFormModal` render. Changing any screen's
shape today requires a code change and a deploy.

Two related engines were already moved to the DB in earlier phases — menus
(`AppMenu`) and interface text (`TranslationOverride`). UiSchema is the remaining
in-code one, and it is the largest and most central: nearly every list and form
in the app reads from it, so a regression here is highly visible.

## Goal

Let a tenant reshape its own screens (add/remove/reorder columns and fields,
toggle filterability, rename labels) at runtime, without a deploy — the same
"shipped defaults in code, tenant overrides in the DB, merged at request time"
pattern already proven for translations.

## Suggested approach

1. **Keep the shipped defaults in code.** Extract the current per-entity schemas
   out of `UiSchemaController` into a `BuiltInUiSchemas` provider (mirrors the
   `BuiltInTranslations` extraction). No behaviour change — pure move + tests.
2. **`UiSchemaOverride` entity + migration.** Store only what a tenant changes,
   keyed `(TenantId, EntityType)`, as a JSONB document (the schema shape is
   already JSON-ish). Global tenant query filter applies as usual.
3. **`IUiSchemaService` with caching**, mirroring `ITranslationService`: return
   `builtIn(entityType)` merged with the tenant override, cached per tenant with
   invalidation on write.
4. **Admin surface.** A schema editor page (reuse the enterprise grid + a JSON/
   form editor) behind a `ui-schema:manage` permission, plus revert-to-default.
5. **Migration + seed.** `UiSchemaOverride` table; no seed needed (defaults are
   code). Verify every existing screen renders byte-identical from the merged
   path before shipping.

## Why it's risky (why it earns its own plan)

- **Blast radius:** every list and form reads UiSchema. A subtle merge bug breaks
  many screens at once, not one.
- **Contract coupling:** the grid/form components depend on exact field shapes
  (`Filterable`, `DataSourceEndpoint`, dropdown option sources). The merge must
  preserve them precisely.
- **Verification cost:** correctness means snapshotting the rendered schema for
  every entity type before/after and diffing — a real test-writing effort.

## Rough size

Medium-large: ~1 focused day. Low *conceptual* risk (the translations pattern is
proven) but high *surface area*, so it wants dedicated verification rather than
being folded into an unrelated change.
