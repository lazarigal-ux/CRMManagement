namespace CRMManagement.Application.Abstractions;

public interface ICompanyContext
{
    int? SelectedCompanyId { get; }
    bool HasCompany => SelectedCompanyId.HasValue && SelectedCompanyId.Value > 0;
}
