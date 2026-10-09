using System.ComponentModel.DataAnnotations;

namespace iDoctor.Api.Dtos;

public class DoctorSearchQuery
{
    [MaxLength(100)] public string? Q { get; set; }              // name search
    public int? SpecialtyId { get; set; }
    public int? ClinicId { get; set; }
    public int? CityId { get; set; }                              // doctor works in a clinic in this city
    [Range(0, 70)] public short? MinExperience { get; set; }
    [Range(0, 100000)] public decimal? MaxPrice { get; set; }

    // rating (default) | experience | price_asc | price_desc | name
    [RegularExpression("^(rating|experience|price_asc|price_desc|name)$",
        ErrorMessage = "sortBy must be one of: rating, experience, price_asc, price_desc, name.")]
    public string? SortBy { get; set; }

    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 50)] public int PageSize { get; set; } = 12;
}

public class DoctorListItemDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? PhotoUrl { get; set; }
    public short ExperienceYears { get; set; }
    public decimal? ConsultationPrice { get; set; }
    public decimal AverageRating { get; set; }
    public List<SpecialtyDto> Specialties { get; set; } = new();
    public List<ClinicSummaryDto> Clinics { get; set; } = new();
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}