using GestionBibliotheque.Services;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class PayrollCalculationServiceTests
{
    [Fact]
    public void CalculateUsesExplicitInputsWithoutInventingTaxRates()
    {
        var result = new PayrollCalculationService().Calculate(new PayrollCalculationInput(
            1000m,
            [new PayrollComponentInput("Allowance", "Transport", 100m, null), new PayrollComponentInput("Bonus", "Performance", 50m, null), new PayrollComponentInput("Deduction", "Advance", 25m, null)],
            EmployeeTaxAmount: 0m,
            EmployerContributionAmount: 40m));

        Assert.Equal(1000m, result.BaseSalary);
        Assert.Equal(150m, result.EarningsAmount);
        Assert.Equal(1150m, result.GrossAmount);
        Assert.Equal(1150m, result.TaxableAmount);
        Assert.Equal(25m, result.EmployeeDeductionAmount);
        Assert.Equal(40m, result.EmployerContributionAmount);
        Assert.Equal(1125m, result.NetAmount);
        Assert.False(result.TaxConfigurationApplied);
    }

    [Fact]
    public void CalculateRejectsAmountsThatExceedGross()
    {
        Assert.Throws<InvalidOperationException>(() => new PayrollCalculationService().Calculate(new PayrollCalculationInput(
            100m, [new PayrollComponentInput("Deduction", "Advance", 80m, null)], EmployeeTaxAmount: 30m)));
    }
}
