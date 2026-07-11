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
                { "assets.assigned", "Assigned To" },
                { "form.error", "An error occurred during save." },
                { "form.success", "Saved successfully!" },
                { "api.error.generic", "An unexpected error occurred." },
                { "tickets.id", "Ticket ID" },
                { "tickets.state", "State" },
                { "tickets.deadline", "SLA Deadline" },
                { "tickets.create", "New Ticket" },
                { "tickets.templateId", "Template ID" },
                { "tickets.requesterId", "Requester ID" },
                { "dashboard.totalTickets", "Total Tickets" },
                { "dashboard.openTickets", "Open Tickets" },
                { "dashboard.breachedSla", "Breached SLAs" },
                { "dashboard.totalAssets", "Total Assets" },
                { "sidebar.leaves", "Leaves" },
                { "leaves.type", "Leave Type" },
                { "leaves.startDate", "Start Date" },
                { "leaves.endDate", "End Date" },
                { "leaves.status", "Status" },
                { "leaves.reason", "Reason" },
                { "leaves.create", "New Leave Request" },
                { "leaves.approve", "Approve" },
                { "sidebar.auditlogs", "Audit Logs" },
                { "auditlogs.timestamp", "Timestamp" },
                { "auditlogs.action", "Action" },
                { "auditlogs.entityType", "Entity Type" },
                { "auditlogs.details", "Details" },
                { "grid.noData", "No data" },
                { "forms.leaveRequest.title", "New Leave Request" },
                { "leave.type", "Leave Type" },
                { "leave.startDate", "Start Date" },
                { "leave.endDate", "End Date" },
                { "leave.reason", "Reason" },
                { "forms.department.title", "New Department" },
                { "department.name", "Department Name" },
                { "action.submit", "Submit" },
                { "action.cancel", "Cancel" },
                { "sidebar.dashboard", "Dashboard" },
                { "dashboard.tickets", "Open Tickets" },
                { "dashboard.slabreaches", "SLA Breaches" },
                { "dashboard.leaves", "Pending Leaves" },
                { "dashboard.assets", "My Assets" },
                { "departments.create", "New Department" },
                { "assets.create", "New Asset" },
                { "form.required", "This field is required" },
                { "grid.actions", "Actions" },
                { "forms.selectTemplate", "Please select a template to continue:" },
                { "forms.selectTemplatePlaceholder", "Select Template" }
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
                { "assets.assigned", "Atanan Kişi" },
                { "form.error", "Kaydetme sırasında bir hata oluştu." },
                { "form.success", "Başarıyla kaydedildi!" },
                { "api.error.generic", "Beklenmeyen bir hata oluştu." },
                { "tickets.id", "Bilet ID" },
                { "tickets.state", "Durum" },
                { "tickets.deadline", "SLA Bitiş" },
                { "tickets.create", "Yeni Bilet" },
                { "tickets.templateId", "Şablon ID" },
                { "tickets.requesterId", "Talep Eden ID" },
                { "dashboard.totalTickets", "Toplam Bilet" },
                { "dashboard.openTickets", "Açık Biletler" },
                { "dashboard.breachedSla", "SLA Aşımları" },
                { "dashboard.totalAssets", "Toplam Zimmet" },
                { "sidebar.leaves", "İzinler" },
                { "leaves.type", "İzin Türü" },
                { "leaves.startDate", "Başlangıç Tarihi" },
                { "leaves.endDate", "Bitiş Tarihi" },
                { "leaves.status", "Durum" },
                { "leaves.reason", "Açıklama" },
                { "leaves.create", "Yeni İzin Talebi" },
                { "leaves.approve", "Onayla" },
                { "sidebar.auditlogs", "Denetim Kayıtları" },
                { "auditlogs.timestamp", "Tarih/Saat" },
                { "auditlogs.action", "İşlem" },
                { "auditlogs.entityType", "Modül" },
                { "auditlogs.details", "Detaylar" },
                { "grid.noData", "Veri bulunamadı" },
                { "forms.leaveRequest.title", "Yeni İzin Talebi" },
                { "leave.type", "İzin Tipi" },
                { "leave.startDate", "Başlangıç Tarihi" },
                { "leave.endDate", "Bitiş Tarihi" },
                { "leave.reason", "Gerekçe" },
                { "forms.department.title", "Yeni Departman" },
                { "department.name", "Departman Adı" },
                { "action.submit", "Kaydet" },
                { "action.cancel", "İptal" },
                { "sidebar.dashboard", "Gösterge Paneli" },
                { "dashboard.tickets", "Açık Biletler" },
                { "dashboard.slabreaches", "SLA Aşımları" },
                { "dashboard.leaves", "Bekleyen İzinler" },
                { "dashboard.assets", "Zimmetlerim" },
                { "departments.create", "Yeni Departman" },
                { "assets.create", "Yeni Zimmet" },
                { "form.required", "Bu alan zorunludur" },
                { "grid.actions", "İşlemler" },
                { "forms.selectTemplate", "Devam etmek için bir şablon seçin:" },
                { "forms.selectTemplatePlaceholder", "Şablon Seçin" }
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
