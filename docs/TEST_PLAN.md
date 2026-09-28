# ONEBOX smoke-test plan

1. Register a new account.
2. Log out and log back in.
3. Allow browser location.
4. Verify `/api/me` returns `locationAllowed=true`.
5. Open the dashboard and map.
6. Open Chat and submit: `Find me a hospital near me tomorrow evening.`
7. Verify a task is persisted and a confirmation is requested.
8. Open Task History and verify the task appears.
9. Open Hospitals and run live nearby search after configuring Google Places.
10. Select a slot and provider.
11. Verify the app opens the provider website rather than claiming a booking without a connector.
12. Configure an authorized connector and verify only that connector can create a provider reference.
13. Verify admin metrics using an email listed in `ADMIN_EMAILS`.
