using Hediyelik.DataAccess.Repository;
using Hediyelik.DataAccess.Repository.IRepository;
using Hediyelik.Models;
using Hediyelik.Models.ViewModels;
using Hediyelik.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace Eticaret.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class OrderController : Controller
{
	private readonly IUnitOfWork _unitOfWork;
	[BindProperty]
	public OrderVM orderVM { get; set; }

	public OrderController(IUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;
	}

	public IActionResult Index()
	{
		return View();
	}

    public IActionResult Details(int orderId)
    {
		orderVM = new OrderVM
		{
			OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderId, includeProperties: "ApplicationUser"),
			OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.Id == orderId, includeProperties: "Product")
		};
        return View(orderVM);
    }

    [ActionName("Details")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Details_PAY_NOW()
    {
        orderVM.OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
        orderVM.OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.Id == orderVM.OrderHeader.Id, includeProperties: "Product");

        // stripe settings
        var domain = "https://localhost:44367/";
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string>
            {
                "card",
            },

            LineItems = new List<SessionLineItemOptions>()
            ,
            Mode = "payment",
            SuccessUrl = domain + $"customer/order/PaymentConfirmation?orderHeaderid={orderVM.OrderHeader.Id}",
            CancelUrl = domain + "admin/order/details?orderId={orderVM.OrderHeader.Id}",
        };

        foreach (var item in orderVM.OrderDetail)
        {
            var sessionLinteItem = new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(item.Price * 100),// 20.00
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.Product.Title,
                    }
                },
                Quantity = item.Count
            };
            options.LineItems.Add(sessionLinteItem);
        }

        var service = new SessionService();
        Session session = service.Create(options);

        //ShoppingCartVM.OrderHeader.SessionId = session.Id;
        //ShoppingCartVM.OrderHeader.PaymentIntentId = session.PaymentIntentId;

        _unitOfWork.OrderHeader.UpdateStripePaymentId(orderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
        _unitOfWork.Save();
        Response.Headers.Add("Location", session.Url);
        return new StatusCodeResult(303);        
    }

    public IActionResult PaymentConfirmation(int orderHeaderid)
    {
        OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderHeaderid);
        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);
            // check the stripe status
            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderid, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                _unitOfWork.Save();
            }
        }

        List<ShoppingCart> shoppingCarts = _unitOfWork.Shopping.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
        _unitOfWork.Shopping.RemoveRange(shoppingCarts);
        _unitOfWork.Save();

        return View(orderHeaderid);
    }

    [ValidateAntiForgeryToken]
    [Authorize(Roles =SD.Role_Admin+","+SD.Role_Employee)]
	[HttpPost]
    public IActionResult UpdateOrderDetail()
    {
		var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderVM.OrderHeader.Id, tracked:false);

		orderHeaderFromDb.Name = orderVM.OrderHeader.Name;
        orderHeaderFromDb.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
        orderHeaderFromDb.StreetNubmer = orderVM.OrderHeader.StreetNubmer;
        orderHeaderFromDb.City = orderVM.OrderHeader.City;
        orderHeaderFromDb.State = orderVM.OrderHeader.State;
        orderHeaderFromDb.PostalCode = orderVM.OrderHeader.PostalCode;

		_unitOfWork.OrderHeader.Update(orderHeaderFromDb);
		_unitOfWork.Save();
		TempData["Success"] = "Order Details Updated Successfully";
		return RedirectToAction("Details", "Order", new { orderId = orderHeaderFromDb.Id });
    }

    [ValidateAntiForgeryToken]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    [HttpPost]
    public IActionResult StartProcessing()
    {       

		_unitOfWork.OrderHeader.UpdateStatus(orderVM.OrderHeader.Id,SD.StatusInProcess);

        _unitOfWork.Save();
        TempData["Success"] = "Order Status Updated Successfully";
        return RedirectToAction("Details", "Order", new { orderId = orderVM.OrderHeader.Id });
    }

    [ValidateAntiForgeryToken]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    [HttpPost]
    public IActionResult ShipOrder()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderVM.OrderHeader.Id, tracked: false);

		orderHeaderFromDb.OrderStatus = SD.StatusShipped;
		orderHeaderFromDb.ShippingDate = DateTime.Now;

        if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            orderHeaderFromDb.PaymentDueDate = DateTime.Now.AddDays(30);
        }

		_unitOfWork.OrderHeader.Update(orderHeaderFromDb);

        _unitOfWork.Save();
        TempData["Success"] = "Order Shipped Updated Successfully";
        return RedirectToAction("Details", "Order", new { orderId = orderVM.OrderHeader.Id });
    }

    [ValidateAntiForgeryToken]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    [HttpPost]
    public IActionResult CancelOrder()
    {
        var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderVM.OrderHeader.Id, tracked: false);
        if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
        {
            var options = new RefundCreateOptions
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = orderHeader.PaymentIntentId
            };

            var service = new RefundService();
            Refund refund = service.Create(options);

            _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id,SD.StatusCancelled,SD.StatusRefunded);
        }
        else
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
        }

        _unitOfWork.Save();

        IList<ShoppingCart> meh;

        TempData["Success"] = "Order Cancelled Successfully";
        return RedirectToAction("Details", "Order", new { orderId = orderVM.OrderHeader.Id });
    }

    #region API CALLS
    [HttpGet]
	public IActionResult GetAll(string status)
	{
		IEnumerable<OrderHeader> orderHeaders;

		if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
		{
            orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
		}
		else
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
			orderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "ApplicationUser");
		}

		

        switch (status)
		{
            case "pending":
				orderHeaders = orderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                break;
			case "inprocess":
                orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                break;
            case "completed":
				Console.WriteLine("girdi mi");
                orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                break;
			default:                
                break;
        }	

		return Json(new
		{
			data = orderHeaders
		});
	}
	#endregion
}
