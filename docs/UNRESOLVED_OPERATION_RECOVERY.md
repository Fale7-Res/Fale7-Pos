# Surviving a power cut mid-operation

## The gap this closes

Every money-moving command in the stations is submitted through `RetainedRequest`. When a response
is lost, the exact immutable DTO is kept so a retry re-sends the same bytes and the server - which
recognises the request's own idempotency key - can answer what actually happened. A later rejection
never releases it, because "no" to a second attempt does not prove the first one did not commit.

That protected a lost response **within one session**. It did not protect the case this hardware
actually suffers: the station losing power mid-payment. The process ended, the retained DTO went
with it, and on the next start there was no way left to ask the question.

## What changed

The retained DTO is now written to disk **before** it is sent, and removed only once the outcome is
known. On the next start the screen that submitted it recovers the same request.

- Storage: one JSON file per owner under `%LOCALAPPDATA%\Falah\pending-operations`, written whole to
  a temporary file and then moved into place, so a power cut mid-write cannot leave a half-written
  file that reads back as a different request than the one submitted.
- Each of the 24 retained requests carries an explicit, stable owner key (`payroll.pay`,
  `employees.cash-out`, `cashier.electronic-confirm`, …). The key is written out rather than derived
  from a type or line number, because it is what reconnects a recovered request to its screen and
  refactoring must not silently orphan it.
- An owner whose request type varies - a screen that submits several different commands - records the
  concrete type with the payload and recovers the right one.

## Recovery is passive, on purpose

Nothing is re-sent by itself. The recovered request comes back into the screen's own retained slot,
so the screen's existing retry path re-sends those same bytes when a person asks it to. Every one of
those screens already renders "التحقق من الحفظ السابق" on its action button while a request is
unresolved, so reopening the screen states plainly that something is outstanding.

On startup the main window also shows how many operations were left unresolved, which screens they
belong to and when they were submitted.

The blocking policy is unchanged. A recovered request does not lock the station on launch: an
operation that can never succeed would otherwise leave a restaurant unable to serve anyone, and
deciding when an unresolved operation may be abandoned is a business rule, not a client-side one.
What the client guarantees is that the question is never silently lost.

## Failure behaviour

A store that cannot write - a full disk, a permissions problem - does not stop the operation. The
in-memory retention still protects the current session, which is exactly the behaviour that existed
before persistence. A corrupt or unreadable pending file is dropped rather than left to strand the
screen that owns it.

## Tests

`tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs` covers: an ambiguous outcome surviving
the process; a resolved outcome leaving nothing behind; a plain first-attempt rejection leaving
nothing behind; a rejection *after* an ambiguous outcome keeping the request; the recovered request
being re-sent byte for byte rather than rebuilt from the form; confirmation clearing it for good;
owners not recovering each other's requests; an owner with several command types recovering the right
one; the listing carrying the submission time; a corrupt file not blocking the station; and an
unwritable store still protecting the session.

## The Call Center

The order desk keeps its own hand-rolled retention rather than `RetainedRequest`, so it was made
durable in place instead of being refactored - rewriting the control flow of the checkout path would
have risked the behaviour those tests already pin down for a change that is orthogonal to it.

Both its unresolved operations are now persisted the same way: the checkout and the print-job
request are written before they are sent and cleared once the outcome is known, and the desk recovers
them in its constructor. A recovered checkout also restores its own `CheckoutRequestId`, which is the
whole point - re-sending under the original idempotency key is what lets the server say whether the
first attempt landed.

Keys are per operator (`callcenter.checkout.{userId}`), because this station keeps one order draft
per logged-in operator and two of them must never recover each other's work.

Each client writes to its own directory (`pending-operations\cashier`, `pending-operations\callcenter`)
so that a machine running both never surfaces one station's pending work as the other's.

## Not covered

Nothing outstanding in either client. The recovery path itself has not been exercised against a real
power cut on the physical terminal - only against a process that is stopped and restarted.
