# ONEBOX Production Master Checklist

## Hard launch gates

### Identity
- Email + password authentication.
- Mandatory mobile OTP verification before login.
- OTP is hashed, short-lived, single-use, attempt-limited and never stored in plaintext.
- JWT contains server-issued role; access tokens are short-lived.
- USER, SUPPORT and ADMIN roles are enforced server-side.

### Admin separation
- `/api/admin/*` requires ADMIN role.
- No email allowlist authorization.
- Admin actions require audit events.
- Admin UI is separate from the customer UI.

### Payments
- ONEBOX calculates its platform fee server-side; client cannot choose the fee.
- Customer payment authentication happens at the authorized payment provider.
- ONEBOX never requests UPI PIN, card PIN, bank password or similar credentials.
- Payment success is accepted only from a verified provider callback/webhook.
- Payment callbacks are authenticated, idempotent and tied to the correct transaction/user/task.
- Booking is never marked successful merely because a redirect/return page says success.
- For production, use an approved merchant/payment-gateway arrangement; a personal UPI ID must not be treated as a substitute for required merchant/payment compliance.

### Data protection (India)
- Map every collected data field to a documented purpose and lawful basis/consent where required.
- Publish privacy notice, terms, consent flows, retention/deletion policy and grievance/contact process.
- Implement user access/correction/deletion workflows as applicable.
- Maintain processor/vendor records and data-sharing disclosures.
- Apply security safeguards, breach response and required notifications.
- DPDP Act 2023 and DPDP Rules 2025 are the primary data-protection framework to assess for the product; final legal applicability and obligations require Indian counsel review.

### Cybersecurity
- HTTPS/HSTS in production.
- Server-side authorization and rate limiting.
- Secure secrets management.
- Restricted database access and encrypted connections.
- Audit logs.
- Backups plus tested restore procedure.
- Security monitoring and incident-response runbook.
- CERT-In incident-reporting obligations must be incorporated where applicable.
- Pre-launch vulnerability assessment/penetration test and remediation.

### AI/execution
- AI plans; it does not declare success.
- Only authorized connectors/browser agent can execute.
- Approved-domain allowlist.
- No arbitrary JavaScript execution or CAPTCHA/MFA bypass.
- Irreversible actions require explicit confirmation.
- Idempotency prevents duplicate execution.
- Payment-success/booking-failure enters recovery state.

### Scheduling
- Scheduled tasks store owner, action, timezone, run time, status and notification state.
- Notifications are reliable and retry safely.
- Irreversible scheduled actions require confirmation unless the user explicitly creates a permitted recurring authorization model.

### Purchases / warranties
- Email access is opt-in and scoped.
- Store only necessary purchase/receipt information.
- Extract order, product, seller, date, amount, warranty/guarantee dates where reliably available.
- Encrypt sensitive stored content and support deletion.
- Send expiry reminders.

### External providers
- Each provider has an explicit connector contract, credentials/configuration, health state, error handling and recovery path.
- No provider completion is fabricated when the connector is unavailable.
- Provider terms and API/browser automation permissions must be reviewed before production use.

## Verification standard
A feature is GREEN only when its code path exists, configuration is documented, automated/manual tests pass, and the external dependency is actually verified.
