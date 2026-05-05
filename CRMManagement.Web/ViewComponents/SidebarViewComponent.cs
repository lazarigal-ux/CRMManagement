using Microsoft.AspNetCore.Mvc;

namespace CRMManagement.Web.ViewComponents;

public sealed class SidebarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(bool showSidebar)
    {
        return View(new SidebarViewModel { ShowSidebar = showSidebar });
    }
}
