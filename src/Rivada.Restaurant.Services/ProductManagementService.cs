using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Rivada.Restaurant.Data.Repositories;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;
using Rivada.Restaurant.Services.Models;

namespace Rivada.Restaurant.Services
{
    public class ProductManagementService : IProductManagementService
    {
        private readonly ProductRepository _productRepository;
        private readonly CategoryRepository _categoryRepository;

        public ProductManagementService(ProductRepository productRepository, CategoryRepository categoryRepository)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        }

        public List<Category> GetCategories()
        {
            return _categoryRepository.GetAllCategories();
        }

        public List<ProductListItem> GetProducts()
        {
            var categories = _categoryRepository.GetAllCategories(false);
            var categoryMap = categories.ToDictionary(x => x.Id, x => x.Name);

            var products = _productRepository.GetAllProducts();
            return products
                .OrderBy(x => x.Name)
                .Select(x => new ProductListItem
                {
                    Id = x.Id,
                    ProductName = x.Name,
                    CategoryId = x.CategoryId,
                    CategoryName = categoryMap.TryGetValue(x.CategoryId, out var categoryName) ? categoryName : "Bilinmiyor",
                    Price = x.Price,
                    IsActive = x.IsActive
                })
                .ToList();
        }

        public void AddProduct(string name, int categoryId, decimal price, bool isActive)
        {
            ValidateInput(name, categoryId, price);

            _productRepository.AddProduct(new Product
            {
                Name = name.Trim(),
                CategoryId = categoryId,
                Price = price,
                IsActive = isActive
            });
        }

        public void UpdateProduct(int productId, string name, int categoryId, decimal price, bool isActive)
        {
            if (productId <= 0)
            {
                throw new InvalidOperationException("Guncellemek icin gecerli bir urun secin.");
            }

            ValidateInput(name, categoryId, price);

            var updated = _productRepository.UpdateProduct(new Product
            {
                Id = productId,
                Name = name.Trim(),
                CategoryId = categoryId,
                Price = price,
                IsActive = isActive
            });

            if (!updated)
            {
                throw new InvalidOperationException("Urun guncellenemedi.");
            }
        }

        public void MarkProductInactive(int productId)
        {
            if (productId <= 0)
            {
                throw new InvalidOperationException("Once gecerli bir urun secin.");
            }

            var updated = _productRepository.SetProductInactive(productId);
            if (!updated)
            {
                throw new InvalidOperationException("Urun pasif olarak isaretlenemedi.");
            }
        }

        public void DeleteProduct(int productId)
        {
            if (productId <= 0)
            {
                throw new InvalidOperationException("Once gecerli bir urun secin.");
            }

            try
            {
                var deleted = _productRepository.DeleteProduct(productId);
                if (!deleted)
                {
                    throw new InvalidOperationException("Urun silinemedi.");
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("Urun hesap gecmisinde kullaniliyor. Silmek yerine pasif yapin.", ex);
            }
        }

        private static void ValidateInput(string name, int categoryId, decimal price)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Urun adi zorunludur.");
            }

            if (categoryId <= 0)
            {
                throw new InvalidOperationException("Kategori zorunludur.");
            }

            if (price <= 0)
            {
                throw new InvalidOperationException("Fiyat sifirdan buyuk olmalidir.");
            }
        }
    }
}
