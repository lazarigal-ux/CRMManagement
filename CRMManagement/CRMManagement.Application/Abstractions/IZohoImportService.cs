using CRMManagement.Application.DTOs;

namespace CRMManagement.Application.Abstractions;

public interface IZohoImportService
{
    /// <summary>Inserts a "Running" job row, kicks off the import on a background Task, and returns the job id.</summary>
    Task<Guid> StartImportAsync(ZohoImportRequest request, CancellationToken ct);

    /// <summary>Inserts a "Running" job row and runs the import inline. Returns the final job DTO.
    /// Used by the internal run-and-wait endpoint so an external agent can block until the local DB mirror is refreshed.</summary>
    Task<ZohoImportJobDto> RunImportAndWaitAsync(ZohoImportRequest request, CancellationToken ct);

    Task<ZohoImportJobDto?> GetLatestJobAsync(CancellationToken ct);

    Task<ZohoImportJobDto?> GetJobAsync(Guid jobId, CancellationToken ct);
}
