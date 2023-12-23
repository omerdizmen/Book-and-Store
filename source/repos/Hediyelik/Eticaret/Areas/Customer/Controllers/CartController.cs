using Hediyelik.DataAccess.Repository.IRepository;
using Hediyelik.Models;
using Hediyelik.Models.ViewModels;
using Hediyelik.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;

namespace Eticaret.Areas.Customer.Controllers;
[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    [BindProperty] // post action'daki ve get actiondaki shoppingcartVM'yi birleştirir. böylelikle parametreye ihtiyaç kalmaz.
    public ShoppingCartVM ShoppingCartVM{ get; set; }

    public int OrderTotal { get; set; }
    public CartController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

        ShoppingCartVM = new ShoppingCartVM()
        {
            ListCart = _unitOfWork.Shopping.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties:"Product"),
            OrderHeader = new() 
        };

        foreach (var cart in ShoppingCartVM.ListCart)
        {
            cart.Price = GetPriceBasedQuantity(cart.Count, cart.Product.Price50, cart.Product.Price100);
            ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }

        return View(ShoppingCartVM);
    }
	[HttpPost]
	[ActionName("Summary")]
	[ValidateAntiForgeryToken]
	public IActionResult SummaryPOST(ShoppingCartVM ShoppingCartVM)
	{
        var claimsIndetity = (ClaimsIdentity)User.Identity;
        var claim = claimsIndetity.FindFirst(ClaimTypes.NameIdentifier);

        ShoppingCartVM.ListCart = _unitOfWork.Shopping.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product");

        ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
        ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
        ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
        ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;


		foreach (var cart in ShoppingCartVM.ListCart)
        {
            cart.Price = GetPriceBasedQuantity(cart.Count, cart.Product.Price50, cart.Product.Price100);
            ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }

		ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);
		if (applicationUser.CompanyId.GetValueOrDefault() == 0)
		{
			ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
			ShoppingCartVM.OrderHeader.OrderStatus = SD.PaymentStatusPending;
		}
		else
		{
			ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
			ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
		}

		_unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
        _unitOfWork.Save();

		foreach (var cart in ShoppingCartVM.ListCart)
		{
            OrderDetail orderDetail = new()
            {
                ProductId = cart.ProductId,
                OrderId = ShoppingCartVM.OrderHeader.Id,
                Price = cart.Price,
                Count = cart.Count
            };
            _unitOfWork.OrderDetail.Add(orderDetail);
            _unitOfWork.Save();
		}

		if (applicationUser.CompanyId.GetValueOrDefault() == 0)
		{
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
				SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
				CancelUrl = domain + "customer/cart/index",
			};

			foreach (var item in ShoppingCartVM.ListCart)
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
			ShoppingCartVM.OrderHeader.SessionId = session.Id;
			ShoppingCartVM.OrderHeader.PaymentIntentId = session.PaymentIntentId;
			_unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
			_unitOfWork.Save();
			Response.Headers.Add("Location", session.Url);
			return new StatusCodeResult(303);
		}
		else
		{
			return RedirectToAction("OrderConfirmation","Cart", new { id = ShoppingCartVM.OrderHeader.Id});
		}
	}

	public IActionResult OrderConfirmation(int id)
	{
		OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id);
		if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
		{
			var service = new SessionService();
			Session session = service.Get(orderHeader.SessionId);
			// check the stripe status
			if (session.PaymentStatus.ToLower() == "paid")
			{

				_unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
				_unitOfWork.Save();
			}
		}
		
		List<ShoppingCart> shoppingCarts = _unitOfWork.Shopping.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList() ;
		_unitOfWork.Shopping.RemoveRange(shoppingCarts);
		_unitOfWork.Save();

		return View(id);
	}

	public IActionResult Summary()
	{
		var claimsIndetity = (ClaimsIdentity)User.Identity;
		var claim = claimsIndetity.FindFirst(ClaimTypes.NameIdentifier);

		ShoppingCartVM = new ShoppingCartVM()
		{
			ListCart = _unitOfWork.Shopping.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product"),
			OrderHeader = new()
		};
		ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

		ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
		ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
		ShoppingCartVM.OrderHeader.StreetNubmer = ShoppingCartVM.OrderHeader.ApplicationUser.StreedAddress;
		ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
		ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
		ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;



		foreach (var cart in ShoppingCartVM.ListCart)
		{
			cart.Price = GetPriceBasedQuantity(cart.Count, cart.Product.Price50, cart.Product.Price100);
			ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
		}

		return View(ShoppingCartVM);

	}

	public  IActionResult Plus(int cartId)
    {
        var cart = _unitOfWork.Shopping.GetFirstOrDefault(u => u.Id == cartId);
        _unitOfWork.Shopping.IncrementCount(cart, 1);
        _unitOfWork.Save();

		return RedirectToAction(nameof(Index));
	}

	public IActionResult Minus(int cartId)
	{
		var cart = _unitOfWork.Shopping.GetFirstOrDefault(u => u.Id == cartId);

        if (cart.Count <= 1)
        {
			_unitOfWork.Shopping.Remove(cart);
        }
        else
        {
			_unitOfWork.Shopping.DecrementCount(cart, 1);
		}

		
		_unitOfWork.Save();

		return RedirectToAction(nameof(Index));
	}
	public IActionResult Remove(int cartId)
	{
		var cart = _unitOfWork.Shopping.GetFirstOrDefault(u => u.Id == cartId);
		_unitOfWork.Shopping.Remove(cart);
		_unitOfWork.Save();

		return RedirectToAction(nameof(Index));
	}

	private double GetPriceBasedQuantity(double quantity, double price50, double price100)
    {
        if (quantity <= 50)
        {
            return price50;
        }
        else
        {
            return price100;
        }
    }
}
