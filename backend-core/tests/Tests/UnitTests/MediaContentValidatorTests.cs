using Xunit;
using Infrastructure.Services;

namespace Tests.UnitTests;

public class MediaContentValidatorTests
{
    [Fact]
    public void ValidateOrThrow_AllowsJpeg()
    {
        MediaContentValidator.ValidateOrThrow("photo.jpg", "image/jpeg", 1024);
    }

    [Fact]
    public void ValidateOrThrow_RejectsExe()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MediaContentValidator.ValidateOrThrow("malware.exe", "application/octet-stream", 1024));
    }

    [Fact]
    public void ValidateOrThrow_RejectsOversized()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MediaContentValidator.ValidateOrThrow("big.pdf", "application/pdf", MediaContentValidator.MaxUploadBytes + 1));
    }

    [Fact]
    public void ValidateOrThrow_AllowsCsv()
    {
        MediaContentValidator.ValidateOrThrow("data.csv", "text/csv", 1024);
        MediaContentValidator.ValidateOrThrow("data.csv", "application/vnd.ms-excel", 1024);
    }

    [Fact]
    public void NormalizeFileName_StripsPath()
    {
        var name = MediaContentValidator.NormalizeFileName(@"..\..\evil.png");
        Assert.Equal("evil.png", name);
    }
}
