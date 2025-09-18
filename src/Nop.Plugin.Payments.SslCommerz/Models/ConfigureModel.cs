using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.SslCommerz.Models
{
    public class ConfigureModel: ISettings
    {
        public string StoreId { get; set; }
        public string StorePassword { get; set; }
        public bool UseSandbox { get; set; }
    }
}
