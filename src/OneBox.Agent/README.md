# ONEBOX Local Execution Agent

This is the real execution component for provider sites that do not expose a public API.
It runs on the user's machine, opens the provider in a visible Chromium session, and performs a strict action plan.

## Why local execution

A cloud API cannot safely or reliably control a user's already-authenticated consumer sessions. The local agent can use the user's browser session and execute provider-specific recipes without receiving passwords.

## Safety

- The backend must obtain final user confirmation before sending a plan.
- Provider domains can be restricted with `ONEBOX_ALLOWED_HOSTS`.
- The agent accepts a small action DSL; arbitrary JavaScript is not supported.
- Payments/cancellations should be represented by a final explicit confirmation step in the UI and provider recipe.
