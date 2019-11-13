﻿using WebGoatCore.Models;
using WebGoatCore.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace WebGoatCore.Controllers
{
    [Route("[controller]/[action]")]
    public class CartController : Controller
    {
        private readonly ProductRepository _productRepository;

        public CartController(ProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public IActionResult Index()
        {
            if (!HttpContext.Session.TryGetInMemory<Cart>("Cart", out var cart))
            {
                cart = new Cart();
            }

            return View(cart);
        }

        [HttpPost("{offerId}")]
        public IActionResult AddOrder(int offerId, short quantity)
        {
            if (!HttpContext.Session.TryGetInMemory<Cart>("Cart", out var cart))
            {
                cart = new Cart();
            }

            var offer = _productRepository.GetProductById(offerId);
            var orderDetail = new OrderDetail()
            {
                Discount = 0.0F,
                ProductId = offerId,
                Quantity = quantity,
                Product = offer,
                UnitPrice = offer.UnitPrice
            };
            cart.OrderDetails.Add(orderDetail);

            HttpContext.Session.SetInMemory("Cart", cart);

            return RedirectToAction("Index");
        }

        [HttpGet("{offerId}")]
        public IActionResult RemoveOrder(int offerId)
        {
            try
            {
                if (HttpContext.Session.TryGetInMemory<Cart>("Cart", out var cart))
                {
                    var orderDetail = cart.OrderDetails.First(od => od.ProductId == offerId);
                    if (orderDetail == null)
                    {
                        return View("RemoveOrderError", string.Format("Offer {0} was not found in your cart.", offerId));
                    }

                    cart.OrderDetails.Remove(orderDetail);
                    HttpContext.Session.SetInMemory("Cart", cart);

                    Response.Redirect("~/ViewCart.aspx");
                }
            }
            catch (Exception ex)
            {
                return View("RemoveOrderError", string.Format("An error has occurred: {0}", ex.Message));
            }

            return RedirectToAction("Index");
        }
    }
}