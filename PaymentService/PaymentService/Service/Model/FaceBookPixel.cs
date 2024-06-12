namespace PaymentService.Service.Model;

public class PixelRequest
{
    public int CustId { get; set; }
    public string Url { get; set; }
    public int? CurrencyId { get; set; }
    public List<PixelData> Data { get; set; }
}

public class PixelData
{
    public FaceBookPixel PixelEvent { get; set; }
    public decimal? Value { get; set; }
}

public enum FaceBookPixel
{
    registerSubmit = 1,
    register = 2,
    depositSubmit = 3,
    firstDeposit = 4,
    withdraw = 5,
    firstDepositArrival = 6,
    deposit = 7,
    redeposit = 8
}