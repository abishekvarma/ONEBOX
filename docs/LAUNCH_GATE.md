# ONEBOX Launch Gate

GREEN means implementation exists and can be tested. It does not mean a third-party provider, legal adviser, app-store review, merchant account or penetration tester has approved it.

## Completed in this revision
- Server-issued USER/ADMIN/SUPPORT role claim support.
- ADMIN endpoint authorization uses backend roles.
- Duplicate AdminController removed from MeController.
- Mobile OTP verification flow: short expiry, hashed OTP, attempt limit, single-use after success.
- Login blocked until phone verification.
- Production access tokens shortened to 30 minutes.
- JWT issuer/audience validation enabled.
- HTTPS redirection and HSTS for non-development.
- Authentication rate limiting.
- Platform fee is server-controlled for checkout; client cannot choose the fee used for a transaction.
- Production Swagger disabled.
- Master production/compliance checklist added.

## External gates still required before public launch
- Connect and verify an SMS/OTP provider.
- Connect and verify an authorized payment gateway/merchant arrangement and webhook signatures.
- Complete legal review of DPDP Act/Rules applicability, privacy notice, terms, retention, grievance and processor contracts.
- Establish incident-response procedures consistent with applicable CERT-In requirements.
- Configure production secrets, database encryption, backups and restore testing.
- Complete real provider connector tests and review provider terms.
- Complete independent security assessment/penetration testing.
