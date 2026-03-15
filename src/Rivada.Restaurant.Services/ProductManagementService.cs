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

        public List<Category> GetCategories(bool onlyActive = true)
        {
            return _categoryRepository.GetAllCategories(onlyActive);
        }

        public void AddCategory(string name, bool isActive)
        {
            ValidateCategoryInput(name);
            try
            {
                _categoryRepository.AddCategory(new Category
                {
                    Name = name.Trim(),
                    IsActive = isActive
                });
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("Aynı isimde kategori zaten var.", ex);
            }
        }

        public void UpdateCategory(int categoryId, string name, bool isActive)
        {
            if (categoryId <= 0)
            {
                throw new InvalidOperationException("Güncellenecek kategori seçilmedi.");
            }

            ValidateCategoryInput(name);
            EnsureCategoryExists(categoryId);

            try
            {
                var updated = _categoryRepository.UpdateCategory(new Category
                {
                    Id = categoryId,
                    Name = name.Trim(),
                    IsActive = isActive
                });

                if (!updated)
                {
                    throw new InvalidOperationException("Kategori güncellenemedi.");
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("Aynı isimde kategori zaten var.", ex);
            }
        }

        public void SetCategoryActive(int categoryId, bool isActive)
        {
            if (categoryId <= 0)
            {
                throw new InvalidOperationException("Güncellenecek kategori seçilmedi.");
            }

            EnsureCategoryExists(categoryId);
            if (!isActive && _productRepository.CountActiveProductsByCategory(categoryId) > 0)
            {
                throw new InvalidOperationException("Bu kategoride aktif ürün var. Önce ürünleri pasif yapın veya taşıyın.");
            }

            var updated = _categoryRepository.SetCategoryActive(categoryId, isActive);
            if (!updated)
            {
                throw new InvalidOperationException("Kategori durumu güncellenemedi.");
            }
        }

        public List<ProductListItem> GetProducts()
        {
            var categories = _categoryRepository.GetAllCategories(false);
            var categoryMap = categories.ToDictionary(x => x.Id, x => x.Name);

            var products = _productRepository.GetAllProducts();
            return products
                .OrderBy(x => categoryMap.TryGetValue(x.CategoryId, out var categoryName) ? categoryName : "Bilinmiyor")
                .ThenBy(x => x.Name)
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
            EnsureCategoryExists(categoryId);

            try
            {
                _productRepository.AddProduct(new Product
                {
                    Name = name.Trim(),
                    CategoryId = categoryId,
                    Price = price,
                    IsActive = isActive
                });
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("Aynı kategori altında aynı isimde ürün zaten var.", ex);
            }
        }

        public void UpdateProduct(int productId, string name, int categoryId, decimal price, bool isActive)
        {
            if (productId <= 0)
            {
                throw new InvalidOperationException("Güncellenecek ürün seçilmedi.");
            }

            ValidateInput(name, categoryId, price);
            EnsureCategoryExists(categoryId);
            EnsureProductExists(productId);

            try
            {
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
                    throw new InvalidOperationException("Ürün güncellenemedi.");
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("Aynı kategori altında aynı isimde ürün zaten var.", ex);
            }
        }

        public void MarkProductInactive(int productId)
        {
            if (productId <= 0)
            {
                throw new InvalidOperationException("Güncellenecek ürün seçilmedi.");
            }

            EnsureProductExists(productId);

            var updated = _productRepository.SetProductInactive(productId);
            if (!updated)
            {
                throw new InvalidOperationException("Ürün pasif duruma alınamadı.");
            }
        }

        public void DeleteProduct(int productId)
        {
            if (productId <= 0)
            {
                throw new InvalidOperationException("Güncellenecek ürün seçilmedi.");
            }

            EnsureProductExists(productId);
            try
            {
                var deleted = _productRepository.DeleteProduct(productId);
                if (!deleted)
                {
                    throw new InvalidOperationException("Ürün silinemedi.");
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("Ürün geçmiş siparişlerde kullanıldığı için silinemez. Pasife almayı deneyin.", ex);
            }
        }

        private static void ValidateInput(string name, int categoryId, decimal price)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Ürün adı boş bırakılamaz.");
            }

            if (categoryId <= 0)
            {
                throw new InvalidOperationException("Lütfen bir kategori seçin.");
            }

            if (price <= 0)
            {
                throw new InvalidOperationException("Fiyat sıfırdan büyük olmalıdır.");
            }
        }

        private static void ValidateCategoryInput(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Kategori adı boş bırakılamaz.");
            }
        }

        private void EnsureCategoryExists(int categoryId)
        {
            var category = _categoryRepository.GetCategoryById(categoryId);
            if (category == null)
            {
                throw new InvalidOperationException("Lütfen bir kategori seçin.");
            }
        }

        private void EnsureProductExists(int productId)
        {
            var product = _productRepository.GetProductById(productId);
            if (product == null)
            {
                throw new InvalidOperationException("Güncellenecek ürün seçilmedi.");
            }
        }
    }
}
