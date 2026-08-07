namespace ContosoUniversity.Web.ViewModels;

/// <summary>
/// ViewModel for the Error view.
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
