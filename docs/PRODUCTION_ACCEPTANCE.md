# ONEBOX production acceptance matrix

A workflow is considered **complete** only when the provider returns a verifiable result. ONEBOX never fabricates a booking/payment reference.

| Capability | Implementation | External dependency |
|---|---|---|
| Natural-language task planning | .NET + OpenAI structured output | OPENAI_API_KEY |
| Nearby discovery | Google Places API (New) | GOOGLE_MAPS_API_KEY + billing |
| Provider browser execution | Persistent Playwright Chromium | User logs into provider in the local agent browser |
| Movie/event handoff | BookMyShow provider pack | Provider account; provider checkout/auth |
| Rail booking | IRCTC provider pack | IRCTC account; provider authentication/payment |
| UPI checkout | Payment gateway abstraction | ONEBOX merchant/PG onboarding |
| Platform fee | Quote engine: provider amount + ONEBOX fee | Merchant/payment-gateway agreement |
| Sensitive authentication | Kept inside provider/UPI app | User enters OTP/PIN/biometric where required |

## Test acceptance

1. Register/login.
2. Allow location.
3. Configure Google Places.
4. Start the local agent.
5. Ask ONEBOX for a task.
6. Choose the provider URL from the provider catalog or use the known provider pack.
7. ONEBOX opens a persistent browser profile, so provider login can be completed once and reused.
8. ONEBOX plans only reversible browser actions and stops before the final irreversible step.
9. User confirms.
10. ONEBOX executes the plan and returns the live provider URL/result.
11. For payments, the customer is redirected to an authorized merchant/payment-gateway checkout; ONEBOX never requests a bank password, UPI PIN, OTP or card PIN.

A deployment may advertise a provider as "supported" only after that provider's live flow has been tested and its result verification is enabled.

## Transaction integrity checks

The smoke test at `scripts/smoke.sh` verifies the critical path:

- registration
- natural-language task creation
- explicit confirmation
- same-task execution
- completed state
- repeat confirmation does not execute twice

Run the stack in deterministic test mode with `ONEBOX_EXECUTION_MODE=sandbox docker compose up --build`, then execute `scripts/smoke.sh`.

Live mode intentionally refuses to fabricate external provider success. A real provider must be connected through an authorized connector or the authenticated browser agent.
