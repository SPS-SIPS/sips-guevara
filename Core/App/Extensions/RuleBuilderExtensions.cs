namespace SPSS.App.Extensions;
public static class RuleBuilderExtensions
{
    public static IRuleBuilder<T, DateTimeOffset[]> DateRange<T>(this IRuleBuilder<T, DateTimeOffset[]> ruleBuilder)
    {
        var options = ruleBuilder.NotNull().ChildRules(date =>
        {
            date.RuleFor(x => x[0]).NotNull()
            .WithMessage("Start Date is required!")
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithMessage("Start Date must be today or a future date!");
            date.RuleFor(x => x[1]).NotNull()
            .WithMessage("End Date is required!")
            .GreaterThan(x => x[0])
            .WithMessage("End Date should be greater than Start Date!");
        });
        return options;
    }

    public static IRuleBuilder<T, string> Password<T>(this IRuleBuilder<T, string> ruleBuilder, int minimumLength = 8)
    {
        var options = ruleBuilder
            .MinimumLength(minimumLength).WithMessage($"Passwords must be at least {minimumLength} characters.")
            .Matches("[A-Z]").WithMessage("Passwords must have at least one uppercase ('A'-'Z').")
            .Matches("[0-9]").WithMessage("Passwords must have at least one digit ('0'-'9').")
            .Matches("[^a-zA-Z0-9]").WithMessage("Passwords must have at least one non alphanumeric character.");
        return options;
    }


    public static IRuleBuilderOptions<T, TProperty> In<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, params TProperty[] validOptions)
    {
        string formatted;
        if (validOptions == null || validOptions.Length == 0)
        {
            throw new ArgumentException("At least one valid option is expected", nameof(validOptions));
        }
        else if (validOptions.Length == 1)
        {
            formatted = validOptions[0]!.ToString()!;
        }
        else
        {
            // format like: option1, option2 or option3
            formatted = $"{string.Join(", ", validOptions.Select(vo => vo!.ToString()).ToArray(), 0, validOptions.Length - 1)} or {validOptions.Last()}";
        }

        return ruleBuilder
            .Must(validOptions.Contains)
            .WithMessage($"{{PropertyName}} must be one of these values: {formatted}");
    }


}
