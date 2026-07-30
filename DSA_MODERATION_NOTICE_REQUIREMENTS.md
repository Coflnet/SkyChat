# SkyChat moderation notice requirements

Status: implementation requirements, not legal advice

Scope: public Flipper Chat

Primary source: [Regulation (EU) 2022/2065 (Digital Services Act), especially Articles 14, 16, 17 and, where applicable, 19–24](https://eur-lex.europa.eu/eli/reg/2022/2065/oj)

## Product facts and scope assumption

SkyChat stores messages submitted by users and distributes them to other users. This document therefore assumes that SkyChat is a hosting service and likely an online platform under the DSA. Coflnet should record the final classification and whether it currently qualifies for the Article 19 micro/small-enterprise exemption. That exemption can remove the additional online-platform duties in Articles 20–23, but it does not remove the hosting-service duties in Articles 14, 16 and 17.

Required product rules:

1. Be nice.
2. Do not advertise something nobody asked for.

The client must display these rules whenever a user attempts to compose a message until that user acknowledges them. A user can click a message to report it, and successful submission must immediately acknowledge receipt. Those are useful parts of the required flow, but they do not by themselves satisfy every requirement below.

## DSA points of contact

Article 11 requires a public, current electronic point of contact for Member
State authorities, the European Commission and the European Board for Digital
Services. The publication must identify the supported languages, including
German and a language broadly understood in the Union. The existing support
mailbox may serve this function only after Coflnet assigns a human owner,
authority-identity verification, urgent escalation, acknowledgement, evidence
preservation and absence cover. Publicly label the mailbox as the Article 11
contact and identify German and English as supported languages.

Article 12 requires a direct, rapid, user-friendly electronic contact for
service recipients, with a choice of means and at least one route that does not
rely solely on automation. A monitored support email and staffed Discord
support can satisfy the channel requirement. Do not use the existing general
contact form as the only DSA route while it can silently discard a submission
as spam and still return success.

Neither Article 11 nor Article 12 requires a telephone number. Germany's
consumer-contract contact duties are a separate question addressed at the end
of this document.

## Mandatory requirements

### 1. Rules and automated moderation

The published rules must describe, in clear and unambiguous language, the restrictions SkyChat applies and the measures used to enforce them. They must also meaningfully describe automated moderation, including its purpose, the main parameters used and the possibility of human review.

Every runtime restriction must have an accurate published ground. The current server also blocks duplicate messages, specified words, links, credential-sharing links and discussion of a named third-party script. Before release, each restriction must either:

- be accurately and foreseeably covered by one of the two published rules; or
- be disclosed as a narrow anti-spam, account-security, legality or service-integrity rule.

Do not send a notice saying only “you violated the chat rules.” It must identify the applicable rule and explain the relevant conduct.

The two short labels can remain minimal while their published explanations make enforcement foreseeable:

- “Be nice” covers personal attacks, harassment, threats, and hateful or degrading abuse, but not civil disagreement or criticism.
- “Do not advertise something nobody asked for” covers unsolicited promotion of a listing, product, service, community or channel. A relevant answer to another user's request is not unsolicited advertising merely because it names or links to something.

The current server does not consistently implement that distinction. It rejects “my ah” without considering whether another user requested the information (`Services/ChatService.cs`, lines 156–158), rejects common top-level domains without context (lines 177–182), and mixes abusive language, advertising and other restrictions in one `BadWords` set (lines 30–35). Map each filter to one published rule or to an accurately disclosed anti-spam, security, legality or service-integrity ground. Do not describe all links or all sale references as Rule 2 violations.

The rule acknowledgement is a notice mechanism, not privacy consent, and must remain separate from general Terms acceptance and optional data-processing consents. Give the rules a stable version and record the acknowledged version, verified account or Minecraft UUID, time and client. Until that version is acknowledged, every attempted message must return a stable `rules_acknowledgement_required` response that causes the client to show both rules and the acknowledgement action. Do not rely only on client-local state, and do not ask again for an unchanged version.

The acknowledgement must be tied to a verified user. The current send endpoint accepts a client-application token and a caller-supplied UUID but does not itself prove the user controls that UUID (`Controllers/ChatController.cs`, lines 57–64; `Services/ChatService.cs`, lines 79–86). Reuse the account/linked-Minecraft verification layer or a short-lived signed capability from it.

### 2. Report submission and receipt — Article 16

The message-click flow may be the single entry point for both community-rule reports and illegal-content notices. To avoid extra UI, show the additional Article 16 fields only when the reporter selects “illegal content” or an equivalent category.

An illegal-content notice must support:

- the exact message identifier and enough context to locate the message, populated automatically from the clicked message;
- a sufficiently substantiated explanation of why the reporter considers it illegal;
- the reporter's name and email address;
- a declaration that the reporter believes in good faith that the submitted information is accurate and complete.

Name and email must not be required for information alleged to concern an offence in Articles 3–7 of Directive 2011/93/EU. The flow must remain usable by a person who is not logged in if public messages can be viewed without login.

If electronic contact details are available, send confirmation of receipt without undue delay. An immediate in-client acknowledgement can satisfy this only if:

- the backend has successfully accepted and durably recorded the report;
- the acknowledgement clearly says the report was received rather than merely that the click occurred; and
- it supplies a stable report reference or another way to associate the later decision with the report.

If submission fails, the client must say so and permit retry. The service must process notices in a timely, diligent, objective and non-arbitrary manner. Any automated processing or decision-making must be disclosed in the decision notice.

After deciding whether to act on the reported message, notify a reporter whose electronic contact details are available without undue delay. The notice must state the decision and explain the available redress. This reporter-facing decision notice is separate from the author-facing statement of reasons below.

Community-rule reports that do not allege illegality can use the shorter existing flow. The additional reporter decision and complaint duties described below apply if Coflnet is not covered by the Article 19 exemption.

The DSA does not set a fixed number of hours here. “Without undue delay” is
measured against the facts, so the queue must prioritize credible threats to
life, child-safety reports and other time-sensitive alleged illegality.
Ordinary rule reports may follow the normal moderation queue.

### 3. Notice to the affected author — Article 17

When SkyChat restricts content or an account because a message is considered illegal or violates the published rules, it must give the affected author a clear, specific statement of reasons no later than when the restriction takes effect, provided SkyChat knows relevant electronic contact details.

This applies to:

- removing, hiding, disabling, demoting or otherwise limiting visibility of a message;
- suspending or terminating part or all of the chat service; and
- suspending or terminating the user's account.

Treat a mute as at least a partial suspension of the service. Treat an automatically rejected message as a moderation restriction for implementation purposes and return the same core explanation immediately, even if counsel ultimately concludes that a pre-publication rejection falls outside Article 17.

The statement of reasons must contain:

1. **Action:** what was restricted, whether the message was removed/hidden/rejected or the user was muted, and the duration and territorial scope where relevant.
2. **Ground:** whether the decision concerns illegality or a Terms/rule violation.
3. **Facts:** the specific conduct and circumstances relied on. Identify whether the decision followed a user report or SkyChat's own detection. Do not identify the reporter unless strictly necessary.
4. **Automation:** whether automation detected the message or made/recommended the decision, and whether a human reviewed it.
5. **Authority:** for illegal content, the legal ground and why it applies; for a rule violation, the exact rule and why the facts breach it.
6. **Redress:** clear, user-friendly instructions for contesting the decision and any other applicable redress.

The notice must be in the user's supported locale where known—German for a German locale and English otherwise. It must remain retrievable for at least the life of the restriction and any applicable complaint period. Avoid reproducing unnecessary harmful content; a message ID and a limited excerpt are normally enough.

Article 17(4)'s exception for deceptive high-volume commercial content is
narrow. Do not treat every unsolicited advertisement or ordinary Rule 2
violation as exempt from a statement of reasons.

Deliver the notice through a retrievable in-account/mod inbox associated with
the verified user and optionally by email. A transient chat line alone is
insufficient if it disappears on reconnect. Record delivery status and retry
temporary failures from a durable outbox.

Minimum English example:

> Your message was not published and you were muted for 15 minutes. The message contained an unsolicited advertisement, contrary to Rule 2 (“Do not advertise something nobody asked for”). An automated filter detected the message; no human reviewed this initial decision. You can ask for human review at [action]. Reference: [decision ID].

### 4. Redress

The Article 17 notice must accurately explain the redress actually available. Article 17 alone does not require an exempt micro/small provider to operate its own internal appeal system. If no internal review exists, do not imply that it does; identify the applicable judicial or other redress instead.

The current Terms draft promises an affected user a support route and human review. That promised route must work even if Coflnet qualifies for the Article 19 exemption; the exemption cannot cure an unimplemented contractual promise.

If Coflnet does **not** qualify for the Article 19 micro/small-enterprise exemption, Article 20 additionally requires an internal complaint system that:

- is electronic and free;
- remains available for at least six months after the decision;
- covers decisions on reports as well as content, service and account restrictions;
- is easy to access and use;
- processes complaints in a timely, non-discriminatory, diligent and non-arbitrary way;
- is not decided solely by automation; and
- reverses an unfounded decision without undue delay.

In that case, the outcome must also explain certified out-of-court dispute settlement under Article 21. Article 24 duties, including submission of qualifying statements of reasons to the Commission's DSA Transparency Database, must also be implemented. Reassess these requirements before the business loses the exemption.

## Data and audit requirements

For each report and moderation decision, retain only what is necessary to prove and review the flow:

- client-generated idempotency key;
- report/decision ID and message ID;
- reporter contact details where supplied, kept separate from the affected user;
- report category and explanation;
- receipt and decision timestamps;
- action, duration and exact rule/legal ground;
- whether detection, recommendation or decision was automated;
- moderator or system actor;
- notices delivered, delivery channel and delivery status;
- complaint and reversal status.

Never expose the reporter's identity to the reported user by default. Define proportionate retention periods for reports and moderation evidence based on active moderation, complaint and legal-claim needs; do not retain them automatically for the full lifetime of the account.

The report-creation endpoint must be idempotent. Enforce a uniqueness
constraint on the authenticated reporter plus client-generated idempotency key;
a retry returns the original receipt and reference without creating a second
report. Decision writes and notification jobs likewise need stable IDs and
unique constraints so a retry cannot apply a sanction or send a notice twice.
Store message bodies separately from operational logs and traces. Audit records
should reference message/decision IDs and contain only the limited excerpt
needed for review.

The local hardening worktree now normalizes rule 1/rule 2 into a dedicated
sanction field. `MuteService.GetMuteTime` uses that field for the existing
400-day repeat-sanction calculation. `Mute.Message` and `Mute.Reason` become
eligible for automatic redaction after 96 hours and are swept every ten
minutes. Deleting a row from `Messages` therefore no longer leaves an
indefinite free-text copy in a sanction. This short convenience-evidence
window is not the future durable complaint record. The same sweep deletes an
expired sanction row when it is outside the 400-day repeat window; active and
permanent sanctions remain. SkyChat has no explicit legal-hold mechanism, so a
future held complaint record needs a scoped record-level hold rather than
blanket retention or evidence in logs/traces.

Moderation cases are not application logs or traces: they need purpose-based
retention and cannot depend on seven-day telemetry. Conversely, they must not
be retained indefinitely merely because they are not logs. If Article 20
applies, the case must remain reviewable throughout its six-month complaint
window. The DSA does not by itself justify a longer period. Anonymous aggregate
moderation metrics can remain longer.

## Good practice

These items reduce disputes and implementation mistakes but are not presented here as independent DSA requirements:

- Use one decision object to generate the API error, mute display, reporter outcome, author statement and audit record.
- Let the report button prefill the message reference and offer the two rules plus “illegal content” and “other.”
- Give community-rule reporters a genuine success acknowledgement and, where contact details are known, a later outcome even while Coflnet qualifies for the Article 19 exemption.
- Show a short notice first and make full reasons expandable.
- While Coflnet qualifies for the Article 19 exemption, still give the author a one-click “request human review” action to correct automated-filter errors.
- Test each server-side filter to ensure it emits a stable decision ID, correct rule, duration, automation flag and localized notice.
- Notify reporters without disclosing what is unnecessary, particularly details of account sanctions.
- Measure receipt-to-decision time and failed notice delivery without placing message bodies in logs or traces.

## Verified Discord and webhook data flows

These flows are separate from the developer-authored update log and must be
resolved before the privacy notice says that SkyChat user messages are not
stored on Discord:

- `Services/MuteService.cs` sends the affected player name, moderator name,
  normalized rule and duration through the `discord-messages` Kafka topic to
  channel `mutes`; if either name lookup fails, that player's UUID is sent
  instead. Quoted message and free-text reason are no longer included. These
  fields remain personal moderation data even though the update-log channel
  itself contains only developer-authored material.
- `Services/ChatBackgroundService.cs` can forward chat messages only for
  database clients whose URL is HTTPS and whose authentication value is
  non-empty. Creating a client additionally requires the separate fail-closed
  `CLIENT_ADMIN_TOKEN`. Inventory any production recipient, including its legal
  role, destination and retention. Remove unused webhook URLs/authentication
  values and disclose any external recipient that remains.

The lowest-maintenance outcome is to keep chat-message webhook forwarding
disabled and remove unused destinations. If moderation metadata delivery
through Discord remains, the privacy notice and processor/recipient register
must accurately describe the destination, fields, purpose and retention, and
access must be limited to moderators.

## Verification before rollout

- Replace the current click-to-report path.
  `SkyModCommands/Dialogs/ChatReportDialog.cs` invokes the generic
  `ReportCommand`, which writes the message, account, settings and connection
  context into error logs/traces and creates a second report span two seconds
  later. It does not create a SkyChat moderation report or support a decision
  outcome. UUIDs and message, report and sanction identifiers are pseudonymous
  personal data whenever reasonably linkable; they are not anonymous metrics.
- Add an authenticated report-submission endpoint. This repository currently
  has a `Report` entity and database table but no report endpoint in
  `ChatController`; the entity has no category, contact channel, idempotency
  key, status, decision or delivery evidence.
- Add a stable public message identifier to the distributed `ChatMessage`.
  SkyChat currently assigns the database ID only after publishing the message,
  so the click flow cannot reliably address the persisted item.
- Make the client show the two rules before each attempted composition until
  acknowledgement. `ChatCommand` now triggers the versioned
  `ChatRules20260728Tutorial` after each successfully sent message and bypasses
  the ordinary tutorial cooldown until acknowledgement. The tutorial now
  contains exactly the two rules above. Moving the acknowledgement before the
  first affected send and binding it to the verified sender remain follow-up
  requirements.
- Reconcile runtime restrictions with the published moderation description.
  SkyChat and SkyModCommands additionally enforce rate, length, duplicate,
  keyword, link, credential-link and named-script filters. Current errors and
  later mute responses do not provide a stable decision ID, exact ground,
  automation disclosure or review action.
- Implement the decision/audit schema, moderation queue, human-review route and
  durable notification outbox in SkyChat before a client claims the full flow.
- Deploy the backward-compatible SkyChat schema/API first, then the reporting
  and notice-capable mod client, and only then publish contractual wording that
  describes the new flow.
- Demonstrate that the UI acknowledgement is emitted only after successful backend acceptance.
- Demonstrate both reporter-facing and author-facing notices for: no action, message rejection/removal, temporary mute, permanent chat suspension and reversal on review.
- Confirm and document Coflnet's Article 19 micro/small-enterprise status.
- Have German/EU counsel confirm SkyChat's DSA classification and the treatment of messages rejected before publication.

## Rollout order and dependencies

1. Record SkyChat's DSA classification, Coflnet's Article 19 company-size
   assessment, the moderation owner, and the approved evidence/sanction
   retention schedule.
2. Reconcile the two headline rules with every filter and sanction. Publish the
   concise rules and expandable moderation explanation.
3. Add stable message IDs and backward-compatible report, decision, notice,
   appeal and durable-outbox storage to SkyChat.
4. Keep the client-creation endpoint protected by its fail-closed admin token
   and, if public messages can be viewed without login, route a narrowly
   scoped central no-login illegal-content form into the same report store.
   Protect moderator endpoints and audit access.
5. Change SkyModCommands click-to-report to call the moderation API instead of
   the diagnostic `ReportCommand`.
6. Connect the verified account, recorded locale, email or retrievable
   in-service inbox. Deploy delivery configuration and monitoring before the
   UI promises decision notices.
7. Inventory chat webhooks and remove destinations that have not been reviewed
   and documented. Verify the current worktree's removal of quoted content from
   Discord mute notifications and unconditional EF sensitive-data
   logging/detailed errors in the deployed build.
8. Deploy the compatible SkyChat schema/API first, then updated
   SkyModCommands/clients, and only then enable the feature and publish wording
   that says the complete flow is live. Old clients must remain functional or
   receive a clear update-required response.
9. Run the end-to-end report, no-action, restriction, notice, appeal/reversal,
   erasure and delivery-retry tests. Alert on stuck moderation cases and
   outbox entries.

This avoids the apparent privacy-policy/code chicken-and-egg problem. Build and
deploy the new server code behind a disabled feature flag or inactive endpoint;
publish the accurate notice immediately before or together with activation;
then enable processing. A privacy notice describes processing—it is not itself
the legal basis or a request for consent. Terms changes that affect an existing
contract still need the agreed acceptance mechanism before those contractual
rules are applied to that user.

## Minimum implementation acceptance criteria

1. A published message carries a stable, authorization-checked public reference. `DbMessage.Id` is currently excluded from JSON (`Models/Message.cs`, lines 11–13), so a click cannot yet identify the persisted record reliably.
2. Report creation binds the clicked message, verified reporter where available, category, explanation, timestamp and idempotency key in one durable write. Illegal-content reports include the additional Article 16 fields above. Success and a report reference are returned only after commit.
3. The immediate UI response says only that the report was received. It does not imply a decision, removal or sanction. A failed write produces no success receipt and permits retry.
4. One durable decision object records action, scope/duration, facts, exact rule or legal ground, report-vs-own-initiative source, automation/human-review flags, time and redress. The current `Mute` and `Report` models do not hold this complete state (`Models/Mute.cs`, lines 9–80; `Models/Report.cs`, lines 9–23).
5. A decision separately queues the reporter outcome and, if a restriction is imposed, the affected-author statement. Delivery is retried from a durable outbox. A process crash cannot silently lose either notice.
6. The author statement is available no later than the restriction; the reporter decision follows without undue delay. The existing mute response gives only a stored message and expiry and is not a complete statement (`Services/ChatService.cs`, lines 190–192).
7. The promised review route is reference-linked, handled by a person able to reverse the action, and communicates the result. Reporter identity is not disclosed to the affected user by default.
8. Receipts and statements are available in German and English. Tests assert action, exact rule or law, specific facts, duration, automation, redress and decision ID in both locales.
9. Production contains the current worktree removal of unconditional EF
   sensitive-data logging and detailed errors (`Startup.cs`, lines 86–93).
   Logs, metrics and traces contain references and delivery states, not message
   bodies, reporter contacts, credentials or full notices.
10. End-to-end tests cover failed report storage, accepted/no-action, each chat rule, illegal content, automated false-positive reversal, unknown author contact, duplicate retry and notification retry.

## Telephone contact is a separate consumer-contract issue

The DSA report and user/authority contact mechanisms are electronic and do not
themselves require SkyChat to publish a telephone number.

That does not settle Coflnet's separate distance-contract information duties.
Current German Article 246a §1(1)(3) EGBGB expressly lists the trader's
telephone number and email:
[official current text](https://www.gesetze-im-internet.de/bgbeg/art_246a__1.html).
The 2019 CJEU Amazon judgment concerned the earlier “where available” wording
and held that a trader did not have to create a telephone/fax channel where
other rapid and effective contact means existed:
[C-649/17](https://curia.europa.eu/juris/liste.jsf?num=C-649/17).

Because the current German provision now lists the telephone requirement
without that qualifier, email, contact form and Discord should not be assumed
to substitute for it. Obtain German consumer-law advice or use a
low-maintenance business/VoIP number before relying on consumer checkout.
