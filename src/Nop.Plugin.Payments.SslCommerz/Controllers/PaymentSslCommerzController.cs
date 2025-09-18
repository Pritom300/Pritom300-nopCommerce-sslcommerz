using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nop.Core.Domain.Orders;
using Nop.Services.Orders;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.SslCommerz.Controllers
{
    public class SslCommerzController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<SslCommerzController> _logger;

        public SslCommerzController(
            IOrderService orderService,
            ILogger<SslCommerzController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpPost]
        
        public async Task<IActionResult> Callback()
        {
            var form = Request.HasFormContentType ? Request.Form : null;
            if (form == null || !form.ContainsKey("tran_id"))
            {
                return Content("Invalid callback");
            }

            var tranId = form["tran_id"].ToString();
            var status = form["status"].ToString(); // "VALID", "FAILED", "CANCELLED"
            var orderId = int.Parse(tranId);

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogError("SSLCommerz callback: Order not found, TranId: " + tranId);
                return Content("Order not found");
            }

            if (status == "VALID")
            {
                // Mark order as Paid
                order.PaymentStatus = Nop.Core.Domain.Payments.PaymentStatus.Paid;
                await _orderService.UpdateOrderAsync(order);

                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }
            else if (status == "FAILED")
            {
                order.PaymentStatus = Nop.Core.Domain.Payments.PaymentStatus.Voided;
                await _orderService.UpdateOrderAsync(order);

                return Content("Payment failed.");
            }
            else if (status == "CANCELLED")
            {
                order.PaymentStatus = Nop.Core.Domain.Payments.PaymentStatus.Voided;
                await _orderService.UpdateOrderAsync(order);

                return Content("Payment cancelled.");
            }

            return Content("Unknown status: " + status);
        }
    }
}
