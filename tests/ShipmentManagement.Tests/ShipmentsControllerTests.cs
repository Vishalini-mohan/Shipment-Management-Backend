using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using ShipmentManagement.Controllers;
using ShipmentManagement.Services;
using Xunit;

namespace ShipmentManagement.Tests
{
    
    class FakeHillebrandClient : IHillebrandClient
    {
        public Func<string?, int, int, Task<JsonElement>>? GetShipmentsImpl { get; set; }
        public Func<string, Task<JsonElement>>? GetShipmentByIdImpl { get; set; }

        public Task<JsonElement> GetShipmentsAsync(string? mainModality, int pageNumber, int pageSize)
        {
            if (GetShipmentsImpl is null) throw new NotImplementedException();
            return GetShipmentsImpl(mainModality, pageNumber, pageSize);
        }

        public Task<JsonElement> GetShipmentByIdAsync(string id)
        {
            if (GetShipmentByIdImpl is null) throw new NotImplementedException();
            return GetShipmentByIdImpl(id);
        }
    }

    public class ShipmentsControllerTests
    {
        private static JsonElement ToJsonElement(object obj)
        {
            var json = JsonSerializer.Serialize(obj);
            return JsonDocument.Parse(json).RootElement;
        }

        [Fact]
        public async Task Get_ReturnsContent_WithShipmentsJson()
        {
            // Arrange
            var fake = new FakeHillebrandClient
            {
                GetShipmentsImpl = (_, pageNumber, pageSize) =>
                    Task.FromResult(ToJsonElement(new { data = new[] { new { id = "S1" } }, total = 1 }))
            };

            var controller = new ShipmentsController(NullLogger<ShipmentsController>.Instance, fake);

            // Act
            var result = await controller.Get(null, 1, 10);

            // Assert
            var content = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", content.ContentType);
            Assert.Contains("\"id\":\"S1\"", content.Content);
        }

        [Fact]
        public async Task Get_InvalidPagination_ReturnsBadRequest()
        {
            var fake = new FakeHillebrandClient();
            var controller = new ShipmentsController(NullLogger<ShipmentsController>.Instance, fake);

            var result = await controller.Get(null, 0, 10);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var value = bad.Value;
            // verify message property on anonymous object
            var msg = value?.GetType().GetProperty("message")?.GetValue(value) as string;
            Assert.Equal("Invalid pagination parameters", msg);
        }

        [Fact]
        public async Task Get_WhenClientThrows_Returns502()
        {
            var fake = new FakeHillebrandClient
            {
                GetShipmentsImpl = (_, __, ___) => throw new InvalidOperationException("upstream")
            };
            var controller = new ShipmentsController(NullLogger<ShipmentsController>.Instance, fake);

            var result = await controller.Get(null, 1, 10);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(502, obj.StatusCode);

            var value = obj.Value;
            var msg = value?.GetType().GetProperty("message")?.GetValue(value) as string;
            Assert.Equal("Upstream service error", msg);
        }

        [Fact]
        public async Task GetById_Valid_ReturnsContent()
        {
            var fake = new FakeHillebrandClient
            {
                GetShipmentByIdImpl = id => Task.FromResult(ToJsonElement(new { id = id, mainModality = "SEA" }))
            };
            var controller = new ShipmentsController(NullLogger<ShipmentsController>.Instance, fake);

            var result = await controller.GetById("1");

            var content = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", content.ContentType);
            Assert.Contains("\"id\":\"1\"", content.Content);
        }

        [Fact]
        public async Task GetById_InvalidId_ReturnsBadRequest()
        {
            var fake = new FakeHillebrandClient();
            var controller = new ShipmentsController(NullLogger<ShipmentsController>.Instance, fake);

            var result = await controller.GetById("");

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var value = bad.Value;
            var msg = value?.GetType().GetProperty("message")?.GetValue(value) as string;
            Assert.Equal("Invalid id", msg);
        }

        [Fact]
        public async Task GetById_WhenClientThrows_Returns502()
        {
            var fake = new FakeHillebrandClient
            {
                GetShipmentByIdImpl = id => throw new InvalidOperationException("upstream")
            };
            var controller = new ShipmentsController(NullLogger<ShipmentsController>.Instance, fake);

            var result = await controller.GetById("1");

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(502, obj.StatusCode);

            var value = obj.Value;
            var msg = value?.GetType().GetProperty("message")?.GetValue(value) as string;
            Assert.Equal("Upstream service error", msg);
        }
    }
}