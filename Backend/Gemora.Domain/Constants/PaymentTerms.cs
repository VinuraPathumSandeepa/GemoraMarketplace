namespace Gemora.Domain.Constants;

public static class PaymentTerms
{
    public const decimal AdvancePercentage = 0.60m;
    public const decimal RoundingUnit = 1000m;

    public static decimal CalculateAdvanceAmount(decimal orderTotal)
    {
        if (orderTotal < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(orderTotal),
                "Order total cannot be negative.");
        }

        var advance = orderTotal * AdvancePercentage;

        return Math.Round(
            advance / RoundingUnit,
            0,
            MidpointRounding.AwayFromZero) * RoundingUnit;
    }
}
