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
        private readonly BillRepository _billRepository;

        public OrderService(
            CategoryRepository categoryRepository,
            ProductRepository productRepository,
            BillItemRepository billItemRepository,
            BillRepository billRepository)
        {
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _billItemRepository = billItemRepository ?? throw new ArgumentNullException(nameof(billItemRepository));
            _billRepository = billRepository ?? throw new ArgumentNullException(nameof(billRepository));
        }

        public List<Category> GetCategories()
        {
            return _categoryRepository.GetAllCategories();
        }

        public List<Product> GetProductsByCategory(int categoryId)
        {
            if (categoryId <= 0)
            {
                throw new InvalidOperationException("Geçerli bir kategori bulunamadı.");
            }

            return _productRepository.GetProductsByCategory(categoryId);
        }

        public BillDetails GetBillDetails(int billId)
        {
            if (billId <= 0)
            {
                throw new InvalidOperationException("Gecersiz hesap bilgisi.");
            }

            EnsureOpenBillOrThrow(billId);
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
            if (billId <= 0)
            {
                throw new InvalidOperationException("Gecersiz hesap bilgisi.");
            }

            if (productId <= 0)
            {
                throw new InvalidOperationException("Gecersiz urun secimi.");
            }

            EnsureOpenBillOrThrow(billId);
            var product = _productRepository.GetProductById(productId);
            if (product == null || !product.IsActive)
            {
                throw new InvalidOperationException("Urun bulunamadi veya pasif.");
            }

            var normalizedNote = NormalizeNote(note);
            var existingItem = _billItemRepository.GetBillItemByProductAndNote(billId, productId, normalizedNote);

            if (existingItem != null)
            {
                if (!_billItemRepository.UpdateBillItemQuantity(existingItem.Id, existingItem.Quantity + 1))
                {
                    throw new InvalidOperationException("Mevcut hesap kalemi adedi guncellenemedi.");
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
            if (billItemId <= 0)
            {
                throw new InvalidOperationException("Gecersiz hesap kalemi secimi.");
            }

            var item = GetBillItemOrThrow(billItemId);
            EnsureOpenBillOrThrow(item.BillId);
            if (!_billItemRepository.UpdateBillItemQuantity(billItemId, item.Quantity + 1))
            {
                throw new InvalidOperationException("Hesap kalemi adedi artirilamadi.");
            }
        }

        public void DecreaseBillItemQuantity(int billItemId)
        {
            if (billItemId <= 0)
            {
                throw new InvalidOperationException("Gecersiz hesap kalemi secimi.");
            }

            var item = GetBillItemOrThrow(billItemId);
            EnsureOpenBillOrThrow(item.BillId);

            if (item.Quantity <= 1)
            {
                if (!_billItemRepository.RemoveBillItem(billItemId))
                {
                    throw new InvalidOperationException("Hesap kalemi silinemedi.");
                }

                return;
            }

            if (!_billItemRepository.UpdateBillItemQuantity(billItemId, item.Quantity - 1))
            {
                throw new InvalidOperationException("Hesap kalemi adedi azaltilamadi.");
            }
        }

        public void RemoveBillItem(int billItemId)
        {
            if (billItemId <= 0)
            {
                throw new InvalidOperationException("Gecersiz hesap kalemi secimi.");
            }

            var item = GetBillItemOrThrow(billItemId);
            EnsureOpenBillOrThrow(item.BillId);
            if (!_billItemRepository.RemoveBillItem(billItemId))
            {
                throw new InvalidOperationException("Hesap kalemi silinemedi.");
            }
        }

        public void UpdateBillItemNote(int billItemId, string? note)
        {
            if (billItemId <= 0)
            {
                throw new InvalidOperationException("Gecersiz hesap kalemi secimi.");
            }

            var item = GetBillItemOrThrow(billItemId);
            var normalizedNote = NormalizeNote(note);

            var duplicate = _billItemRepository.GetBillItemByProductAndNote(item.BillId, item.ProductId, normalizedNote);
            if (duplicate != null && duplicate.Id != billItemId)
            {
                EnsureOpenBillOrThrow(item.BillId);
                if (!_billItemRepository.MergeBillItems(billItemId, duplicate.Id))
                {
                    throw new InvalidOperationException("Ayni hesap kalemleri birlestirilemedi.");
                }

                return;
            }

            EnsureOpenBillOrThrow(item.BillId);
            if (!_billItemRepository.UpdateBillItemNote(billItemId, normalizedNote))
            {
                throw new InvalidOperationException("Hesap kalemi notu guncellenemedi.");
            }
        }

        private BillItem GetBillItemOrThrow(int billItemId)
        {
            var item = _billItemRepository.GetBillItemById(billItemId);
            if (item == null)
            {
                throw new InvalidOperationException("Seçili sipariş satırı bulunamadı.");
            }

            return item;
        }

        private void EnsureOpenBillOrThrow(int billId)
        {
            var bill = _billRepository.GetBillById(billId);
            if (bill == null || bill.Status != BillStatus.Open)
            {
                throw new InvalidOperationException("Aktif adisyon bulunamadı.");
            }
        }

        private static string? NormalizeNote(string? note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return null;
            }

            var trimmed = note.Trim();
            if (trimmed.Length > 250)
            {
                throw new InvalidOperationException("Not en fazla 250 karakter olabilir.");
            }

            return trimmed;
        }
    }
}

