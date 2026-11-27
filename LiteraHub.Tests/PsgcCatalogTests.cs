using LiteraHub.Services.Students;
using Xunit;

namespace LiteraHub.Tests;

public sealed class PsgcCatalogTests
{
    private static PsgcCatalog Catalog => PsgcCatalog.Instance;

    [Fact]
    public void EmbeddedSnapshotHasExpectedCountsAndValidHierarchy()
    {
        Assert.Equal(18, Catalog.Regions.Count);
        int provinces = 0, localities = 0, barangays = 0;
        var codes = new HashSet<string>(StringComparer.Ordinal);
        void CheckCode(string code)
        {
            Assert.Matches("^[0-9]{10}$", code);
            Assert.True(codes.Add(code), $"Duplicate PSGC code: {code}");
        }
        foreach (var region in Catalog.Regions)
        {
            CheckCode(region.Code);
            var children = Catalog.Provinces(region.Code);
            provinces += children.Count;
            foreach (var province in children)
            {
                CheckCode(province.Code);
                Assert.Equal(region.Code, province.RegionCode);
            }
            foreach (var provinceCode in children.Select(p => (string?)p.Code).Append(null))
            foreach (var locality in Catalog.Localities(region.Code, provinceCode))
            {
                CheckCode(locality.Code);
                localities++;
                Assert.Equal(region.Code, locality.RegionCode);
                Assert.Equal(provinceCode, locality.ProvinceCode);
                foreach (var barangay in Catalog.Barangays(locality.Code))
                {
                    CheckCode(barangay.Code);
                    barangays++;
                    Assert.True(Catalog.TryResolve(region.Code, provinceCode, locality.Code, barangay.Code, out var address));
                    Assert.NotNull(address);
                    Assert.Equal(barangay.Code, address!.Barangay.Code);
                }
            }
        }
        Assert.Equal(82, provinces);
        Assert.Equal(1642, localities);
        Assert.Equal(42010, barangays);
    }

    [Fact]
    public void RejectsWrongParentAndMissingCodes()
    {
        var region = Catalog.Regions.First(r => Catalog.Provinces(r.Code).Any());
        var province = Catalog.Provinces(region.Code).First(p => Catalog.Localities(region.Code, p.Code).Any());
        var locality = Catalog.Localities(region.Code, province.Code).First();
        var barangay = Catalog.Barangays(locality.Code).First();
        var otherRegion = Catalog.Regions.First(r => r.Code != region.Code);
        Assert.False(Catalog.TryResolve(otherRegion.Code, province.Code, locality.Code, barangay.Code, out _));
        Assert.False(Catalog.TryResolve(region.Code, null, locality.Code, barangay.Code, out _));
        Assert.False(Catalog.TryResolve(region.Code, province.Code, locality.Code, null, out _));
        Assert.False(Catalog.TryResolve(region.Code, province.Code, locality.Code, "0000000000", out _));
    }

    [Fact]
    public void SupportsProvinceFreeLocalities()
    {
        var region = Catalog.Regions.First(r => Catalog.Localities(r.Code, null).Any());
        var locality = Catalog.Localities(region.Code, null).First();
        var barangay = Catalog.Barangays(locality.Code).First();
        Assert.True(Catalog.TryResolve(region.Code, null, locality.Code, barangay.Code, out var address));
        Assert.Null(address!.Province);
    }

    [Fact]
    public void CatalogIsCached() => Assert.Same(Catalog, PsgcCatalog.Instance);
}
