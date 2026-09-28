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
1. Copy `.env.example` to `.env` and set strong `JWT_KEY` and `ONEBOX_AGENT_SECRET` values.
2. Set `EXPOSE_OTP_IN_DEVELOPMENT=true` only for local development; keep it `false` for deployment.
3. Set `OPENAI_API_KEY` for AI planning and `GOOGLE_API_KEY` for nearby Places search/maps.
4. For deployment, set `PUBLIC_API_URL`, `PUBLIC_AGENT_URL`, and `CORS_ORIGINS` to the public HTTPS origins before building the web image.
5. `docker compose up --build`
6. Open `http://localhost:5173`
7. API health: `http://localhost:5080/health`
8. Swagger: `http://localhost:5080/swagger`

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

`React Web -> .NET 10 API -> AI task planner -> confirmation -> Browser Agent -> provider -> result`

The browser agent runs a persistent Playwright Chromium session in its configured runtime, inspects the live provider DOM and builds a constrained browser action plan. It does not accept arbitrary JavaScript. It stops before irreversible actions and requires a second explicit confirmation before execution.

### What is genuinely implemented

- .NET 10/C# API and MySQL persistence
- JWT authentication
- AI task planning with structured output
- live Google Places nearby search
- task state machine and audit records
- browser execution agent using Playwright
- provider-domain allowlist
- live DOM-based browser action planning
- explicit confirmation before sensitive execution
- provider result URL/data returned to the application
- React UI integration for the local agent

### Important production requirement

There is no universal consumer API for every hospital, restaurant, marketplace, courier, bank/utility biller, airline and retailer. ONEBOX therefore supports two real integration modes: authorized provider APIs/OAuth where available, and browser execution for services where the agent runtime has an authorized authenticated session. Provider-specific DOMs and anti-bot policies can change, so each production provider should be tested and maintained as a recipe/connector.


## Investor-grade acceptance rules

ONEBOX is not declared production-complete merely because the UI loads. A workflow is considered live only when:

1. The user request creates exactly one task.
2. Confirmation acts on that same task; it never creates a duplicate.
3. The provider action is executed through an authorized API/OAuth integration or an authorized authenticated browser session available to the agent runtime.
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
