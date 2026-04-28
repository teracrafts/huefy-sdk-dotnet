using Teracrafts.Huefy.Sdk.Validators;
using Xunit;

namespace Teracrafts.Huefy.Sdk.Tests;

public class EmailValidatorsTests
{
    // -------------------------------------------------------------------------
    // ValidateEmail
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidateEmail_ValidEmail_ReturnsNull()
    {
        Assert.Null(EmailValidators.ValidateEmail("user@example.com"));
    }

    [Fact]
    public void ValidateEmail_ValidEmailWithSubdomain_ReturnsNull()
    {
        Assert.Null(EmailValidators.ValidateEmail("user@mail.example.com"));
    }

    [Fact]
    public void ValidateEmail_EmptyEmail_ReturnsError()
    {
        var result = EmailValidators.ValidateEmail("");
        Assert.NotNull(result);
        Assert.Contains("required", result);
    }

    [Fact]
    public void ValidateEmail_NullEmail_ReturnsError()
    {
        var result = EmailValidators.ValidateEmail(null!);
        Assert.NotNull(result);
    }

    [Fact]
    public void ValidateEmail_NoAtSign_ReturnsError()
    {
        Assert.NotNull(EmailValidators.ValidateEmail("userexample.com"));
    }

    [Fact]
    public void ValidateEmail_NoDomain_ReturnsError()
    {
        Assert.NotNull(EmailValidators.ValidateEmail("user@"));
    }

    [Fact]
    public void ValidateEmail_ExceedsMaxLength_ReturnsError()
    {
        var longEmail = new string('a', 250) + "@b.co";
        var result = EmailValidators.ValidateEmail(longEmail);
        Assert.NotNull(result);
        Assert.Contains("maximum length", result);
    }

    [Fact]
    public void ValidateEmail_WithSpaces_ReturnsError()
    {
        Assert.NotNull(EmailValidators.ValidateEmail("user @example.com"));
    }

    // -------------------------------------------------------------------------
    // ValidateTemplateKey
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidateTemplateKey_ValidKey_ReturnsNull()
    {
        Assert.Null(EmailValidators.ValidateTemplateKey("welcome-email"));
    }

    [Fact]
    public void ValidateTemplateKey_EmptyKey_ReturnsError()
    {
        var result = EmailValidators.ValidateTemplateKey("");
        Assert.NotNull(result);
        Assert.Contains("required", result);
    }

    [Fact]
    public void ValidateTemplateKey_WhitespaceOnly_ReturnsError()
    {
        Assert.NotNull(EmailValidators.ValidateTemplateKey("   "));
    }

    [Fact]
    public void ValidateTemplateKey_ExceedsMaxLength_ReturnsError()
    {
        var longKey = new string('a', 101);
        var result = EmailValidators.ValidateTemplateKey(longKey);
        Assert.NotNull(result);
        Assert.Contains("maximum length", result);
    }

    [Fact]
    public void ValidateTemplateKey_AtMaxLength_ReturnsNull()
    {
        var key = new string('a', 100);
        Assert.Null(EmailValidators.ValidateTemplateKey(key));
    }

    // -------------------------------------------------------------------------
    // ValidateEmailData
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidateEmailData_ValidData_ReturnsNull()
    {
        Assert.Null(EmailValidators.ValidateEmailData(
            new Dictionary<string, object?> { ["name"] = "John" }));
    }

    [Fact]
    public void ValidateEmailData_NullData_ReturnsError()
    {
        Assert.NotNull(EmailValidators.ValidateEmailData(null));
    }

    [Fact]
    public void ValidateEmailData_EmptyData_ReturnsNull()
    {
        Assert.Null(EmailValidators.ValidateEmailData(new Dictionary<string, object?>()));
    }

    // -------------------------------------------------------------------------
    // ValidateBulkCount
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidateBulkCount_ValidCount_ReturnsNull()
    {
        Assert.Null(EmailValidators.ValidateBulkCount(10));
    }

    [Fact]
    public void ValidateBulkCount_AtLimit_ReturnsNull()
    {
        Assert.Null(EmailValidators.ValidateBulkCount(1000));
    }

    [Fact]
    public void ValidateBulkCount_Zero_ReturnsError()
    {
        Assert.NotNull(EmailValidators.ValidateBulkCount(0));
    }

    [Fact]
    public void ValidateBulkCount_Negative_ReturnsError()
    {
        Assert.NotNull(EmailValidators.ValidateBulkCount(-1));
    }

    [Fact]
    public void ValidateBulkCount_OverLimit_ReturnsError()
    {
        var result = EmailValidators.ValidateBulkCount(1001);
        Assert.NotNull(result);
        Assert.Contains("maximum", result);
    }

    // -------------------------------------------------------------------------
    // ValidateSendEmailInput
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidateSendEmailInput_ValidInput_ReturnsEmptyList()
    {
        var errors = EmailValidators.ValidateSendEmailInput(
            "welcome",
            new Dictionary<string, object?> { ["name"] = "John" },
            "user@example.com");

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateSendEmailInput_AllInvalid_ReturnsMultipleErrors()
    {
        var errors = EmailValidators.ValidateSendEmailInput("", null, "bad");
        Assert.True(errors.Count >= 3);
    }

    [Fact]
    public void ValidateSendEmailInput_PartiallyInvalid_ReturnsSingleError()
    {
        var errors = EmailValidators.ValidateSendEmailInput(
            "welcome",
            new Dictionary<string, object?> { ["name"] = "John" },
            "bad");

        Assert.Single(errors);
    }
}
