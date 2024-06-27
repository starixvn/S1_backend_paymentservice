namespace PaymentCore.Models
{
    public enum SiteEnum
    {
        Nova88 = 1,
        Goal123 = 2,
        Bwing = 3
    }

    public enum CurrencyEnum
    {
        MYR = 2,
        USD = 3,
        THB = 4,
        CNY = 13,
        IDR = 15,
        VND = 51,
        INR = 61,
        BDT = 74,
        USDT = 96
    }
    
    public enum DeviceTypeEnum
    {
        Old = 0,
        Desktop = 1,
        H5 = 2,
        App = 3
    }

    public enum WithdrawalType
    {
        Undefined = 0,
        OnlineBanking = 1,
        LocalBank = 2,
        EWalletPayment = 3,
        CryptoPayment = 4,
        CryptoWallet = 5,
        Settlement = 6
    }
    
    public enum DepositType
    {
        OnlineBanking = 1,
        MobilePayment = 2,
        LocalBank = 3,
        CryptoPayment = 4,
        LoadCardPayment = 5,
        QrPayPayment = 6,
        BankAutoChannel = 7,
        CryptoWallet = 8,
        QuickTransfer = 9,
        ChatTransfer = 10
    }
}