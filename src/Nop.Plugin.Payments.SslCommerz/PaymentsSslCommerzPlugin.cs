using System.Threading.Tasks;
using Nop.Plugin.Payments.SslCommerz.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Plugins;

namespace Nop.Plugin.Payments.SslCommerz
{
    public class PaymentsSslCommerzPlugin 
    {
        private readonly ISettingService _settingService;

        public PaymentsSslCommerzPlugin(ISettingService settingService)
        {
            _settingService = settingService;
        }

        //public override async Task InstallAsync()
        //{
        //    var settings = new SslCommerzPaymentSettings
        //    {
        //        StoreId = "",
        //        StorePassword = "",
        //        UseSandbox = true,
        //        IsActive = true
        //    };
        //    await _settingService.SaveSettingAsync(settings);
        //    await base.InstallAsync();
        //}

        //public override async Task UninstallAsync()
        //{
        //    await _settingService.DeleteSettingAsync<SslCommerzPaymentSettings>();
        //    await base.UninstallAsync();
        //}
    }
}
