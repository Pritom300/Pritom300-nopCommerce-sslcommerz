using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.SslCommerz.Models
{
    public class SslCommerzPaymentSettings : ISettings
    {
        public string StoreId { get; set; }
        public string StorePassword { get; set; }
        public bool UseSandbox { get; set; }
        public bool IsActive { get; set; } // required for Enable toggle
    }
}
