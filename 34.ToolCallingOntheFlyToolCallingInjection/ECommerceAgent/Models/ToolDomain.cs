using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceAgent.Models;

[Flags]
public enum ToolDomain
{
    None = 0,
    Order = 1 << 0,   // "order", "item", "purchase", "bought"
    Return = 1 << 1,   // "return", "refund", "exchange", "cancel"
    Payment = 1 << 2,   // "charged", "invoice", "billing", "payment"
    Shipping = 1 << 3    // "delivery", "ship", "track", "arrive"
}