using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Services.Configuration;
using System.Threading.Tasks;
using Nop.Plugin.Payments.SslCommerz.Models;
using Nop.Web.Framework;

namespace Nop.Plugin.Payments.SslCommerz.Controllers
{
    [Area(AreaNames.ADMIN)]
    [AuthorizeAdmin]
    public class AdminPaymentSslCommerzController : BasePluginController
    {
        private readonly ISettingService _settingService;

        public AdminPaymentSslCommerzController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        [HttpGet]
        public async Task<IActionResult> Configure()
        {
            var settings = await _settingService.LoadSettingAsync<SslCommerzPaymentSettings>();
            var model = new ConfigureModel
            {
                StoreId = settings.StoreId,
                StorePassword = settings.StorePassword,
                UseSandbox = settings.UseSandbox
            };
            return View("~/Plugins/Payments.SslCommerz/Views/Admin/Configure.cshtml", model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Configure(ConfigureModel model)
        {
            var settings = new SslCommerzPaymentSettings
            {
                StoreId = model.StoreId,
                StorePassword = model.StorePassword,
                UseSandbox = model.UseSandbox,
                IsActive = true
            };

            await _settingService.SaveSettingAsync(settings);
            ViewBag.Success = "Settings saved";
            return View("~/Plugins/Payments.SslCommerz/Views/Admin/Configure.cshtml", model);
        }
    }
}
