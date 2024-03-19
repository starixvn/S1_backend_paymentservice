namespace PaymentService.Model
{
    public enum DataBase
    {
        Main,
    }

    public enum RedisDataBase
    {
        Member = 0,
        Shared = 1,
        Tp = 2,
        Bonus = 3,
        Admin = 4,
        Payment = 5
    }
}