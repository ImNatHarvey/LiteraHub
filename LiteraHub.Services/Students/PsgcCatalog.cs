using System.Text.Json;

namespace LiteraHub.Services.Students;

public sealed record PsgcRegion(string Code, string Name);
public sealed record PsgcProvince(string Code, string Name, string RegionCode);
public sealed record PsgcLocality(string Code, string Name, string RegionCode, string? ProvinceCode, string Level, string? CityClass);
public sealed record PsgcBarangay(string Code, string Name, string RegionCode, string CityMunicipalityCode, string? DistrictCode);
public sealed record PsgcDistrict(string Code, string Name, string RegionCode, string CityMunicipalityCode);
public sealed record PsgcAddress(PsgcRegion Region, PsgcProvince? Province, PsgcLocality Locality, PsgcBarangay Barangay);

/// <summary>Immutable, session-cached geographic catalog from the embedded PSA resource.</summary>
public sealed class PsgcCatalog
{
    private sealed class Dataset
    {
        public Dataset() { }
        public PsgcRegion[] Regions { get; set; } = [];
        public PsgcProvince[] Provinces { get; set; } = [];
        public PsgcLocality[] CitiesMunicipalities { get; set; } = [];
        public PsgcBarangay[] Barangays { get; set; } = [];
        public PsgcDistrict[] Districts { get; set; } = [];
    }
    private static readonly Lazy<PsgcCatalog> Cached = new(Load);
    public static PsgcCatalog Instance => Cached.Value;
    private readonly Dictionary<string, PsgcRegion> _regions;
    private readonly Dictionary<string, PsgcProvince> _provinces;
    private readonly Dictionary<string, PsgcLocality> _localities;
    private readonly Dictionary<string, PsgcBarangay> _barangays;
    private readonly Dictionary<string, PsgcDistrict> _districts;
    private readonly ILookup<string, PsgcProvince> _provincesByRegion;
    private readonly ILookup<string, PsgcLocality> _localitiesByRegion;
    private readonly ILookup<string, PsgcBarangay> _barangaysByLocality;
    public IReadOnlyList<PsgcRegion> Regions { get; }
    private PsgcCatalog(Dataset data)
    {
        _regions = data.Regions.ToDictionary(x => x.Code);
        _provinces = data.Provinces.ToDictionary(x => x.Code);
        _localities = data.CitiesMunicipalities.ToDictionary(x => x.Code);
        _barangays = data.Barangays.ToDictionary(x => x.Code);
        _districts = data.Districts.ToDictionary(x => x.Code);
        if (_regions.Count == 0 || _localities.Count == 0 || _barangays.Count == 0)
            throw new InvalidDataException("The PSGC resource is empty or invalid.");
        Regions = Array.AsReadOnly(data.Regions.OrderBy(x => x.Name).ToArray());
        _provincesByRegion = data.Provinces.OrderBy(x => x.Name).ToLookup(x => x.RegionCode);
        _localitiesByRegion = data.CitiesMunicipalities.OrderBy(x => x.Name).ToLookup(x => x.RegionCode);
        _barangaysByLocality = data.Barangays.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToLookup(x => x.CityMunicipalityCode);
        foreach (var p in data.Provinces)
            if (!_regions.ContainsKey(p.RegionCode)) throw new InvalidDataException("Invalid PSGC province parent.");
        foreach (var c in data.CitiesMunicipalities)
            if (!_regions.ContainsKey(c.RegionCode) || (c.ProvinceCode is not null && (!_provinces.TryGetValue(c.ProvinceCode, out var p) || p.RegionCode != c.RegionCode)))
                throw new InvalidDataException("Invalid PSGC locality parent.");
        foreach (var b in data.Barangays)
            if (!_localities.TryGetValue(b.CityMunicipalityCode, out var c) || c.RegionCode != b.RegionCode ||
                (b.DistrictCode is not null && (!_districts.TryGetValue(b.DistrictCode, out var d) || d.CityMunicipalityCode != c.Code)))
                throw new InvalidDataException("Invalid PSGC barangay parent.");
    }
    private static PsgcCatalog Load()
    {
        using var stream = typeof(PsgcCatalog).Assembly.GetManifestResourceStream("LiteraHub.PSGC.Data")
            ?? throw new InvalidDataException("The embedded PSGC dataset is missing.");
        var data = JsonSerializer.Deserialize<Dataset>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("The embedded PSGC dataset is invalid.");
        return new PsgcCatalog(data);
    }
    public IReadOnlyList<PsgcProvince> Provinces(string? region) => region is null ? [] : _provincesByRegion[region].ToArray();
    public IReadOnlyList<PsgcLocality> Localities(string? region, string? province) => region is null ? [] : _localitiesByRegion[region].Where(x => x.ProvinceCode == province).ToArray();
    public IReadOnlyList<PsgcBarangay> Barangays(string? locality) => locality is null ? [] : _barangaysByLocality[locality].ToArray();
    public string BarangayCaption(PsgcBarangay b) => b.DistrictCode is not null && _districts.TryGetValue(b.DistrictCode, out var d) ? $"{b.Name} ({d.Name})" : b.Name;
    public bool TryResolve(string? region, string? province, string? locality, string? barangay, out PsgcAddress? result)
    {
        result = null;
        if (region is null || locality is null || barangay is null || !_regions.TryGetValue(region, out var r) ||
            !_localities.TryGetValue(locality, out var c) || c.RegionCode != region || c.ProvinceCode != province ||
            !_barangays.TryGetValue(barangay, out var b) || b.CityMunicipalityCode != locality || b.RegionCode != region) return false;
        PsgcProvince? p = null;
        if (province is not null && (!_provinces.TryGetValue(province, out p) || p.RegionCode != region)) return false;
        result = new PsgcAddress(r, p, c, b); return true;
    }
}
