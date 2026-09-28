# Real execution setup

## 1. Start MySQL + API + web

Use the Docker Compose file or run the .NET API and React app locally.

## 2. Start the local browser agent

From `src/OneBox.Agent`:

```bash
npm install
npx playwright install chromium
set ONEBOX_AGENT_SECRET=change-this-agent-secret
set OPENAI_API_KEY=your-key
npm start
```

On PowerShell use `$env:...` instead of `set`.

The API and agent must use the same `Execution:AgentSharedSecret` / `ONEBOX_AGENT_SECRET`.

## 3. Real execution flow

1. User says what they want.
2. ONEBOX creates a task and requires confirmation.
3. User confirms.
4. User supplies/chooses the provider page.
5. Local agent opens the user's visible Chromium session.
6. Agent reads the actual provider DOM and produces an action plan.
7. Agent stops before irreversible submission.
8. User gives final confirmation.
9. Agent executes the actions and returns the resulting provider URL/data.

This is an execution system, not a mock success response.
