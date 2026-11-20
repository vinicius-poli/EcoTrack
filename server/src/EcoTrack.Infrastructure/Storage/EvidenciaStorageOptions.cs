namespace EcoTrack.Infrastructure.Storage;

public sealed class EvidenciaStorageOptions
{
    public const string SectionName = "EvidenciaStorage";

    public string BasePath { get; set; } = "App_Data/evidencias";
}