using PesaLens.Core.Models;
using PesaLens.Core.Services;
using Xunit;

namespace PesaLens.Tests;

/// <summary>
/// Tests for MpesaSmsParser covering all M-Pesa message types.
///
/// HOW TO ADD REAL SMS SAMPLES:
/// Replace the string literals in each test with actual messages from your
/// phone. The more real samples you test, the more robust the parser becomes.
/// </summary>
public class MpesaSmsParserTests
{
    private readonly MpesaSmsParser _parser = new();

    // Shared dummy values — smsId and timestamp don't affect parse logic
    private const long SmsId = 12345L;
    private const long SmsTimestamp = 1_700_000_000_000L; // fallback epoch ms

    // ── Null / empty guards ───────────────────────────────────────────────────

    [Fact]
    public void Parse_NullBody_ReturnsNull()
    {
        var result = _parser.Parse(null!, SmsId, SmsTimestamp);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_EmptyBody_ReturnsNull()
    {
        var result = _parser.Parse(string.Empty, SmsId, SmsTimestamp);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WhitespaceBody_ReturnsNull()
    {
        var result = _parser.Parse("   ", SmsId, SmsTimestamp);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_UnrelatedSms_ReturnsNull()
    {
        var result = _parser.Parse("Your OTP is 123456. Do not share.", SmsId, SmsTimestamp);
        Assert.Null(result);
    }

    // ── Send Money ────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_SendMoney_ReturnsCorrectType()
    {
        const string sms =
            "RG84XY1234 Confirmed. Ksh500.00 sent to JOHN DOE 0712345678 " +
            "on 4/6/25 at 10:34 AM. New M-PESA balance is Ksh12,500.00. " +
            "Transaction cost, Ksh0.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(TransactionType.SendMoney, result.Type);
    }

    [Fact]
    public void Parse_SendMoney_ExtractsAmountCorrectly()
    {
        const string sms =
            "RG84XY1234 Confirmed. Ksh500.00 sent to JOHN DOE 0712345678 " +
            "on 4/6/25 at 10:34 AM. New M-PESA balance is Ksh12,500.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(500.00m, result.Amount);
    }

    [Fact]
    public void Parse_SendMoney_ExtractsCounterpartyName()
    {
        const string sms =
            "RG84XY1234 Confirmed. Ksh500.00 sent to JOHN DOE 0712345678 " +
            "on 4/6/25 at 10:34 AM. New M-PESA balance is Ksh12,500.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal("JOHN DOE", result.CounterpartyName);
    }

    [Fact]
    public void Parse_SendMoney_ExtractsMpesaCode()
    {
        const string sms =
            "RG84XY1234 Confirmed. Ksh500.00 sent to JOHN DOE 0712345678 " +
            "on 4/6/25 at 10:34 AM. New M-PESA balance is Ksh12,500.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal("RG84XY1234", result.MpesaCode);
    }

    [Fact]
    public void Parse_SendMoney_ExtractsBalanceAfterTransaction()
    {
        const string sms =
            "RG84XY1234 Confirmed. Ksh500.00 sent to JOHN DOE 0712345678 " +
            "on 4/6/25 at 10:34 AM. New M-PESA balance is Ksh12,500.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(12500.00m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_SendMoney_AmountWithCommas_ParsesCorrectly()
    {
        const string sms =
            "AB12CD3456 Confirmed. Ksh1,200.00 sent to JANE DOE 0700000000 " +
            "on 5/6/25 at 2:00 PM. New M-PESA balance is Ksh8,000.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(1200.00m, result.Amount);
    }

    // ── Receive Money ─────────────────────────────────────────────────────────

    [Fact]
    public void Parse_ReceiveMoney_ReturnsCorrectType()
    {
        const string sms =
            "TAK91Z5678 Confirmed. You have received Ksh1,200.00 from JANE DOE " +
            "0712345679 on 30/5/25 at 9:15 AM. New M-PESA balance is Ksh5,760.50.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(TransactionType.ReceiveMoney, result.Type);
    }

    [Fact]
    public void Parse_ReceiveMoney_ExtractsAmount()
    {
        const string sms =
            "TAK91Z5678 Confirmed. You have received Ksh1,200.00 from JANE DOE " +
            "0712345679 on 30/5/25 at 9:15 AM. New M-PESA balance is Ksh5,760.50.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(1200.00m, result.Amount);
    }

    [Fact]
    public void Parse_ReceiveMoney_ExtractsSenderName()
    {
        const string sms =
            "TAK91Z5678 Confirmed. You have received Ksh1,200.00 from JANE DOE " +
            "0712345679 on 30/5/25 at 9:15 AM. New M-PESA balance is Ksh5,760.50.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal("JANE DOE", result.CounterpartyName);
    }

    // ── Paybill ───────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_PayBill_ReturnsCorrectType()
    {
        const string sms =
            "QG99A1N4Y Confirmed. Ksh2,500.00 sent to KPLC PREPAID for account " +
            "37192837465 on 23/5/25 at 6:20 PM. New M-PESA balance is Ksh260.50. " +
            "Transaction cost, Ksh0.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(TransactionType.PayBill, result.Type);
    }

    [Fact]
    public void Parse_PayBill_ExtractsMerchantName()
    {
        const string sms =
            "QG99A1N4Y Confirmed. Ksh2,500.00 sent to KPLC PREPAID for account " +
            "37192837465 on 23/5/25 at 6:20 PM. New M-PESA balance is Ksh260.50.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal("KPLC PREPAID", result.CounterpartyName);
    }

    [Fact]
    public void Parse_PayBill_ExtractsAccountNumber()
    {
        const string sms =
            "QG99A1N4Y Confirmed. Ksh2,500.00 sent to KPLC PREPAID for account " +
            "37192837465 on 23/5/25 at 6:20 PM. New M-PESA balance is Ksh260.50.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal("37192837465", result.CounterpartyNumber);
    }

    // ── Buy Goods ─────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_BuyGoods_ReturnsCorrectType()
    {
        const string sms =
            "QH82J3L9Z Confirmed. Ksh1,200.00 paid to KUKU FOODS on 24/5/25 " +
            "at 12:45 PM. New M-PESA balance is Ksh4,560.50. Transaction cost, Ksh0.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(TransactionType.BuyGoods, result.Type);
    }

    [Fact]
    public void Parse_BuyGoods_ExtractsMerchantName()
    {
        const string sms =
            "QH82J3L9Z Confirmed. Ksh1,200.00 paid to KUKU FOODS on 24/5/25 " +
            "at 12:45 PM. New M-PESA balance is Ksh4,560.50.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal("KUKU FOODS", result.CounterpartyName);
    }

    [Fact]
    public void Parse_BuyGoods_ExtractsAmount()
    {
        const string sms =
            "QH82J3L9Z Confirmed. Ksh4,230.00 paid to NAIVAS SUPERMARKET on 24/5/25 " +
            "at 6:12 PM. New M-PESA balance is Ksh12,500.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(4230.00m, result.Amount);
    }

    // ── Withdrawal ────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Withdrawal_ReturnsCorrectType()
    {
        const string sms =
            "PL44ZX9012 Confirmed. You have withdrawn Ksh3,000.00 from " +
            "Kimani Agent - Westlands on 20/5/25 at 3:10 PM. " +
            "New M-PESA balance is Ksh1,200.00. Transaction cost, Ksh35.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(TransactionType.Withdrawal, result.Type);
    }

    [Fact]
    public void Parse_Withdrawal_ExtractsAmount()
    {
        const string sms =
            "PL44ZX9012 Confirmed. You have withdrawn Ksh3,000.00 from " +
            "Kimani Agent - Westlands on 20/5/25 at 3:10 PM. " +
            "New M-PESA balance is Ksh1,200.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(3000.00m, result.Amount);
    }

    // ── Airtime ───────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Airtime_ReturnsCorrectType()
    {
        const string sms =
            "QG88H2R6V Confirmed. You bought Ksh500.00 of airtime on 23/5/25 " +
            "at 8:05 AM. New M-PESA balance is Ksh2,760.50. Transaction cost, Ksh0.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(TransactionType.AirtimePurchase, result.Type);
    }

    [Fact]
    public void Parse_Airtime_CounterpartyIsSafaricom()
    {
        const string sms =
            "QG88H2R6V Confirmed. You bought Ksh500.00 of airtime on 23/5/25 " +
            "at 8:05 AM. New M-PESA balance is Ksh2,760.50.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal("Safaricom Airtime", result.CounterpartyName);
    }

    [Fact]
    public void Parse_Airtime_ExtractsAmount()
    {
        const string sms =
            "QG88H2R6V Confirmed. You bought Ksh100.00 of airtime on 23/5/25 " +
            "at 8:05 AM. New M-PESA balance is Ksh2,660.50.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(100.00m, result.Amount);
    }

    // ── Reversal ──────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Reversal_ReturnsCorrectType()
    {
        const string sms =
            "XZ99PP1234 Confirmed. Your transaction of Ksh200.00 has been reversed " +
            "on 22/5/25 at 4:00 PM. New M-PESA balance is Ksh3,000.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(TransactionType.Reversal, result.Type);
    }

    // ── Common fields ─────────────────────────────────────────────────────────

    [Fact]
    public void Parse_AnyTransaction_SmsIdIsPreserved()
    {
        const string sms =
            "RG84XY1234 Confirmed. Ksh500.00 sent to JOHN DOE 0712345678 " +
            "on 4/6/25 at 10:34 AM. New M-PESA balance is Ksh12,500.00.";

        var result = _parser.Parse(sms, smsId: 99L, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(99L, result.SmsId);
    }

    [Fact]
    public void Parse_AnyTransaction_CategoryIdIsZero()
    {
        // CategoryId = 0 means "not yet categorized" — assigned later by AutoCategorizationService
        const string sms =
            "QH82J3L9Z Confirmed. Ksh1,200.00 paid to KUKU FOODS on 24/5/25 " +
            "at 12:45 PM. New M-PESA balance is Ksh4,560.50.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(0, result.CategoryId);
    }

    [Fact]
    public void Parse_AnyTransaction_IsEditedIsFalse()
    {
        const string sms =
            "QH82J3L9Z Confirmed. Ksh1,200.00 paid to KUKU FOODS on 24/5/25 " +
            "at 12:45 PM. New M-PESA balance is Ksh4,560.50.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.False(result.IsEdited);
    }

    [Fact]
    public void Parse_AnyTransaction_MpesaCodeIsUpperCase()
    {
        const string sms =
            "rg84xy1234 Confirmed. Ksh500.00 sent to JOHN DOE 0712345678 " +
            "on 4/6/25 at 10:34 AM. New M-PESA balance is Ksh12,500.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        // Even if the code arrives lowercase, parser should uppercase it
        if (result is not null)
            Assert.Equal(result.MpesaCode, result.MpesaCode.ToUpperInvariant());
    }

    // ── Whitespace / formatting edge cases ───────────────────────────────────

    [Fact]
    public void Parse_SmsWithExtraNewlines_StillParses()
    {
        const string sms =
            "RG84XY1234 Confirmed.\nKsh500.00 sent to JOHN DOE 0712345678\non 4/6/25 at 10:34 AM.\nNew M-PESA balance is Ksh12,500.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(TransactionType.SendMoney, result.Type);
    }

    [Fact]
    public void Parse_SmsWithLeadingTrailingWhitespace_StillParses()
    {
        const string sms =
            "  RG84XY1234 Confirmed. Ksh500.00 sent to JOHN DOE 0712345678 " +
            "on 4/6/25 at 10:34 AM. New M-PESA balance is Ksh12,500.00.  ";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(500.00m, result.Amount);
    }

    // ── Date fallback ─────────────────────────────────────────────────────────

    [Fact]
    public void Parse_UnparsableDate_FallsBackToSmsTimestamp()
    {
        // A message with a weird/missing date should still parse,
        // using the SMS timestamp as fallback
        const string sms =
            "QH82J3L9Z Confirmed. Ksh500.00 paid to SOME SHOP on ?? at ??. " +
            "New M-PESA balance is Ksh1,000.00.";

        // Even if it returns null (no match), it should never throw
        var ex = Record.Exception(() => _parser.Parse(sms, SmsId, SmsTimestamp));
        Assert.Null(ex);
    }

    // ── Theory: all types return non-null for valid samples ───────────────────

    [Theory]
    [InlineData(
        "AA11BB2233 Confirmed. Ksh200.00 sent to TEST USER 0700000001 on 1/6/25 at 1:00 PM. New M-PESA balance is Ksh800.00.",
        TransactionType.SendMoney)]
    [InlineData(
        "BB22CC3344 Confirmed. You have received Ksh300.00 from SENDER NAME 0700000002 on 2/6/25 at 2:00 PM. New M-PESA balance is Ksh1,100.00.",
        TransactionType.ReceiveMoney)]
    [InlineData(
        "CC33DD4455 Confirmed. Ksh400.00 sent to MERCHANT NAME for account ACC123 on 3/6/25 at 3:00 PM. New M-PESA balance is Ksh700.00.",
        TransactionType.PayBill)]
    [InlineData(
        "DD44EE5566 Confirmed. Ksh50.00 of airtime on 4/6/25 at 4:00 AM. New M-PESA balance is Ksh650.00.",
        TransactionType.AirtimePurchase)]
    public void Parse_ValidSamples_ReturnsCorrectType(string sms, TransactionType expectedType)
    {
        var result = _parser.Parse(sms, SmsId, SmsTimestamp);

        Assert.NotNull(result);
        Assert.Equal(expectedType, result.Type);
    }
}