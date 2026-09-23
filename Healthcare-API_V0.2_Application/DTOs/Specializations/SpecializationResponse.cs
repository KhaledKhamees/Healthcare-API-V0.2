namespace Healthcare_API_V0._2_Application.DTOs.Specializations;

public sealed class SpecializationResponse
{
    public Guid SpecializationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
