namespace PaymentService.Service.Model;

public class PixelRequest
{
    public int CustId { get; set; }
    public string Url { get; set; }
    public int? CurrencyId { get; set; }
    public decimal? Value { get; set; }
    public IEnumerable<FaceBookPixel> PixelEvent { get; set; }
}

public enum FaceBookPixel
{
    registerSubmit = 1,
    CompleteRegistration = 2,
    depositSubmit = 3,
    firstDeposit = 4,
    withdraw = 5,
    firstDepositArrival = 6,
    Purchase = 7,
    redeposit = 8
}