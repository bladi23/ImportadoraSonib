namespace ImportadoraSonib.DTOs;

public record CartItemReq(int ProductId, int Quantity);
public record CreateOrderReq(List<CartItemReq> Items);
public record CreateOrderRes(int OrderId, decimal Total, string WhatsappUrl);
