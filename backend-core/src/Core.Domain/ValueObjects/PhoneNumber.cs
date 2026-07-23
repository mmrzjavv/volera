namespace Core.Domain.ValueObjects;

public class PhoneNumber
{
    public string Value { get; private set; }

    private PhoneNumber() { }

    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Phone number cannot be empty.");

        // Basic validation, can be enhanced
        if (!IsValidPhoneNumber(value))
            throw new ArgumentException("Invalid phone number format.");

        Value = value;
    }

    private bool IsValidPhoneNumber(string phoneNumber)
    {
        // Simple regex for international format
        var regex = new System.Text.RegularExpressions.Regex(@"^\+?[1-9]\d{1,14}$");
        return regex.IsMatch(phoneNumber);
    }

    public override bool Equals(object? obj)
    {
        return obj is PhoneNumber phone && Value == phone.Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;
    public static explicit operator PhoneNumber(string value) => new PhoneNumber(value);
}