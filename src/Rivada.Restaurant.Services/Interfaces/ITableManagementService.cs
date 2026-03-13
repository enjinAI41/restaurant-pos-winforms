using System.Collections.Generic;
using Rivada.Restaurant.Services.Models;

namespace Rivada.Restaurant.Services.Interfaces
{
    public interface ITableManagementService
    {
        List<TableDisplayItem> GetTableDisplayItems();
        int GetOrCreateOpenBillId(int tableId, int userId);
    }
}
