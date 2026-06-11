namespace CRMManagement.Application.Abstractions;

public sealed record ZohoImportStatusSnapshot(
    bool IsRunning,
    Guid? JobId,
    string? CurrentModule,
    string Phase,
    int Processed,
    int Total,
    int Percent,
    DateTime? StartedAt,
    DateTime? UpdatedAt,
    DateTime? CompletedAt,
    string? LastMessage);

public interface IZohoImportStatusService
{
    ZohoImportStatusSnapshot GetStatus();

    void Start(Guid jobId, string? currentModule);
    void SetPhase(string phase);
    void SetCurrentModule(string? module);
    void SetTotal(int total);
    void ReportProgress(int processed, string? message = null);
    void Complete(string? summary = null);
    void Fail(string message);
}
