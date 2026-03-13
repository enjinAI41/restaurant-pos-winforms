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
                    CategoryName = categoryMap.TryGetValue(x.CategoryId, out var categoryName) ? categoryName : "Unknown",
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
                throw new InvalidOperationException("Select a valid product to update.");
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
                throw new InvalidOperationException("Product could not be updated.");
            }
        }

        public void MarkProductInactive(int productId)
        {
            if (productId <= 0)
            {
                throw new InvalidOperationException("Select a valid product first.");
            }

            var updated = _productRepository.SetProductInactive(productId);
            if (!updated)
            {
                throw new InvalidOperationException("Product could not be marked inactive.");
            }
        }

        public void DeleteProduct(int productId)
        {
            if (productId <= 0)
            {
                throw new InvalidOperationException("Select a valid product first.");
            }

            try
            {
                var deleted = _productRepository.DeleteProduct(productId);
                if (!deleted)
                {
                    throw new InvalidOperationException("Product could not be deleted.");
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("Product is used in bill history. Mark it inactive instead of deleting.", ex);
            }
        }

        private static void ValidateInput(string name, int categoryId, decimal price)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Product name is required.");
            }

            if (categoryId <= 0)
            {
                throw new InvalidOperationException("Category is required.");
            }

            if (price <= 0)
            {
                throw new InvalidOperationException("Price must be greater than zero.");
            }
        }
    }
}
