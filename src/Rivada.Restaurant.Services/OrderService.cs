using System;
using System.Collections.Generic;
using System.Linq;
using Rivada.Restaurant.Data.Repositories;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.Services.Models;

namespace Rivada.Restaurant.Services
{
    public class OrderService : IOrderService
    {
        private readonly CategoryRepository _categoryRepository;
        private readonly ProductRepository _productRepository;
        private readonly BillItemRepository _billItemRepository;

        public OrderService(
            CategoryRepository categoryRepository,
            ProductRepository productRepository,
            BillItemRepository billItemRepository)
        {
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _billItemRepository = billItemRepository ?? throw new ArgumentNullException(nameof(billItemRepository));
        }

        public List<Category> GetCategories()
        {
            return _categoryRepository.GetAllCategories();
        }

        public List<Product> GetProductsByCategory(int categoryId)
        {
            return _productRepository.GetProductsByCategory(categoryId);
        }

        public BillDetails GetBillDetails(int billId)
        {
            var items = _billItemRepository.GetBillItemsByBillId(billId);
            var subtotal = items.Sum(x => x.LineTotal);

            return new BillDetails
            {
                Items = items,
                Subtotal = subtotal,
                Total = subtotal
            };
        }

        public void AddProductToBill(int billId, int productId, string? note = null)
        {
            var product = _productRepository.GetProductById(productId);
            if (product == null || !product.IsActive)
            {
                throw new InvalidOperationException("Product not found or inactive.");
            }

            var normalizedNote = NormalizeNote(note);
            var existingItem = _billItemRepository.GetBillItemByProductAndNote(billId, productId, normalizedNote);

            if (existingItem != null)
            {
                if (!_billItemRepository.UpdateBillItemQuantity(existingItem.Id, existingItem.Quantity + 1))
                {
                    throw new InvalidOperationException("Could not update existing bill item quantity.");
                }

                return;
            }

            _billItemRepository.AddBillItem(new BillItem
            {
                BillId = billId,
                ProductId = productId,
                ProductNameSnapshot = product.Name,
                Quantity = 1,
                UnitPrice = product.Price,
                LineTotal = product.Price,
                Note = normalizedNote
            });
        }

        public void IncreaseBillItemQuantity(int billItemId)
        {
            var item = GetBillItemOrThrow(billItemId);
            if (!_billItemRepository.UpdateBillItemQuantity(billItemId, item.Quantity + 1))
            {
                throw new InvalidOperationException("Could not increase bill item quantity.");
            }
        }

        public void DecreaseBillItemQuantity(int billItemId)
        {
            var item = GetBillItemOrThrow(billItemId);

            if (item.Quantity <= 1)
            {
                if (!_billItemRepository.RemoveBillItem(billItemId))
                {
                    throw new InvalidOperationException("Could not remove bill item.");
                }

                return;
            }

            if (!_billItemRepository.UpdateBillItemQuantity(billItemId, item.Quantity - 1))
            {
                throw new InvalidOperationException("Could not decrease bill item quantity.");
            }
        }

        public void RemoveBillItem(int billItemId)
        {
            if (!_billItemRepository.RemoveBillItem(billItemId))
            {
                throw new InvalidOperationException("Could not remove bill item.");
            }
        }

        public void UpdateBillItemNote(int billItemId, string? note)
        {
            var item = GetBillItemOrThrow(billItemId);
            var normalizedNote = NormalizeNote(note);

            var duplicate = _billItemRepository.GetBillItemByProductAndNote(item.BillId, item.ProductId, normalizedNote);
            if (duplicate != null && duplicate.Id != billItemId)
            {
                if (!_billItemRepository.UpdateBillItemQuantity(duplicate.Id, duplicate.Quantity + item.Quantity))
                {
                    throw new InvalidOperationException("Could not merge duplicate bill item.");
                }

                if (!_billItemRepository.RemoveBillItem(billItemId))
                {
                    throw new InvalidOperationException("Could not remove old bill item after merge.");
                }

                return;
            }

            if (!_billItemRepository.UpdateBillItemNote(billItemId, normalizedNote))
            {
                throw new InvalidOperationException("Could not update bill item note.");
            }
        }

        private BillItem GetBillItemOrThrow(int billItemId)
        {
            var item = _billItemRepository.GetBillItemById(billItemId);
            if (item == null)
            {
                throw new InvalidOperationException("Bill item was not found.");
            }

            return item;
        }

        private static string? NormalizeNote(string? note)
        {
            return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }
    }
}
