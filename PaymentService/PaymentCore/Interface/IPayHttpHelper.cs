namespace PaymentCore.Interface;

public interface IPayHttpHelper
{
    HttpClient GetClient();
    
    HttpClient GetClientWithProxy();
}