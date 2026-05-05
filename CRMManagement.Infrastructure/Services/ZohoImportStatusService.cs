using CRMManagement.Application.Abstractions;

namespace CRMManagement.Infrastructure.Services;

public sealed class ZohoImportStatusService : IZohoImportStatusService
{
    private readonly object _lock = new();

    private bool _isRunning;
    private Guid? _jobId;
    private string? _currentModule;
    private string _phase = "";
    private int _processed;
    private int _total;
    private DateTime? _startedAt;
    private DateTime? _updatedAt;
    private DateTime? _completedAt;
    private string? _lastMessage;

    public ZohoImportStatusSnapshot GetStatus()
    {
        lock (_lock)
        {
            var percent = (_total > 0) ? Math.Clamp((int)Math.Round(100.0 * _processed / _total), 0, 100) : 0;
            return new ZohoImportStatusSnapshot(
                _isRunning, _jobId, _currentModule, _phase,
                _processed, _total, percent,
                _startedAt, _updatedAt, _completedAt, _lastMessage);
        }
    }

    public void Start(Guid jobId, string? currentModule)
    {
        lock (_lock)
        {
            _isRunning = true;
            _jobId = jobId;
            _currentModule = currentModule;
            _phase = "Starting";
            _processed = 0;
            _total = 0;
            _startedAt = DateTime.UtcNow;
            _updatedAt = DateTime.UtcNow;
            _completedAt = null;
            _lastMessage = null;
        }
    }

    public void SetPhase(string phase)
    {
        lock (_lock)
        {
            _phase = phase ?? "";
            _updatedAt = DateTime.UtcNow;
        }
    }

    public void SetCurrentModule(string? module)
    {
        lock (_lock)
        {
            _currentModule = module;
            _updatedAt = DateTime.UtcNow;
        }
    }

    public void SetTotal(int total)
    {
        lock (_lock)
        {
            _total = total;
            _updatedAt = DateTime.UtcNow;
        }
    }

    public void ReportProgress(int processed, string? message = null)
    {
        lock (_lock)
        {
            _processed = processed;
            if (message != null) _lastMessage = message;
            _updatedAt = DateTime.UtcNow;
        }
    }

    public void Complete(string? summary = null)
    {
        lock (_lock)
        {
            _isRunning = false;
            _phase = "Completed";
            _completedAt = DateTime.UtcNow;
            _updatedAt = DateTime.UtcNow;
            if (summary != null) _lastMessage = summary;
        }
    }

    public void Fail(string message)
    {
        lock (_lock)
        {
            _isRunning = false;
            _phase = "Failed";
            _completedAt = DateTime.UtcNow;
            _updatedAt = DateTime.UtcNow;
            _lastMessage = message;
        }
    }
}
