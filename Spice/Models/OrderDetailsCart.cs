using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Models
{
    public class OrderDetailsCart
    {

        public  List<ShoppingCart> CartList { set; get; }
        public OrderHeader OrderHeader{ get; set; }

    }
}
