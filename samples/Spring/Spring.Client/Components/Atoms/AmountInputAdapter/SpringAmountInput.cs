using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

using Mississippi.Refraction.Client;
using Mississippi.Refraction.Client.Components.Atoms.Input;


namespace MississippiSamples.Spring.Client.Components.Atoms.AmountInputAdapter;

/// <summary>
///     Adapts Refraction's string input to a validated decimal amount.
/// </summary>
internal sealed class SpringAmountInput : ComponentBase
{
    private static readonly IReadOnlyDictionary<string, object> NativeInputAttributes =
        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["autocomplete"] = "off",
            ["inputmode"] = "decimal",
        };

    private string draft = string.Empty;

    private string? errorText;

    private bool hasObservedValue;

    private decimal lastObservedValue;

    /// <summary>Initializes a new instance of the <see cref="SpringAmountInput" /> class.</summary>
    public SpringAmountInput()
    {
    }

    /// <summary>Gets or sets the native input ID.</summary>
    [Parameter]
    [EditorRequired]
    public string InputId { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the input is disabled.</summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>Gets or sets the callback when draft validity changes.</summary>
    [Parameter]
    public EventCallback<bool> IsValidChanged { get; set; }

    /// <summary>Gets or sets the accessible label.</summary>
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the placeholder displayed for an empty draft.</summary>
    [Parameter]
    public string Placeholder { get; set; } = "0.00";

    /// <summary>Gets or sets the current validated amount.</summary>
    [Parameter]
    public decimal Value { get; set; }

    /// <summary>Gets or sets the callback when a valid amount changes.</summary>
    [Parameter]
    public EventCallback<decimal> ValueChanged { get; set; }

    private string InputState => errorText is null ? RefractionStates.Idle : RefractionStates.Invalid;

    private static bool HasExactDecimalRepresentation(
        string value,
        decimal amount
    ) =>
        string.Equals(
            NormalizeDecimalText(value),
            NormalizeDecimalText(amount.ToString(CultureInfo.InvariantCulture)),
            StringComparison.Ordinal);

    private static bool HasSupportedSignificantPrecision(
        string value
    )
    {
        const int maximumSignificantDigits = 29;
        int digitPosition = 0;
        int firstNonZeroDigitPosition = -1;
        int lastNonZeroDigitPosition = -1;
        foreach (char character in value)
        {
            if (!char.IsAsciiDigit(character))
            {
                continue;
            }

            if (character != '0')
            {
                if (firstNonZeroDigitPosition < 0)
                {
                    firstNonZeroDigitPosition = digitPosition;
                }

                lastNonZeroDigitPosition = digitPosition;
            }

            digitPosition++;
        }

        return (firstNonZeroDigitPosition < 0) ||
               (((lastNonZeroDigitPosition - firstNonZeroDigitPosition) + 1) <= maximumSignificantDigits);
    }

    private static string NormalizeDecimalText(
        string value
    )
    {
        int valueStart = value[0] is '+' or '-' ? 1 : 0;
        bool isNegative = value[0] == '-';
        int decimalPointIndex = value.IndexOf('.', valueStart);
        int integerEnd = decimalPointIndex < 0 ? value.Length : decimalPointIndex;
        int integerStart = valueStart;
        while ((integerStart < integerEnd) && (value[integerStart] == '0'))
        {
            integerStart++;
        }

        int fractionStart = decimalPointIndex < 0 ? value.Length : decimalPointIndex + 1;
        int fractionEnd = value.Length;
        while ((fractionEnd > fractionStart) && (value[fractionEnd - 1] == '0'))
        {
            fractionEnd--;
        }

        bool isZero = true;
        for (int index = valueStart; index < value.Length; index++)
        {
            if (char.IsAsciiDigit(value[index]) && (value[index] != '0'))
            {
                isZero = false;
                break;
            }
        }

        if (isZero)
        {
            return "0";
        }

        string integer = integerStart == integerEnd ? "0" : value[integerStart..integerEnd];
        string fraction = value[fractionStart..fractionEnd];
        string sign = isNegative ? "-" : string.Empty;
        return fraction.Length == 0 ? $"{sign}{integer}" : $"{sign}{integer}.{fraction}";
    }

    private static bool TryParseAmount(
        string value,
        out decimal amount
    )
    {
        amount = default;
        if (string.IsNullOrWhiteSpace(value) || value.Contains(',', StringComparison.Ordinal) || (value[^1] == '.'))
        {
            return false;
        }

        if (value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        int decimalPointIndex = value.IndexOf('.', StringComparison.Ordinal);
        if ((decimalPointIndex >= 0) && ((value.Length - decimalPointIndex - 1) > 28))
        {
            return false;
        }

        int decimalPointCount = 0;
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (character == '.')
            {
                decimalPointCount++;
                if (decimalPointCount > 1)
                {
                    return false;
                }
            }
            else if (!char.IsAsciiDigit(character) && ((index != 0) || ((character != '+') && (character != '-'))))
            {
                return false;
            }
        }

        if (!HasSupportedSignificantPrecision(value))
        {
            return false;
        }

        if (!decimal.TryParse(
                value,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out amount))
        {
            return false;
        }

        if (!HasExactDecimalRepresentation(value, amount))
        {
            amount = default;
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(
        RenderTreeBuilder builder
    )
    {
        builder.OpenComponent<InputField>(0);
        builder.AddAttribute(1, nameof(InputField.Id), InputId);
        builder.AddAttribute(2, nameof(InputField.Label), Label);
        builder.AddAttribute(3, nameof(InputField.Type), "text");
        builder.AddAttribute(4, nameof(InputField.Value), draft);
        builder.AddAttribute(
            5,
            nameof(InputField.ValueChanged),
            EventCallback.Factory.Create<string>(this, HandleInputAsync));
        builder.AddAttribute(6, nameof(InputField.State), InputState);
        builder.AddAttribute(7, nameof(InputField.ErrorText), errorText);
        builder.AddAttribute(8, nameof(InputField.HelperText), "Use a period (.) for decimal values.");
        builder.AddAttribute(9, nameof(InputField.InputAttributes), NativeInputAttributes);
        builder.AddAttribute(10, nameof(InputField.IsDisabled), IsDisabled);
        builder.AddAttribute(11, nameof(InputField.Placeholder), Placeholder);
        builder.CloseComponent();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (!hasObservedValue || (lastObservedValue != Value))
        {
            bool wasInvalid = errorText is not null;
            draft = Value.ToString(CultureInfo.InvariantCulture);
            errorText = null;
            lastObservedValue = Value;
            hasObservedValue = true;
            if (wasInvalid)
            {
                await IsValidChanged.InvokeAsync(true);
            }
        }
    }

    private async Task HandleInputAsync(
        string value
    )
    {
        draft = value;
        if (!TryParseAmount(value, out decimal amount))
        {
            errorText = "Enter a complete amount using digits and a period (.), for example 12.34.";
            await IsValidChanged.InvokeAsync(false);
            return;
        }

        errorText = null;
        lastObservedValue = amount;
        hasObservedValue = true;
        await ValueChanged.InvokeAsync(amount);
        await IsValidChanged.InvokeAsync(true);
    }
}