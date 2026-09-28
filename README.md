# ONEBOX — deployable execution platform build

ONEBOX is a personal execution agent: users describe a task in natural language, ONEBOX plans it, gathers missing information, uses permitted tools/connectors, asks for confirmation for sensitive actions, executes the supported action, and records the result.

## Stack
- ASP.NET Core 10 / C#
- EF Core + MySQL 8.4
- React + Vite
- OpenAI Responses API for agent reasoning/tool calling
- Browser geolocation + optional Google Places/Maps
- Docker Compose

## What is implemented
- Registration/login with JWT
- MySQL persistence
- Profile and location permission persistence
- Agent chat
- Deterministic fallback when no AI key is configured
- OpenAI Responses API integration when `OPENAI_API_KEY` is configured
- Task creation, task steps, task history and audit events
- Provider directory interface
- Google Places search when `GOOGLE_MAPS_API_KEY` is configured
- Sensitive-action confirmation gate
- Provider connector registry, idempotent task confirmation, execution states and recovery-required state
- Deterministic sandbox connector for end-to-end testing without real money
- Payment transaction ledger and signed webhook endpoint
- Complete frontend navigation and interactive states

## External credentials
The software cannot invent credentials belonging to Google, OpenAI, hospitals, restaurants, airlines, Amazon, payment providers, etc. Set the relevant keys in `.env`.

A provider is only marked completed when its connector returns a provider result. Sandbox mode is explicitly labelled and never represents a real booking. Live mode requires an authorized provider connector or the local authenticated-browser agent. ONEBOX never fabricates a live booking confirmation.

## Run
1. Copy `.env.example` to `.env` and set `JWT_KEY` and `OPENAI_API_KEY`.
2. If using nearby search/maps, set `GOOGLE_MAPS_API_KEY` with the required Google Maps/Places APIs enabled.
3. `docker compose up --build`
4. Open `http://localhost:5173`
5. API health: `http://localhost:8080/health`
6. Swagger: `http://localhost:8080/swagger`

For a cloud deployment, build the API image and web image separately and use a managed MySQL instance.

## Production safety
- Never put API keys in the React bundle.
- Use HTTPS.
- Replace all development passwords/secrets.
- Configure a real email/SMS/push provider before enabling those actions.
- Configure only provider connectors for which you have authorized access.

## ONEBOX 2.0 — Real Execution

The important architectural change in this version is the **Local Execution Agent** under `src/OneBox.Agent`.

### Execution architecture

`React Web -> .NET 10 API -> AI task planner -> confirmation -> Local Browser Agent -> user's visible Chromium session -> provider -> result`

The local agent can inspect the live provider DOM and build a constrained browser action plan. It does not accept arbitrary JavaScript. It stops before irreversible actions and requires a second explicit confirmation before execution.

### What is genuinely implemented

- .NET 10/C# API and MySQL persistence
- JWT authentication
- AI task planning with structured output
- live Google Places nearby search
- task state machine and audit records
- local browser execution agent using Playwright
- provider-domain allowlist
- live DOM-based browser action planning
- explicit confirmation before sensitive execution
- provider result URL/data returned to the application
- React UI integration for the local agent

### Important production requirement

There is no universal consumer API for every hospital, restaurant, marketplace, courier, bank/utility biller, airline and retailer. ONEBOX therefore supports two real integration modes: authorized provider APIs/OAuth where available, and local browser execution for services where the user is already authenticated. Provider-specific DOMs and anti-bot policies can change, so each production provider should be tested and maintained as a recipe/connector.


## Investor-grade acceptance rules

ONEBOX is not declared production-complete merely because the UI loads. A workflow is considered live only when:

1. The user request creates exactly one task.
2. Confirmation acts on that same task; it never creates a duplicate.
3. The provider action is executed through an authorized API/OAuth integration or the user's authenticated browser.
4. The provider returns a verifiable result/reference.
5. Payment is represented as a separate transaction with provider amount, ONEBOX fee, total, and external status.
6. A payment-success/booking-failure combination enters `RECOVERY_REQUIRED` rather than being reported as success.
7. OTP, CAPTCHA, UPI PIN, biometric and other user authentication stay inside the provider/payment UI.
8. Every execution attempt is auditable.
9. Repeating confirmation after completion is idempotent.
10. Provider health can be measured from persisted attempts.

### Launch modes

- `ONEBOX_EXECUTION_MODE=live`: no fake external completion. Missing provider credentials/connector causes a safe block.
- `ONEBOX_EXECUTION_MODE=sandbox`: deterministic end-to-end testing with clearly labelled sandbox receipts.

This distinction is deliberate: it lets the product be tested completely before real money or real customer transactions are enabled.
