using Microsoft.AspNetCore.Mvc;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocalizationController : ControllerBase
{
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        {
            "en", new Dictionary<string, string>
            {
                { "sidebar.departments", "Departments" },
                { "sidebar.assets", "Assets" },
                { "sidebar.tickets", "Tickets" },
                { "grid.search", "Search..." },
                { "grid.new", "New" },
                { "grid.total", "Total {0} items" },
                { "departments.title", "Department Directory" },
                { "departments.id", "ID" },
                { "departments.name", "Name" },
                { "departments.manager", "Manager ID" },
                { "assets.title", "IT Assets Management" },
                { "assets.tag", "Asset Tag" },
                { "assets.category", "Category" },
                { "assets.model", "Model" },
                { "assets.serial", "S/N" },
                { "assets.status", "Status" },
                { "assets.assigned", "Assigned To" }
            }
        },
        {
            "tr", new Dictionary<string, string>
            {
                { "sidebar.departments", "Departmanlar" },
                { "sidebar.assets", "Zimmetler" },
                { "sidebar.tickets", "Talepler" },
                { "grid.search", "Ara..." },
                { "grid.new", "Yeni" },
                { "grid.total", "Toplam {0} kayıt" },
                { "departments.title", "Departman Dizini" },
                { "departments.id", "ID" },
                { "departments.name", "Departman Adı" },
                { "departments.manager", "Yönetici ID" },
                { "assets.title", "BT Zimmet Yönetimi" },
                { "assets.tag", "Zimmet Kodu" },
                { "assets.category", "Kategori" },
                { "assets.model", "Model" },
                { "assets.serial", "Seri No" },
                { "assets.status", "Durum" },
                { "assets.assigned", "Atanan Kişi" }
            }
        }
    };

    [HttpGet("{lang}")]
    public IActionResult GetTranslations(string lang)
    {
        var cleanLang = lang.Split('-').FirstOrDefault()?.ToLower() ?? "en";
        if (!Translations.TryGetValue(cleanLang, out var value))
        {
            return Ok(Translations["en"]);
        }
        return Ok(value);
    }
}
