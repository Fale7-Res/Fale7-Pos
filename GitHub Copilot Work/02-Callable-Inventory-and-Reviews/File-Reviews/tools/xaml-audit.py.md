# tools/xaml-audit.py

Operational tool side-effect and correctness review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `0c4f176f6c12288d549342672851b0be534db2cc68f99640600d59cb08342665`. Working SHA-256 `00c267f65d79a1316c15317abd8cf279be3da6773569030dcfcdfe467a3543b1`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-164.

Symbols indexed (index is not a claim every method was reviewed): that@16, that@87, exists@99.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

Record/configuration/script top-level or lambdas; see source range and analysis.

Pure local static reader over two WinUI projects. Checks XML structure, converter registration and several regex patterns for resources/styles/bindings. No network/DB/registry writes. Glob scope omits nested resource dictionaries and regexes assume attribute ordering/simple keys; false positives on valid framework/theme resources possible. Clean result is not runtime UI proof. Existing syntax safety utility predates current audit; no security audit performed by invoking it here (not executed).

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
