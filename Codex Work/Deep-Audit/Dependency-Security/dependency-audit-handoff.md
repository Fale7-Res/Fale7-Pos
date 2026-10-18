# Dependency advisory assessment — 210-component baseline SBOM

Assessed 2026-10-17 UTC against baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. This is the printing/workflow reviewer's separate phase-4 dependency assignment. The earlier 130 route self-revisions remain **pending independent acceptance**; neither this lookup nor identity/schema receipts establish completion of all original audit phases.

## Outcome and exact coverage

All **210 component/version entries**, representing **201 case-insensitive package IDs**, were compared against every page advertised by the retrieved NuGet VulnerabilityInfo index. Both advertised pages were obtained: base updated `2026-10-16T05:29:22.8211423Z` with 607 package keys; update page updated `2026-10-16T23:29:26.3048526Z` with zero additional keys. There were no inaccessible advertised pages or range-evaluation errors.

| Component disposition | Count | Meaning |
|---|---:|---|
| Package ID absent from retrieved known-advisory feed | 191 | No listed package advisory in this snapshot; not a safety certificate |
| Package ID has advisories, observed version outside every listed affected range | 17 | Version exclusions for these specific known advisories |
| Version matches affected package range | 2 | Test-only transitive restore-graph matches; loaded vulnerability not established |

There were **59 NuGet range comparisons**. All **24 referenced GitHub advisories** were retrieved as public JSON, giving 59 corresponding comparisons against their current package ranges; all package/advisory match decisions agreed. GitHub is related underlying advisory data, not an independent exhaustive second vulnerability database. Public MSRC CVRF documents were obtained for both matched CVEs. The JavaScript-only MSRC web pages did not supply readable details; the CVRF endpoint supplied the corresponding primary metadata instead.

Complete component decisions, package/project relationships, source URLs, retrieval UTC, transport/decoded-body/file SHA-256, and source responses are in [dependency-advisory-assessment.json](dependency-advisory-assessment.json) and `feeds/`. The parent's original [advisory-checks.json](advisory-checks.json) was **not modified**; its exact hash is recorded alongside this assessment. Its three narrow exclusions remain available without being substituted for whole-SBOM coverage.

## The two matches and runtime counterevidence

| Observed package | Advisory | Listed affected range | First patched package | Audit classification |
|---|---|---|---|---|
| `System.Net.Http 4.3.0` | [GHSA-7jgj-8wvc-jh57 / CVE-2018-8292](https://github.com/advisories/GHSA-7jgj-8wvc-jh57) | `<4.3.4` | `4.3.4` | Version-range match in test dependency graph; not a confirmed production-loaded vulnerability |
| `System.Text.RegularExpressions 4.3.0` | [GHSA-cmhx-cq75-c4mj / CVE-2019-0820](https://github.com/advisories/GHSA-cmhx-cq75-c4mj) | `>=4.3.0,<4.3.1` | `4.3.1` | Version-range match in test dependency graph; not a confirmed production-loaded vulnerability |

Both occur only in `Falah.Client.Tests`, `Falah.Domain.Tests`, and `Falah.Server.IntegrationTests` restore graphs. For each, the actual shortest dependency chain is:

`xunit/2.5.3 → xunit.assert/2.5.3 → NETStandard.Library/1.6.1 → matched System package/4.3.0`

All six relevant `project.assets.json` target records still hash-match the SBOM's saved assets. Those restore records contain old reference/runtime candidates, so the package matches are real inventory observations, not invented package versions. However, **all 61 existing own-test `.deps.json` manifests** omit `runtime`, `runtimeTargets`, and `native` implementation entries for both old packages, and no adjacent implementation DLL was found in those output directories. Their runtime configurations target `Microsoft.NETCore.App 8.0.0` (integration tests also reference ASP.NET Core 8); this is a framework requirement, not proof of the exact patch actually selected.

That static manifest evidence materially counters an assertion that either old implementation is loaded in the POS application. Source package version, restored compile/runtime candidate, final dependency manifest, framework servicing, and an assembly actually loaded into a process are different evidence levels. The host uses application **and framework** dependency manifests to populate probing paths. See [Microsoft default-probing documentation](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing) and [AssemblyLoadContext versioning](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext). No application/test process was started and no assembly location was observed in the actual application/test process.

Full hashes, manifests, target entries, and chains are in [test-package-runtime-selection-evidence.json](test-package-runtime-selection-evidence.json). Existing generated outputs may be stale or differ from a future publish, and arbitrary custom loading could differ; therefore the result is **runtime exposure not established**, not an unconditional not-vulnerable assertion.

For HTTP, the vendor's [original .NET announcement](https://github.com/dotnet/announcements/issues/88) identifies redirect authentication disclosure in older .NET Core release families and lists the package fix. For both CVEs, corresponding MSRC CVRF records were retrieved through `Updates('CVE-…')` and the advertised monthly document. Those product advisories must not be transplanted automatically onto a .NET 8 shared-framework assembly merely because a legacy test metapackage is present.

## Range evaluation and reproducibility

No ad hoc lexicographic version comparison was used. The assessment invokes the already installed official `NuGet.Versioning` assembly from SDK `8.0.303`, assembly version `6.10.0.97`; its hash and path are preserved in [nuget-advisory-range-evaluations.json](nuget-advisory-range-evaluations.json). `NuGetVersion.Parse` and `VersionRange.Parse(...).Satisfies(...)` evaluate every candidate. Twelve explicit semantic cases pass, covering inclusive/exclusive endpoints, exact versions, absent bounds, prerelease ordering, four-part NuGet versions, normalized versions, build metadata, case-insensitive release labels, and the previously checked caching patched boundary. Four strict GitHub-comparator conversion cases also pass; unsupported syntax would be recorded as a gap rather than classified clean. No conversion gaps occurred in the actual 59 ranges.

Audit-only reproduction scripts are under `Codex Work/Verification-Tools/`: `dependency_advisory_collect.py`, `dependency_advisory_details.py`, `dependency_nuget_ranges.ps1`, `dependency_github_ranges.py`, and `dependency_advisory_assessment.py`. They retrieve public JSON, perform local version comparison, and persist audit artifacts. They do not invoke restore, package installation, POS handlers, tests, databases, authentication endpoints, or printers. Re-running retrieval later can naturally change the snapshot; retain historical hashes before doing so.

## Remaining risks and handoff

- NuGet/GitHub known-advisory coverage cannot establish absence of vulnerabilities or zero-days. The 191 unlisted IDs have an explicit feed-absence disposition, not a risk-free label.
- Inventory comes from existing restore assets, not a new locked restore, SBOM-certified publish, file-signature verification, or actual loaded-module inventory. Direct/transitive and test/application scope are carried for every component.
- Installed .NET/ASP.NET/WindowsDesktop shared-framework patch levels, Windows App SDK/VC/native payloads, SQL/OS/drivers, and build-host tooling require their own version/advisory assessment. Package feed checks do not discharge those runtime/host responsibilities.
- No license, package signature, downloaded package-byte integrity, malicious package, provenance, or dependency-confusion assessment was implied by this advisory lookup.
- Before any approved maintenance, verify the actual final test/publish dependency graph and framework servicing. A future approved test-dependency refresh may remove the legacy metapackage entries; no version change is proposed as already implemented. Do not report the two historical package matches as remotely exploitable POS defects without a loaded vulnerable implementation and reachable triggering behavior.
- Parent may use its approved isolated reflection harness to observe framework assembly identities without network requests. Such a generic harness would still need careful distinction from the actual test/app host before asserting runtime proof.

No source/configuration changes, dependency restoration/upgrades, software installation, database/service actions, application launches, authentication calls, physical printing, commits, or pushes were performed. The only executed dependency code was the existing NuGet version parser inside an isolated audit process; the only network requests were public advisory metadata retrievals.
