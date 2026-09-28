# Deployment checklist

## Local
1. Install Docker Desktop.
2. Copy `.env.example` to `.env`.
3. Set a long random `JWT_KEY`.
4. Set `OPENAI_API_KEY` for AI planning.
5. Set `GOOGLE_MAPS_API_KEY` if you want live Maps/Places.
6. Set `ADMIN_EMAILS` for the admin dashboard.
7. Run `docker compose up --build`.

## Cloud
- Build and publish `src/OneBox.Api` as a container.
- Build and publish `src/OneBox.Web` as a container.
- Use managed MySQL with TLS.
- Put secrets in the cloud secret manager, not source control.
- Put the web/API behind HTTPS.
- Restrict CORS to the production web origin.
- Configure backups, monitoring and log retention.

## External actions
Before enabling a provider connector, confirm the provider permits the integration and use its official API/authorized interface where available. Do not store third-party passwords in plain text. OTP/CAPTCHA and other user-verification steps should remain user-controlled.
