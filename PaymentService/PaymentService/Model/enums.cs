namespace PaymentService.Model
{
    public enum DataBase
    {
        Main, //ConnectionString
        Report,//ConnectionStringReport
        Wager,//ConnectionStringWager
        Log,//ConnectionStringLog
    }

    public enum CurrencyId
    {
        MYR = 2,
        THB = 4,
        IDR = 15,
        VND = 51,
        INR = 61,
        USDT = 96
    }
}