using LocalCrm.Shared;

namespace LocalCrm.Api.Tests;

public class FieldMergerTests
{
    [Fact]
    public void Pick_NewerTimestamp_ReturnsNewer()
    {
        var older = new FieldClock
        {
            Value = "Old",
            UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            DeviceId = "device-1"
        };
        
        var newer = new FieldClock
        {
            Value = "New",
            UpdatedAt = DateTimeOffset.UtcNow,
            DeviceId = "device-2"
        };

        var result = FieldMerger.Pick(older, newer);
        
        Assert.Equal("New", result.Value);
    }

    [Fact]
    public void Pick_SameTimestamp_UsesDeviceId()
    {
        var time = DateTimeOffset.UtcNow;
        var field1 = new FieldClock
        {
            Value = "A",
            UpdatedAt = time,
            DeviceId = "device-a"
        };
        
        var field2 = new FieldClock
        {
            Value = "B",
            UpdatedAt = time,
            DeviceId = "device-b"
        };

        var result = FieldMerger.Pick(field1, field2);
        
        Assert.Equal("B", result.Value);
    }

    [Fact]
    public void Merge_CombinesFields()
    {
        var local = new ContactRecord
        {
            Id = Guid.NewGuid(),
            Name = FieldMerger.Touch("Local Name", "device-1"),
            Email = FieldMerger.Touch("old@example.com", "device-1")
        };

        var remote = new ContactRecord
        {
            Id = local.Id,
            Name = FieldMerger.Touch("Remote Name", "device-2"),
            Email = new FieldClock 
            { 
                Value = local.Email.Value, 
                UpdatedAt = local.Email.UpdatedAt.AddHours(-1),
                DeviceId = local.Email.DeviceId
            }
        };

        var merged = FieldMerger.Merge(local, remote);
        
        Assert.Equal("Remote Name", merged.Name.Value);
        Assert.Equal("old@example.com", merged.Email.Value);
    }
}

public class ContactRecordTests
{
    [Fact]
    public void Validate_EmptyId_ReturnsInvalid()
    {
        var contact = new ContactRecord
        {
            Id = Guid.Empty,
            Name = FieldMerger.Touch("Test", "device-1")
        };

        var result = contact.Validate();
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ID"));
    }

    [Fact]
    public void Validate_EmptyName_ReturnsInvalid()
    {
        var contact = new ContactRecord
        {
            Id = Guid.NewGuid(),
            Name = new FieldClock { Value = "" }
        };

        var result = contact.Validate();
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Name"));
    }

    [Fact]
    public void Validate_InvalidEmail_ReturnsInvalid()
    {
        var contact = new ContactRecord
        {
            Id = Guid.NewGuid(),
            Name = FieldMerger.Touch("Test", "device-1"),
            Email = FieldMerger.Touch("not-an-email", "device-1")
        };

        var result = contact.Validate();
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Email"));
    }

    [Fact]
    public void Validate_ShortPhone_ReturnsInvalid()
    {
        var contact = new ContactRecord
        {
            Id = Guid.NewGuid(),
            Name = FieldMerger.Touch("Test", "device-1"),
            Phone = FieldMerger.Touch("123", "device-1")
        };

        var result = contact.Validate();
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Phone"));
    }

    [Fact]
    public void Validate_ValidContact_ReturnsValid()
    {
        var contact = new ContactRecord
        {
            Id = Guid.NewGuid(),
            Name = FieldMerger.Touch("Test User", "device-1"),
            Email = FieldMerger.Touch("test@example.com", "device-1"),
            Phone = FieldMerger.Touch("1234567890", "device-1")
        };

        var result = contact.Validate();
        
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
