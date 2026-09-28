# ONEBOX Real Execution Model

ONEBOX now has two execution paths:

1. **API connector** — for providers with an authorized API/OAuth integration.
2. **Local browser connector** — for consumer services where the user is already logged in locally.

The browser connector is not a fake success response. It opens Chromium, navigates to the provider, performs the declared actions, and returns the resulting provider URL/data.

## Provider recipes

A provider recipe maps a task type to:
- provider URL
- selector/value actions
- extraction/proof steps

Selectors are intentionally data-driven because provider DOMs change. Updating a recipe does not require changing the execution engine.

## Definition of done

A task is marked `COMPLETED` only when:
- the user confirmed the action,
- the execution engine returned success,
- the provider flow completed,
- a provider URL/reference or verifiable result was returned.

If a provider cannot be connected, ONEBOX reports `CONNECTOR_REQUIRED` rather than claiming success.
