namespace LocalCrm.Shared;

public sealed class FieldClock
{
    public string Value { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
    public string DeviceId { get; set; } = "";
}

public sealed class ContactRecord
{
    public Guid Id { get; set; }
    public FieldClock Name { get; set; } = new();
    public FieldClock Email { get; set; } = new();
    public FieldClock Phone { get; set; } = new();
    public FieldClock Company { get; set; } = new();
    public FieldClock Notes { get; set; } = new();
    public DateTimeOffset DeletedAt { get; set; }
    
    public ValidationResult Validate()
    {
        var errors = new List<string>();
        
        if (Id == Guid.Empty)
            errors.Add("ID cannot be empty");
            
        if (string.IsNullOrWhiteSpace(Name?.Value))
            errors.Add("Name is required");
            
        if (!string.IsNullOrWhiteSpace(Email?.Value) && !IsValidEmail(Email.Value))
            errors.Add("Email format is invalid");
            
        if (!string.IsNullOrWhiteSpace(Phone?.Value) && Phone.Value.Length < 10)
            errors.Add("Phone number must be at least 10 characters");
            
        return new ValidationResult(errors.Count == 0, errors);
    }
    
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

public sealed record ValidationResult(bool IsValid, List<string> Errors);

public static class FieldMerger
{
    public static FieldClock Pick(FieldClock local, FieldClock remote)
    {
        if (remote.UpdatedAt > local.UpdatedAt)
        {
            return Clone(remote);
        }

        if (remote.UpdatedAt < local.UpdatedAt)
        {
            return Clone(local);
        }

        return string.CompareOrdinal(remote.DeviceId, local.DeviceId) > 0 ? Clone(remote) : Clone(local);
    }

    public static ContactRecord Merge(ContactRecord local, ContactRecord remote) => new()
    {
        Id = local.Id == Guid.Empty ? remote.Id : local.Id,
        Name = Pick(local.Name, remote.Name),
        Email = Pick(local.Email, remote.Email),
        Phone = Pick(local.Phone, remote.Phone),
        Company = Pick(local.Company, remote.Company),
        Notes = Pick(local.Notes, remote.Notes),
        DeletedAt = local.DeletedAt > remote.DeletedAt ? local.DeletedAt : remote.DeletedAt
    };

    public static FieldClock Touch(string value, string deviceId) => new()
    {
        Value = value,
        UpdatedAt = DateTimeOffset.UtcNow,
        DeviceId = deviceId
    };

    private static FieldClock Clone(FieldClock f) => new()
    {
        Value = f.Value,
        UpdatedAt = f.UpdatedAt,
        DeviceId = f.DeviceId
    };
}
