using System.Text;
using Microsoft.Extensions.Configuration;

namespace ImportadoraSonib.Services;

public class WhatsappLinkService
{
    private readonly string _phone;

    public WhatsappLinkService(IConfiguration cfg)
    {
        _phone = cfg["Business:WhatsAppNumber"] ?? "";
    }

    public string BuildOrderLink(
        int orderId,
        IEnumerable<(string Name, int Quantity, decimal UnitPrice)> lines,
        decimal total)
    {
        var sb = new StringBuilder();
        sb.Append($"Hola, quiero finalizar la compra del pedido #{orderId}%0A");
        foreach (var l in lines)
            sb.Append($"- {l.Name} x{l.Quantity} = {(l.UnitPrice * l.Quantity):0.00}%0A");
        sb.Append($"Total: {total:0.00}");
        return $"https://wa.me/{_phone}?text={sb}";
    }
}
