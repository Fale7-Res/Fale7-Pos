import json, csv, os
from datetime import datetime, timezone

def sync_specs():
    with open('GitHub Copilot Work/Final-Handoff/FIX_IMPLEMENTATION_SPECIFICATIONS.json', 'r', encoding='utf-8') as f:
        spec_data = json.load(f)

    existing_specs = {s['finding_id']: s for s in spec_data['specifications']}

    new_specs = [
        {
            'finding_id': 'FIN-030',
            'priority': 'P2_STABILITY_AND_MAINTAINABILITY',
            'title': 'Arithmetic Overflow Exception in Money.Abs() on long.MinValue',
            'category': 'FINANCIAL_AND_DATABASE_INTEGRITY',
            'severity': 'MEDIUM',
            'affected_files': ['src/Falah.Domain/Common/Money.cs'],
            'root_cause': 'Direct invocation of Math.Abs(Piastres) without handling long.MinValue boundary edge condition in Money.Abs().',
            'impact': 'Unhandled System.OverflowException crashes execution if a long.MinValue piastres value is encountered.',
            'implementation_blueprint': 'Add guard in Money.Abs(): if (Piastres == long.MinValue) throw new DomainException("Cannot compute absolute value of minimum long piastres"); or clamp safely.',
            'required_regression_tests': [
                'Assert.Throws<DomainException>(() => Money.FromPiastres(long.MinValue).Abs()); or assert bounded behavior without unhandled OverflowException.'
            ],
            'acceptance_criteria': [
                'Money.Abs() handles or rejects long.MinValue safely without unhandled System.OverflowException.'
            ]
        },
        {
            'finding_id': 'FIN-031',
            'priority': 'P1_HIGH_CORE_OPERATIONS',
            'title': 'Payroll Advance Calculation Ledger Entry Type Case Mismatch Causes Zero Advance Deductions',
            'category': 'FINANCIAL_AND_DATABASE_INTEGRITY',
            'severity': 'HIGH',
            'affected_files': ['src/Falah.Domain/Payroll/PayrollRules.cs'],
            'root_cause': 'Verbatim port of legacy JavaScript lowercase string comparisons (\'advance\', \'payroll_settlement\') in PayrollRules.cs while the C# infrastructure persists PascalCase enum names (nameof(LedgerEntryType.Advance), nameof(LedgerEntryType.PayrollSettlement)).',
            'impact': 'Payroll calculation silently skips cash advance deductions; employees receive unreduced net salaries, causing immediate financial loss to the restaurant.',
            'implementation_blueprint': 'Update PayrollRules.OutstandingAdvance to check string equality using StringComparison.OrdinalIgnoreCase or match against nameof(LedgerEntryType.Advance) / nameof(LedgerEntryType.PayrollSettlement).',
            'required_regression_tests': [
                'Create employee cash advance with Type="Advance" (Debit=500 EGP); call PayrollRules.OutstandingAdvance; assert outstanding advance equals 500 EGP.',
                'Add settlement with Type="PayrollSettlement" (AdvanceDeduction=200 EGP); assert outstanding advance reduces to 300 EGP.'
            ],
            'acceptance_criteria': [
                'PayrollRules.OutstandingAdvance recognizes both PascalCase and lowercase ledger entry type strings.'
            ]
        },
        {
            'finding_id': 'FIN-032',
            'priority': 'P2_STABILITY_AND_MAINTAINABILITY',
            'title': 'Unvalidated Negative Delivery Service Fee Allows Artificially Reduced Order Payable Total',
            'category': 'FINANCIAL_AND_DATABASE_INTEGRITY',
            'severity': 'MEDIUM',
            'affected_files': [
                'src/Falah.Infrastructure/Delivery/CreateDeliveryServiceHandler.cs',
                'src/Falah.Infrastructure/Delivery/UpdateDeliveryServiceHandler.cs',
                'src/Falah.Domain/Orders/CheckoutRules.cs'
            ],
            'root_cause': 'Delivery service creation and update endpoints lack validation asserting FeePiastres >= 0, and CheckoutRules does not guard against negative delivery fee amounts.',
            'impact': 'Negative delivery service fees can be injected to artificially deflate order payable totals, leading to cash drawer shortages and corrupted financial journal entries.',
            'implementation_blueprint': 'Add validation in CreateDeliveryServiceHandler, UpdateDeliveryServiceHandler, and DeliveryServiceRequests validating FeePiastres >= 0; add domain assertion in CheckoutRules asserting deliveryFee.IsPositive || deliveryFee.IsZero.',
            'required_regression_tests': [
                'Submit CreateDeliveryServiceRequest with FeePiastres = -100; assert validation fails with DomainException or 400 Bad Request.',
                'Invoke CheckoutRules.ComputePayableTotal with negative delivery fee; assert DomainException is thrown.'
            ],
            'acceptance_criteria': [
                'Delivery service fees cannot be negative anywhere in the domain or persistence layer.'
            ]
        },
        {
            'finding_id': 'FIN-033',
            'priority': 'P1_HIGH_CORE_OPERATIONS',
            'title': 'BusinessClock DST Spring-Forward Transition Causes ArgumentException on Business Date Boundary Calculation',
            'category': 'FINANCIAL_AND_DATABASE_INTEGRITY',
            'severity': 'HIGH',
            'affected_files': ['src/Falah.Domain/Common/BusinessClock.cs'],
            'root_cause': 'UtcRangeForBusinessDate passes midnight (00:00:00) directly to TimeZoneInfo.ConvertTimeToUtc without verifying whether the time falls within a daylight saving time spring-forward gap.',
            'impact': 'Unhandled System.ArgumentException crashes all API endpoints, reports, and background jobs that calculate date ranges for the last Friday of April (and the preceding Thursday when computing range end).',
            'implementation_blueprint': 'Before calling ConvertTimeToUtc, check CairoTimeZone.IsInvalidTime(localMidnight). If true, adjust forward by the DST gap (e.g. 1 hour to 01:00:00) or calculate the UTC instant using TimeZoneInfo.GetUtcOffset with gap tolerance.',
            'required_regression_tests': [
                'Call BusinessClock.UtcRangeForBusinessDate with DateOnly(2024, 4, 26) and DateOnly(2025, 4, 25); assert method returns valid UTC range without throwing System.ArgumentException.',
                'Call BusinessClock.UtcRangeForBusinessDate with the preceding Thursday; assert method returns valid UTC range without throwing ArgumentException.'
            ],
            'acceptance_criteria': [
                'BusinessClock.UtcRangeForBusinessDate handles DST spring-forward boundary dates without throwing System.ArgumentException and maintains continuous UTC coverage.'
            ]
        },
        {
            'finding_id': 'FIN-034',
            'priority': 'P1_HIGH_CORE_OPERATIONS',
            'title': 'Payroll Advance Deduction Missing from Employee Ledger Posting and Ledger Schema',
            'category': 'FINANCIAL_AND_DATABASE_INTEGRITY',
            'severity': 'HIGH',
            'affected_files': [
                'src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs',
                'src/Falah.Infrastructure/Persistence/Entities/EmployeeLedgerEntryEntity.cs',
                'src/Falah.Infrastructure/Payroll/PayPayrollHandler.cs'
            ],
            'root_cause': 'EmployeeLedgerEntryEntity lacks an AdvanceDeduction column and PayPayrollHandler only posts net settlement debit without recording the advance deduction component into the employee ledger.',
            'impact': 'Outstanding employee advance balance is never credited or reduced in the ledger when payroll is disbursed; ledger reports continue to show the advance as unpaid, leading to double deductions or accounting discrepancies.',
            'implementation_blueprint': 'Add an AdvanceDeductionPiastres column to EmployeeLedgerEntryEntity and update PayPayrollHandler to record the advance deduction amount on the posted settlement entry or post a distinct advance recovery entry.',
            'required_regression_tests': [
                'Execute payroll run with an employee having advance deduction; execute PayPayrollHandler; assert EmployeeLedgerEntry records advance deduction and reduces outstanding advance.'
            ],
            'acceptance_criteria': [
                'Payroll settlement postings in EmployeeLedger record both net payable payout and advance deduction recovery.'
            ]
        },
        {
            'finding_id': 'FIN-035',
            'priority': 'P2_STABILITY_AND_MAINTAINABILITY',
            'title': 'BusinessDayCloseEvaluator Performs Global Unbounded Table Scans Blocking Business Day Close on Historical Data',
            'category': 'FINANCIAL_AND_DATABASE_INTEGRITY',
            'severity': 'MEDIUM',
            'affected_files': ['src/Falah.Infrastructure/BusinessDay/BusinessDayCloseEvaluator.cs'],
            'root_cause': 'BusinessDayCloseEvaluator queries shifts, driver custodies, and financial journal entries without predicate scoping to the target business day or date.',
            'impact': 'An unclosed shift or outstanding custody from an old date permanently blocks closing any subsequent business day; queries degrade as tables grow.',
            'implementation_blueprint': 'Add WHERE predicates restricting shifts and driver custodies to the specific business day being closed (e.g. s.BusinessDate == day.BusinessDate or s.BusinessDayId == day.Id), and scope journal checks to the relevant window.',
            'required_regression_tests': [
                'Seed an unclosed shift on date D-1 and an open shift on date D; close date D shift; call EvaluateAsync for date D; assert unclosed shift from D-1 does not block day close for date D.'
            ],
            'acceptance_criteria': [
                'BusinessDayCloseEvaluator only evaluates blockers relevant to the target business day and executes bounded queries.'
            ]
        },
        {
            'finding_id': 'FIN-036',
            'priority': 'P2_STABILITY_AND_MAINTAINABILITY',
            'title': 'PurchasingQueryService Performs Unbounded Full Table Scan on Supplier Ledger Entries in ListSuppliersAsync',
            'category': 'FINANCIAL_AND_DATABASE_INTEGRITY',
            'severity': 'MEDIUM',
            'affected_files': ['src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs'],
            'root_cause': 'ListSuppliersAsync fetches the entire SupplierLedgerEntries table into memory instead of computing supplier ledger balance aggregations via SQL GROUP BY in the database.',
            'impact': 'Memory consumption scales linearly O(N) with the entire historical purchasing ledger; causes out-of-memory crashes and severe database contention as supplier ledger entries grow.',
            'implementation_blueprint': 'Replace the full-table memory load with an EF Core query that groups by SupplierId and aggregates Debit and Credit sums directly in SQL Server.',
            'required_regression_tests': [
                'Seed 10,000 SupplierLedgerEntries across 50 suppliers; verify ListSuppliersAsync generates a grouped SQL query and executes in bounded memory without loading all entity instances into memory.'
            ],
            'acceptance_criteria': [
                'ListSuppliersAsync does not load full ledger table into memory and derives supplier totals via database aggregation.'
            ]
        },
        {
            'finding_id': 'FIN-037',
            'priority': 'P2_STABILITY_AND_MAINTAINABILITY',
            'title': 'Multi-Payment Order Refund Inability in Cashier UI and Backend Domain Exception on Treasury Allocation',
            'category': 'FINANCIAL_AND_DATABASE_INTEGRITY',
            'severity': 'MEDIUM',
            'affected_files': [
                'src/Falah.Client.Cashier/ViewModels/CashierViewModel.Refund.cs',
                'src/Falah.Domain/Orders/RefundOrderHandler.cs',
                'src/Falah.Infrastructure/Treasury/TreasuryMovementFactory.cs'
            ],
            'root_cause': 'The Cashier UI hard-disables refunds for orders with PaymentMethod == "multi", and TreasuryMovementFactory lacks treasury allocation rules for multi-method refunds.',
            'impact': 'Cashiers cannot process legitimate returns for customers who paid using mixed tenders (e.g. Cash + InstaPay); backend throws DomainException if invoked directly.',
            'implementation_blueprint': 'Obtain restaurant owner business decision on multi-payment refund settlement rules (e.g., refund to cash drawer vs original tender pro-rata). Update UI to allow refund initiation and extend RefundOrderHandler and TreasuryMovementFactory to allocate multi-payment refund legs.',
            'required_regression_tests': [
                'Submit refund on multi-payment order with designated tender refund legs; verify appropriate treasury accounts are debited/credited according to owner business policy.'
            ],
            'acceptance_criteria': [
                'Multi-payment orders can be refunded according to agreed owner business policies without throwing unhandled DomainException.'
            ]
        },
        {
            'finding_id': 'UI-023',
            'priority': 'P2_STABILITY_AND_MAINTAINABILITY',
            'title': 'EmployeesViewModel Sequentially Executes N+1 API Calls for Balances and Exits Early on First Failure Leaving Employee List Empty',
            'category': 'UI_UX_AND_DESKTOP_CLIENT_STATE',
            'severity': 'MEDIUM',
            'affected_files': [
                'src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs',
                'src/Falah.Client.CallCenterWinUI/ViewModels/EmployeesViewModel.cs'
            ],
            'root_cause': 'Sequential N+1 API balance calls in UI loading routine coupled with catastrophic early exit on single failure prior to applying display filter.',
            'impact': 'A single network hiccup or unmapped employee ledger record results in the entire employee list disappearing from the UI, blocking shift attendance and payroll operations.',
            'implementation_blueprint': 'Introduce a bulk employee balance endpoint on the server, or fetch balances concurrently with Task.WhenAll and gracefully degrade individual failed balances while still displaying successfully loaded employees.',
            'required_regression_tests': [
                'Simulate employee balance API failure for 1 out of 10 employees; assert EmployeesViewModel displays remaining 9 employees and shows non-blocking warning rather than an empty list.'
            ],
            'acceptance_criteria': [
                'EmployeesViewModel displays all successfully loaded employees even if an individual employee balance lookup fails, and eliminates sequential N+1 network requests.'
            ]
        },
        {
            'finding_id': 'INV-ROUND-001',
            'priority': 'P2_STABILITY_AND_MAINTAINABILITY',
            'title': 'CostingRules Yield Unit Conversion Uses Midpoint Banker\'s Rounding Causing Asymmetric Stock Deduction Truncation',
            'category': 'INVENTORY_AND_COSTING',
            'severity': 'MEDIUM',
            'affected_files': ['src/Falah.Domain/Inventory/CostingRules.cs'],
            'root_cause': 'CostingRules.BaseQuantityMilliUnits applies default MidpointRounding.ToEven on single portion quantities before multiplying by order quantity, truncating fractional units.',
            'impact': 'Asymmetric rounding truncation leads to cumulative inventory drift where physical stock and system stock diverge over time across high-volume recipe lines.',
            'implementation_blueprint': 'Compute total deducted base milliUnits across the entire ordered quantity using integer division with rounding: (line.QuantityMilliUnits * orderQuantity + ratio / 2) / ratio, or specify explicit MidpointRounding.AwayFromZero.',
            'required_regression_tests': [
                'Calculate stock deduction for 3 units of an item with a yield ratio of 3 and 1000 milliUnits declared; verify total deducted milliUnits equals exactly 1000 rather than 999.'
            ],
            'acceptance_criteria': [
                'Inventory stock deduction for yield-ratio recipe lines preserves exact aggregate integer milliUnits without rounding truncation.'
            ]
        }
    ]

    for s in new_specs:
        existing_specs[s['finding_id']] = s

    all_specs_list = sorted(existing_specs.values(), key=lambda x: (x.get('priority', ''), x.get('finding_id', '')))

    out_spec_data = {
        'generated_at': datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ'),
        'total_specifications': len(all_specs_list),
        'specifications': all_specs_list
    }

    with open('GitHub Copilot Work/Final-Handoff/FIX_IMPLEMENTATION_SPECIFICATIONS.json', 'w', encoding='utf-8') as f:
        json.dump(out_spec_data, f, indent=2)

    print(f'Updated FIX_IMPLEMENTATION_SPECIFICATIONS.json with {len(all_specs_list)} specifications!')

    csv_headers = ['finding_id', 'priority', 'title', 'severity', 'category', 'target_files', 'blueprint_summary']
    with open('GitHub Copilot Work/Final-Handoff/FIX_IMPLEMENTATION_SPECIFICATIONS.csv', 'w', encoding='utf-8', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(csv_headers)
        for s in all_specs_list:
            bp = s.get('implementation_blueprint', '')
            if isinstance(bp, list):
                bp = ' '.join(str(x) for x in bp)
            writer.writerow([
                s.get('finding_id', ''),
                s.get('priority', ''),
                s.get('title', ''),
                s.get('severity', ''),
                s.get('category', ''),
                ';'.join(s.get('affected_files', [])),
                str(bp).replace('\n', ' ')
            ])

    print('Updated FIX_IMPLEMENTATION_SPECIFICATIONS.csv successfully!')

if __name__ == '__main__':
    sync_specs()
