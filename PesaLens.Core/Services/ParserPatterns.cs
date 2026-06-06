using System.Text.RegularExpressions;

namespace PesaLens.Core.Services;

public static class ParserPatterns
{
    // ── Compiled patterns ────────────────────────────────────────────────────
    // All patterns are case-insensitive. The transaction code is always the
    // first word in every M-Pesa message.

    public static readonly Regex CodePattern = new(
        @"^(?<code>[A-Z0-9]{10})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly Regex SendMoneyPattern = new(
        @"^(?<code>[A-Z0-9]{10}) Confirmed\.\s*Ksh(?<amount>[\d,]+\.?\d*) sent to (?<name>[A-Z\s]+?) (?<phone>\d+) on (?<date>[\d/]+) at (?<time>[\d:]+\s*[APM]+)\.?\s*(New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*))?.*?(Transaction cost,?\s*Ksh(?<cost>[\d,]+\.?\d*))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex ReceiveMoneyPattern = new(
        @"^(?<code>[A-Z0-9]{10}) Confirmed\.\s*You have received Ksh(?<amount>[\d,]+\.?\d*) from (?<name>[A-Z\s]+?) (?<phone>\d+) on (?<date>[\d/]+) at (?<time>[\d:]+\s*[APM]+)\.?\s*(New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex PayBillPattern = new(
        @"^(?<code>[A-Z0-9]{10}) Confirmed\.\s*Ksh(?<amount>[\d,]+\.?\d*) sent to (?<name>[A-Z0-9\s&]+?) for account (?<account>[A-Z0-9\-\s]+?) on (?<date>[\d/]+) at (?<time>[\d:]+\s*[APM]+)\.?\s*(New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*))?.*?(Transaction cost,?\s*Ksh(?<cost>[\d,]+\.?\d*))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex BuyGoodsPattern = new(
        @"^(?<code>[A-Z0-9]{10}) Confirmed\.\s*Ksh(?<amount>[\d,]+\.?\d*) paid to (?<name>[A-Z0-9\s&'\-\.]+?) on (?<date>[\d/]+) at (?<time>[\d:]+\s*[APM]+)\.?\s*(New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*))?.*?(Transaction cost,?\s*Ksh(?<cost>[\d,]+\.?\d*))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex WithdrawalPattern = new(
        @"^(?<code>[A-Z0-9]{10}) Confirmed\.\s*You have withdrawn Ksh(?<amount>[\d,]+\.?\d*) from (?<agent>[A-Z0-9\s]+?) - (?<location>.+?) on (?<date>[\d/]+) at (?<time>[\d:]+\s*[APM]+)\.?\s*(New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*))?.*?(Transaction cost,?\s*Ksh(?<cost>[\d,]+\.?\d*))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex AirtimePattern = new(
        @"^(?<code>[A-Z0-9]{10}) Confirmed\.\s*You bought Ksh(?<amount>[\d,]+\.?\d*) of airtime on (?<date>[\d/]+) at (?<time>[\d:]+\s*[APM]+)\.?\s*(New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex FulizaPattern = new(
        @"^(?<code>[A-Z0-9]{10}) Confirmed\.\s*(You have fulfilled|Ksh(?<amount>[\d,]+\.?\d*) of Fuliza)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex ReversalPattern = new(
        @"^(?<code>[A-Z0-9]{10}) Confirmed\.\s*Your transaction of Ksh(?<amount>[\d,]+\.?\d*) has been reversed",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex DepositPattern = new(
        @"^(?<code>[A-Z0-9]{10}) Confirmed\.\s*Ksh(?<amount>[\d,]+\.?\d*) deposited to your M-PESA",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
}
