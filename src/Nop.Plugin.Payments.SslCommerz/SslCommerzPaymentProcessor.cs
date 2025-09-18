using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.SslCommerz.Models;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;

namespace Nop.Plugin.Payments.SslCommerz
{
    public class SslCommerzPaymentProcessor : BasePlugin, IPaymentMethod
    {
        private readonly ISettingService _settingService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SslCommerzPaymentProcessor> _logger;
        private readonly SslCommerzPaymentSettings _settings;
        protected readonly IWebHelper _webHelper;
        private readonly ICustomerService _customerService;
        private readonly IAddressService _addressService;
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;

        public SslCommerzPaymentProcessor(
            ISettingService settingService,
            IHttpClientFactory httpClientFactory,
            ILogger<SslCommerzPaymentProcessor> logger, IWebHelper webHelper,
            ICustomerService customerService, IAddressService addressService, IOrderService orderService
            , IProductService productService)
        {
            _settingService = settingService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _webHelper = webHelper;
            _customerService = customerService;
            _addressService = addressService;

            _settings = _settingService.LoadSettingAsync<SslCommerzPaymentSettings>().GetAwaiter().GetResult();
            _orderService = orderService;
            _productService = productService;
        }

        public bool SupportCapture => false;
        public bool SupportPartiallyRefund => false;
        public bool SupportRefund => false;
        public bool SupportVoid => false;
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;
       // public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;
        public bool SkipPaymentInfo => true;
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        //public PluginDescriptor PluginDescriptor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        //public string GetConfigurationPageUrl() => "/Views/Admin//Configure.cshtml";

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/AdminPaymentSslCommerz/Configure";

            //return "/Admin/AdminPaymentSslCommerz/Configure";
        }

      

        public Type GetPublicViewComponent() => null;

        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form) => Task.FromResult<IList<string>>(new List<string>());
        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form) => Task.FromResult(new ProcessPaymentRequest());
        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request) =>
            Task.FromResult(new ProcessPaymentResult { NewPaymentStatus = Core.Domain.Payments.PaymentStatus.Pending });

        public async Task PostProcessPaymentAsync(PostProcessPaymentRequest request)
        {
            var order = request.Order;

            // load order items explicitly (learning purpose)
            var orderItems = await _orderService.GetOrderItemsAsync(order.Id);


            foreach (var item in orderItems)
            {
                // Load the actual product using ProductId
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                var productName = product?.Name ?? "Unknown Product";

                var quantity = item.Quantity;
                var price = item.UnitPriceInclTax;

                Console.WriteLine($"Product: {productName}, Qty: {quantity}, Price: {price}");
            }


            //  Load full customer object
            var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);

            var shipping = order.ShippingAddressId;

            //  Order information
            var orderId = order.Id;
            var orderTotal = order.OrderTotal;
            var currency = order.CustomerCurrencyCode;

            // Now you can access details safely
            var customerName = customer.FirstName;
            var customerEmail = customer?.Email ?? "demo@gmail.com";
            var customerPhone = customer.Phone ?? "";
            var customerCountry =customer.County;
            var customerCity = customer.City;
          
            

            var billing = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
            var billingEmail = billing?.Email ?? "";
            var billingPhone = billing?.PhoneNumber ?? "1234";
            var billingCity = billing?.City ?? "";

            // Get Shipping Address (if exists)
            var shippingaddress = order.ShippingAddressId.HasValue
                ? await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value)
                : null;

            var shippigaddress = shippingaddress?.Address1 ?? "Demo Address";

            //learning purpose end





            var url = _settings.UseSandbox
                ? "https://sandbox.sslcommerz.com/gwprocess/v4/api.php"
                : "https://securepay.sslcommerz.com/gwprocess/v4/api.php";

            var baseUrl = _webHelper.GetStoreLocation();  // e.g. https://localhost:5001/

            //var callbackUrl = $"{baseUrl}/Plugins/Payments.SslCommerz/Callback"; // We'll implement this controller next
            var callbackUrl = $"{baseUrl}SslCommerz/Callback";

            var data = new Dictionary<string, string>
            {
                {"store_id", _settings.StoreId},
                {"store_passwd", _settings.StorePassword},
                {"total_amount", orderTotal.ToString("F2")},
                {"currency", order.CustomerCurrencyCode},
                {"tran_id", order.Id.ToString()},
                {"success_url", callbackUrl},
                {"fail_url", callbackUrl},
                {"cancel_url", callbackUrl},
                {"cus_name", customerName},
                {"cus_email", customerEmail},
                {"cus_add1", shippigaddress},
                {"cus_city", customerCity},
                {"shipping_method", "NO" },
                {"cus_country", customerCountry},
                {"cus_phone",  "1234"},
                {"product_name", "UD" },
                {"product_category", "Service"},
                {"product_profile", "general" },
                {"cus_postcode", "1219" },
            };

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(url, new FormUrlEncodedContent(data));
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("GatewayPageURL", out var gateway))
                {
                    var gatewayUrl = gateway.GetString();
                    if (!string.IsNullOrEmpty(gatewayUrl))
                    {
                        var httpContext = EngineContext.Current.Resolve<IHttpContextAccessor>().HttpContext;
                        httpContext.Response.Redirect(gatewayUrl);
                    }
                }
            }
            else
            {
                _logger.LogError("SSLCommerz Error: " + body);
                throw new Exception("SSLCommerz payment initialization failed.");
            }
        }

        public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart) => Task.FromResult(0m);
        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart) => Task.FromResult(false);
        public Task<bool> CanRePostProcessPaymentAsync(Order order) => Task.FromResult(true);
        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest r) => Task.FromResult(new CancelRecurringPaymentResult());
        public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest r) => Task.FromResult(new CapturePaymentResult());
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest r) => Task.FromResult(new ProcessPaymentResult());
        public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest r) => Task.FromResult(new RefundPaymentResult());
        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest r) => Task.FromResult(new VoidPaymentResult());
        public Task<string> GetPaymentMethodDescriptionAsync() => Task.FromResult("Pay with SSLCommerz");
   


        public override async Task InstallAsync()
        {
            var settings = new SslCommerzPaymentSettings
            {
                StoreId = "",
                StorePassword = "",
                UseSandbox = true,
                IsActive = true
            };
            await _settingService.SaveSettingAsync(settings);
            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await _settingService.DeleteSettingAsync<SslCommerzPaymentSettings>();
            await base.UninstallAsync();
        }

        public Task UpdateAsync(string currentVersion, string targetVersion)
        {
            throw new NotImplementedException();
        }

        public override async Task PreparePluginToUninstallAsync()
        {
            await _settingService.DeleteSettingAsync<SslCommerzPaymentSettings>();
            await base.UninstallAsync();
        }
    }
}
