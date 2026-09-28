# ONEBOX ready-to-deploy checklist

## Required
- Docker + Docker Compose
- MySQL 8.4 (or managed MySQL)
- OPENAI_API_KEY
- JWT_KEY
- ONEBOX_AGENT_SECRET
- Google Maps/Places key for location discovery
- Authorized payment gateway account if ONEBOX collects platform fees

## Provider onboarding
- Add provider hostname to `ONEBOX_ALLOWED_HOSTS`.
- Prefer an official API/OAuth connector when the provider offers one.
- Otherwise use the local browser agent with the user's authenticated session.
- Test login, search, data entry, checkout, confirmation, failure, refund/cancel and result verification.
- Do not bypass CAPTCHA/MFA/OTP/biometric checks or provider terms.

## Deploy

`docker compose up --build`

Web: `http://localhost:5173`
API: `http://localhost:5080`
Agent health: `http://127.0.0.1:8787/health`
Swagger: `http://localhost:5080/swagger`
