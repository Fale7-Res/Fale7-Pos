# Security plugin launch failure — bounded read-only investigation

Original stable error is preserved in [security-launch-blocker.json](security-launch-blocker.json). No scan ID was returned; discovery did not start. There is no canonical plugin security finding set or completed report.

Checks actually performed on continuation:

1. Verified both reported paths exist: bundled Python executable and plugin `scripts/workbench_db.py`.
2. Executed the bundled interpreter only with `-B -c "import sys,sqlite3; ..."`: it eventually returned Python 3.12.14 and SQLite 3.53.1, exit 0. Therefore neither missing executable nor missing sqlite3 module is established as the cause. The delayed probe does not by itself identify a timeout root cause.
3. Read plugin storage/connection code. `list-scans` uses a connection path that can create its private directory, apply schema migrations, change journal mode to WAL and chmod its database. Therefore the apparently read-only CLI was **not rerun** directly against user state. No plugin storage repair, installation, schema change or configuration edit was performed.
4. Computed the current process's documented storage path from CODEX_SECURITY_STATE_DIR or CODEX_HOME/default without exposing environment secrets. The expected `workbench.sqlite3` file was absent at that resolved path. The MCP server could have a different environment; absence is a diagnostic observation, not proof of the failure cause. No SQLite connection was made because the file was absent. The proposed inspection used URI mode=ro/immutable=1, never creation.
5. Process-command-line inspection through CIM was denied by this environment; no process or system setting was changed. The bundled Python probe subsequently completed normally.

**Root cause remains UNKNOWN.** The original tool did not expose child stderr, so permission, initialization, environment and transient runtime failures cannot be distinguished. No repeat coordinator launch, replacement scan or false successful scan was made. Future plugin recovery should preserve this error and request supported diagnostics before changing installation/state.

The blocker applies to that plugin workflow only. Independent source inspection, cross-review and an isolated in-memory middleware/parser harness continued. See [manual security audit draft](SECURITY_AND_BACKDOOR_AUDIT.md) and [actual harness result](middleware-case-harness-results.json). These are ordinary audit artifacts, not substituted or forged plugin canonical results.
