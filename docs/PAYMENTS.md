# ONEBOX payments

ONEBOX does not collect bank credentials, UPI PINs, OTPs or card PINs.

The payment architecture supports two production modes:

1. **Provider checkout** — the user is sent to the provider's own authorized checkout and the provider charges the transaction.
2. **ONEBOX merchant checkout** — ONEBOX creates a checkout for `providerAmount + platformFee` through an authorized payment gateway. The gateway presents UPI/card/netbanking options and handles payment authentication. The provider is then booked/fulfilled using the authorized provider integration.

The second mode is the correct way to collect a ONEBOX platform fee together with a service amount. It requires ONEBOX to have a merchant/payment-gateway relationship; a generic `upi://` redirect cannot legally or technically turn an arbitrary provider's checkout into a combined merchant transaction.
