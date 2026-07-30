## SkyChat

SkyChat is the moderated public Flipper Chat service. It stores message bodies,
Minecraft UUIDs, sending-client identifiers and timestamps for chat operation
and moderation.

`DELETE /api/chat/messages/user/{uuid}` removes stored messages and their
dependent reports for a canonical Minecraft UUID. It is an internal endpoint:
the account-facing caller must authenticate the user, resolve the linked UUID
and supply `X-Chat-Deletion-Token`. The service fails closed when
`MESSAGE_DELETION_TOKEN` is missing or shorter than 32 bytes. Store the token in
OpenBao; do not put a live value in source or a fleet manifest.

Mute and sanction records are intentionally separate from message deletion.
They may be retained only while needed for moderation, abuse prevention, legal
requirements or related claims.

Sanction `Message` and `Reason` free text becomes eligible for automatic
redaction at 96 hours and the running service sweeps every ten minutes. Set
`MODERATION_QUOTE_RETENTION_HOURS` to a positive number below 96 to shorten that
window; missing, invalid and larger values resolve to 96. The normalized rule
remains for the existing 400-day repeat-sanction calculation. Discord mute
notifications contain the normalized rule and sanction metadata, not either
free-text field. Do not put unrelated details or message content in other
sanction fields.

Once an expired sanction reaches the end of that 400-day window, the same
ten-minute sweep deletes its standalone database row. Active and permanent
sanctions are not eligible because their expiry is later than the cutoff.
SkyChat currently has no explicit legal-hold mechanism; add a scoped,
record-level hold before using this table for held evidence rather than
retaining every sanction indefinitely or putting evidence in logs/traces.

Chat webhook forwarding requires a database client with an HTTPS webhook URL
and a non-empty authentication value. Authentication is sent only in the
`Authorization` header. Before configuring a webhook, record the recipient,
purpose, data fields, location, access controls, deletion/retention behavior
and contract or internal role.

`POST /api/chat/internal/client` additionally requires
`X-Chat-Admin-Token` to match `CLIENT_ADMIN_TOKEN`. Missing secrets and values
shorter than 32 bytes fail closed. Store the secret in OpenBao and never in
source or a fleet manifest.

## Deploying
This project should be deployed within a container. 
### Configuration
See appsettings.json
