# src/Falah.Client.CashierWinUI/Platform/ImageFilePicker.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `255209e418f421aa749563c9dc2cedfd4f5ed59a22ee7845ecab737733ad5275`

Worktree SHA-256: `6c354f62493fe359a802524930a881d825b21fc3c1359c47c162017020858bad`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Unpackaged WinUI product-photo file picker.

## Source paths and function/resource analysis

- `PickAsync` at `src/Falah.Client.CashierWinUI/Platform/ImageFilePicker.cs:15`: Parents Windows picker with actual window handle, restricts extensions, reads entire selected stream into MemoryStream then ToArray. All streams disposed; duplicates full image memory and has no pre-read size ceiling/cancellation. Very large file can consume significant memory before server limit rejection. MIME inferred from extension; server must validate bytes independently.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Native picker cannot be covered by current plain net8.0 tests; missing large file/cancel/read error UI behavior.

## Recommendations (not implemented)

Apply file size limit before full read, cancellation and safe error message; use content signatures server-side. No file picker invoked in audit.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
