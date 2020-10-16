using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace PaymentCore
{
    public interface IPayHttpHelper
    {
        HttpClient GetClient();
    }
}
