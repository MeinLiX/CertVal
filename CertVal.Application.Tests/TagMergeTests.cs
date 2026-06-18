using CertVal.Application.Common.Certificates;
using Xunit;

namespace CertVal.Application.Tests;

public class TagMergeTests
{
    [Fact]
    public void Add_AppendsNewTags_PreservingOrder()
    {
        var result = TagMerge.Add(["prod"], ["team:payments", "eu"]);
        Assert.Equal(["prod", "team:payments", "eu"], result);
    }

    [Fact]
    public void Add_IsCaseInsensitiveDedup()
    {
        var result = TagMerge.Add(["Prod"], ["prod", "PROD"]);
        Assert.Single(result);
        Assert.Equal("Prod", result[0]);
    }

    [Fact]
    public void Add_TrimsAndDropsEmpties()
    {
        var result = TagMerge.Add([], ["  api ", "", "   "]);
        Assert.Equal(["api"], result);
    }

    [Fact]
    public void Remove_IsCaseInsensitive()
    {
        var result = TagMerge.Remove(["Prod", "eu", "api"], ["PROD", "API"]);
        Assert.Equal(["eu"], result);
    }

    [Fact]
    public void Remove_NonExistentTag_NoOp()
    {
        var result = TagMerge.Remove(["prod"], ["staging"]);
        Assert.Equal(["prod"], result);
    }
}
