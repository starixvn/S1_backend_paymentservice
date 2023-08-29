using PaymentService.Model;

using StackExchange.Redis;

namespace PaymentService.Service.Library;

public interface ICacheHelper
{
    TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory, RedisDataBase redisDataBase = RedisDataBase.Payment);
    Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan? timeSpan, Func<Task<TItem>> factory, RedisDataBase redisDataBase = RedisDataBase.Payment);
    Task<bool> StringSetAsync(string key, RedisValue value, TimeSpan? timeSpan, RedisDataBase redisDataBase = RedisDataBase.Payment);
    bool StringSet(string key, RedisValue value, TimeSpan? timeSpan, RedisDataBase redisDataBase = RedisDataBase.Payment);
    bool KeyExists(string key, RedisDataBase redisDataBase = RedisDataBase.Payment);
    Task<TItem> StringGetAsync<TItem>(string key, RedisDataBase redisDataBase = RedisDataBase.Payment);
    List<TItem> GetCacheList<TItem>(IEnumerable<string> keys, RedisDataBase redisDataBase = RedisDataBase.Payment);
    bool SetCacheList<TItem>(Dictionary<string, TItem> dataList, TimeSpan? timeSpan, RedisDataBase redisDataBase = RedisDataBase.Payment);
    bool InsertList<TItem>(string key, TimeSpan? timeSpan, IEnumerable<TItem> list, RedisDataBase redisDataBase = RedisDataBase.Payment);
    List<TItem> GetListByRange<TItem>(string key, long startIndex, long endIndex, RedisDataBase redisDataBase = RedisDataBase.Payment);
    long GetListLength(string key, RedisDataBase redisDataBase = RedisDataBase.Payment);
    bool KeyDelete(string key, RedisDataBase redisDataBase = RedisDataBase.Payment);
    bool TryCreateLock(RedisKey lockKey, RedisValue lockValue, TimeSpan? expiry, RedisDataBase redisDataBase = RedisDataBase.Payment);
    bool UnLock(RedisKey lockKey, RedisValue lockValue, RedisDataBase redisDataBase = RedisDataBase.Payment);
}