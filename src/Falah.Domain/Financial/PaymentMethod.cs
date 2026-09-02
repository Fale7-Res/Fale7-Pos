namespace Falah.Domain.Financial;

public enum PaymentMethod { Cash, Card, InstaPay, Wallet, Multi }

/// <summary>Well-known account ids that already exist as fixed rows in the current system (unlike
/// a driver custody account, which is created dynamically per driver). Kept as plain strings, not a
/// closed enum, because TreasuryAccount identity in general is not a fixed set.</summary>
public static class TreasuryAccounts
{
    public const string Main = "tre-main";
    public const string Drawer = "tre-drawer";
    public const string Bank = "tre-bank";
    public const string InstaPay = "tre-instapay";
    public const string Wallet = "tre-wallet";

    /// <summary>Accounts a protected-balance check applies to. Ported from
    /// assertProtectedSourceBalance in lib/financial-domain.mjs - note tre-wallet is deliberately
    /// NOT in this set there, matching the original list exactly.</summary>
    public static readonly IReadOnlySet<string> Protected = new HashSet<string> { Main, Drawer, Bank, InstaPay };
}
