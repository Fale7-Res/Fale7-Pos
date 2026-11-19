---
name: falah-security-review
description: Use when adding or changing a Falah server endpoint, handler, permission, login/PIN/approval flow, session handling, print-agent call, or anything reachable over the LAN - and when reviewing such a change. Lists the identity, authorization, PIN-guessing, approval and audit rules with the harness helpers that prove them.
---

# Falah security review

Threat model in one line: a LAN with several tills, staff who each hold a session and know their own
PIN (insiders), possibly an untrusted device on the network, plain HTTP until HTTPS is installed
(`docs/LAN_DEPLOYMENT.md`). The restaurant must keep trading while an attack is refused.

## Checklist for every endpoint

1. **Identity from the session.** Read the actor with `HttpContextActorExtensions.Actor(http)`.
   Body/query actor fields (`AuthenticationMiddleware.ActorFields`) are checked against the session
   by the middleware; a new "who did this" field must be added to that list.
2. **Permission in the server.** Handler calls `AuthorizationPolicy.AuthorizeCommand(...)` or a
   `Require*` gate in `Program.cs`. Client-side hiding is not a control. New permission strings go in
   `PermissionCatalog`. Never widen a permission to make a test pass.
3. **Reads leak too.** Totals, balances, expected cash before a blind count (`prepare-close` needs
   `shifts_blind_close`), user lists with permissions (`users_manage`), reports (`reports_view`).
   Who may see a treasury balance is an open owner decision (A1).
4. **Errors.** Expected failures → 422/404/409 with an Arabic message; nothing internal in the body;
   unexpected → the global handler's reference id.
5. **Idempotency.** Commands carry a request id and replay before validating.

## PINs and approvals

- Every PIN check or choice goes through a guard: `LoginAttemptGuard` (login; per machine via
  `ClientSource`, server-under-attack mode, restart-proof from audit rows), `ApprovalPinAttemptGuard`
  (`/api/approvals/issue`), `PinChoiceGuard` (create user / change PIN; uniqueness is an oracle).
  A guess is reserved before the PBKDF2 work; a right PIN returns only its own slot.
- Same answer for every wrong PIN; never log or audit the PIN value.
- Approvals: single-use (`Consumed` concurrency token), one action, spent only by the requesting
  user, within `Approvals:LifetimeMinutes`. A manager's own permission is never replaced by an
  approval where the code says "no approval fallback" (`expenses_main_treasury`).
- Print agents log in as a `print_agent`-only account; claim/report endpoints require it.

## Harness for negative tests (`tests/Falah.Server.IntegrationTests`)

- `FalahApiFactory.CreateClientFrom("10.0.0.5")` — requests from a given machine address.
- `factory.PostAsSessionOfAsync(client, userId, url, body)` — an actor-less request (e.g. approval)
  from a specific user's session.
- `new FalahApiFactory(environment: "Production")` — production startup behaviour.
- `new FalahApiFactory(reuseDatabaseName: other.DatabaseName)` — a restarted server.
- Swap the clock: `factory.WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<TimeProvider>(clock)))`.
- A client without `ActorTokenHandler` (`factory.CreateClient()`) is anonymous.

Write the attack as a test (it must fail first), then an availability test (legitimate users still
work), then a restart test if state is involved.

## Review questions

Can another session/user/machine do it? Can it be repeated (replay, concurrency, restart)? Does a
refusal leak whether a secret was right? Can one insider stop every till? Is it audited with who and
which machine, without the secret?

## Done means

Negative tests RED → GREEN, availability test green, residual risk and owner decisions written in
the issue file (`All Issues/ISSUES/`) and `docs/ASSESSMENT.md`.
