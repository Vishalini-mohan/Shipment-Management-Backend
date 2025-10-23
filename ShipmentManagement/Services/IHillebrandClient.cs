using System.Text.Json;

namespace ShipmentManagement.Services
{
    public interface IHillebrandClient
    {
        Task<JsonElement> GetShipmentsAsync(string? mainModality, int pageNumber, int pageSize);
        Task<JsonElement> GetShipmentByIdAsync(string id);
    }
}
