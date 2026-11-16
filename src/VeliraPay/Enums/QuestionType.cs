namespace VeliraPay;

/// <summary>The kind of field a payment link's checkout question is shown as, as <see cref="CheckoutQuestion.Type"/> gives it and <see cref="CheckoutQuestionOptions.Type"/> takes it.</summary>
public static class QuestionType
{
    /// <summary>Short text, on one line; the type of a question given none.</summary>
    public const string Text = "text";

    /// <summary>Long text, over several lines.</summary>
    public const string Textarea = "textarea";

    /// <summary>A number.</summary>
    public const string Number = "number";

    /// <summary>An email address.</summary>
    public const string Email = "email";

    /// <summary>A phone number, answered in E.164.</summary>
    public const string Phone = "phone";

    /// <summary>A link.</summary>
    public const string Url = "url";

    /// <summary>A date, answered as YYYY-MM-DD.</summary>
    public const string Date = "date";

    /// <summary>A dropdown of the question's options.</summary>
    public const string Select = "select";

    /// <summary>A checkbox, answered "yes" when ticked.</summary>
    public const string Checkbox = "checkbox";

    /// <summary>A country, answered as its two-letter code.</summary>
    public const string Country = "country";
}
