using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DemoExchange.Api;
using DemoExchange.Interface;
using Google.Protobuf;
using Grpc.Core;
using Serilog;
using StackExchange.Redis;
using static Utils.Time;

namespace DemoExchange.QuoteService {
  public class QuoteServiceGrpc : DemoExchange.Api.QuoteService.QuoteServiceBase {
    private static Serilog.ILogger Logger => Serilog.Log.ForContext<QuoteServiceGrpc>();

    private readonly IDatabase redis;
    private readonly IOrderServiceRpcClient orderService;

    public QuoteServiceGrpc(IDatabase redis, IOrderServiceRpcClient orderService) {
      this.redis = redis;
      this.orderService = orderService;
    }

    public override Task<Level2> GetLevel2(StringMessage request, ServerCallContext context) {
      Logger.Here().Information("BGN");
      try {
        CacheData data = Cache.Get(redis, request.Value);
        if (data == null) {
          data = GetAndSetLevel2(request);
        }

        Logger.Here().Information("END");
        return Task.FromResult(data.Level2);
      } catch (Exception e) {
        Logger.Here().Warning(e.Message);
        throw new RpcException(new Status(StatusCode.Internal, e.Message));
      }
    }

    public override async Task GetLevel2Streams(StringMessage request,
      IServerStreamWriter<Level2> responseStream, ServerCallContext context) {
      Logger.Here().Information("BGN");
      try {
        Random rnd = new Random();
        int basePrice = rnd.Next(1, 11);
        for (int i = 0; i < 50; i++) {
          Level2 lvl2 = new Level2();
          lvl2.Bids.Add(new Level2Quote{
            Price = basePrice - rnd.NextDouble(),
            Quantity = rnd.Next(100, 300)
          });
          lvl2.Asks.Add(new Level2Quote{
            Price = basePrice + rnd.NextDouble(),
            Quantity = rnd.Next(100, 300)
          });
          Logger.Here().Information("Waiting 2 seconds");
          await Task.Delay(2 * 1000);
          Logger.Here().Information(lvl2.ToString());
          await responseStream.WriteAsync(lvl2);
        }
      } catch (Exception e) {
        Logger.Here().Warning(e.Message);
        throw new RpcException(new Status(StatusCode.Internal, e.Message));
      }
    }

    public override Task<Quote> GetQuote(StringMessage request, ServerCallContext context) {
      Logger.Here().Information("BGN");
      try {
        CacheData data = Cache.Get(redis, request.Value);
        if (data == null) {
          data = GetAndSetLevel2(request);
        }

        Logger.Here().Information("END");
        return Task.FromResult(data.Quote);
      } catch (Exception e) {
        Logger.Here().Warning(e.Message);
        throw new RpcException(new Status(StatusCode.Internal, e.Message));
      }
    }

    private CacheData GetAndSetLevel2(StringMessage request) {
      Logger.Here().Information("BGN");
      Task<Level2> level2Response = orderService.GetLevel2Async(request).ResponseAsync;
      Level2 level2 = level2Response.Result;
      Quote quote = new Quote() {
        Bid = level2.Bids[0].Price,
        Ask = level2.Asks[0].Price,
        Last = 0,
        Volume = 0
      };
      CacheData data = new CacheData(Now, quote, level2);
      Cache.Set(redis, request.Value, data);

      Logger.Here().Information("END");
      return data;
    }
  }

  public class CacheData {
    public long Timestamp { get; set; }
    public Quote Quote { get; set; }
    public Level2 Level2 { get; set; }

    public CacheData(long timestamp, Quote quote, Level2 level2) {
      Timestamp = timestamp;
      Quote = quote;
      Level2 = level2;
    }
  }

  public class Cache {
    private const int POS_TIMESTAMP = 0;
    private const int POS_QUOTE = 1;
    private const int POS_LEVEL2 = 2;

    public static CacheData Get(IDatabase redis, String ticker) {
      var data = redis.StringGet(KeySet(ticker));
      RedisValue timestamp = data[POS_TIMESTAMP];
      if (!timestamp.HasValue) {
        return null;
      }
      byte[] quoteBytes = (RedisValue)data[POS_QUOTE];
      byte[] level2Bytes = (RedisValue)data[POS_LEVEL2];

      return new CacheData(Int64.Parse(timestamp),
        Quote.Parser.ParseFrom(quoteBytes),
        Level2.Parser.ParseFrom(level2Bytes));
    }

    public static void Set(IDatabase redis, String ticker, CacheData data) {
      RedisKey[] keys = KeySet(ticker);
      List<KeyValuePair<RedisKey, RedisValue>> batch =
        new List<KeyValuePair<RedisKey, RedisValue>>(3) {
          new KeyValuePair<RedisKey, RedisValue>(
          keys[POS_TIMESTAMP], data.Timestamp),
          new KeyValuePair<RedisKey, RedisValue>(
          keys[POS_QUOTE], data.Quote.ToByteArray()),
          new KeyValuePair<RedisKey, RedisValue>(
          keys[POS_LEVEL2], data.Level2.ToByteArray())
        };

      redis.StringSet(batch.ToArray());
    }

    public static RedisKey[] KeySet(String ticker) {
      return new RedisKey[] {
        Constants.Redis.QUOTE_TIMESTAMP + ticker,
          Constants.Redis.QUOTE + ticker,
          Constants.Redis.QUOTE_LEVEL2 + ticker
      };
    }

    private Cache() {
      // Prevent instantiation
    }
  }

  public class Handlers {
    private const int EXPIRATION_MINUTES = 10;

    public static Action<RedisChannel, RedisValue> TransactionProcessHandler(ILogger logger,
      IDatabase redis) {
      return (channel, message) => {
        logger.Here().Information("BGN");
        byte[] bytes = message;
        TransactionProcessed transaction = TransactionProcessed.Parser.ParseFrom(bytes);
        RedisValue transactionId = redis.StringGet(transaction.TransactionId);
        if (transactionId.HasValue) {
          return;
        }

        redis.StringSet(transaction.TransactionId, "1", TimeSpan.FromMinutes(EXPIRATION_MINUTES));
        CacheData data = Cache.Get(redis, transaction.Ticker);
        Quote quote = new Quote();
        bool isNewer = (data == null || transaction.CreatedTimestamp > data.Timestamp);
        quote.Bid = isNewer ? transaction.Level2.Bids[0].Price : data.Quote.Bid;
        quote.Ask = isNewer ? transaction.Level2.Asks[0].Price : data.Quote.Ask;
        quote.Last = isNewer ? transaction.Last : data.Quote.Last;
        quote.Volume = (data == null ? 0 : data.Quote.Volume) +
          transaction.Volume;
        Cache.Set(redis, transaction.Ticker,
          new CacheData(isNewer ? transaction.CreatedTimestamp : data.Timestamp,
            quote, isNewer ? transaction.Level2 : data.Level2));
        logger.Here().Information("END");
      };
    }

    private Handlers() {
      // Prevent instantiation
    }
  }
}
