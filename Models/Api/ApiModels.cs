namespace SuperMarket.Models.Api
{
    // ── المنتج اللي بيتعرض على الموقع ──
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Unit { get; set; } = string.Empty;
        public bool InStock { get; set; }
    }

    // ── عنصر في الطلب اللي بيبعته العميل ──
    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    // ── الطلب الكامل من الموقع ──
    public class PlaceOrderRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    // ── الرد اللي بيرجع للموقع بعد الطلب ──
    public class PlaceOrderResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OrderNumber { get; set; }
        public decimal? Total { get; set; }
    }

    // ── رد عام ──
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
    }
}
