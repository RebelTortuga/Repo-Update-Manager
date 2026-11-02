using Xunit;

namespace RepoUpdateManager.Tests;

public sealed class VersionAndFilterTests
{
    [Theory]
    [InlineData("4.9.0", "4.8.0")]
    [InlineData("v4.8.1", "4.8.0")]
    [InlineData("2.0.0", "2.0.0-beta.2")]
    public void Newer_versions_compare_higher(string available, string installed) => Assert.True(VersionComparer.IsUpdate(installed, available));

    [Theory]
    [InlineData("skill-v4.8.0", "skill-v*", true)]
    [InlineData("cli-v4.8.0", "skill-v*", false)]
    [InlineData("v1.2.3", null, true)]
    public void Release_streams_honor_wildcards(string tag, string? stream, bool expected) => Assert.Equal(expected, ReleaseStreamFilter.Matches(tag, stream));
}

