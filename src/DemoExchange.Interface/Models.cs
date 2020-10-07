using System;

namespace DemoExchange.Interface {
  /// <summary>
  /// Is a model.
  /// </summary>
  public interface IModel { }

  public interface IIsValid {
    public bool IsValid { get; }
  }

  /// <summary>
  /// Model for an order.
  /// </summary>
  public interface IModelOrder : IModel, IIsValid {
    public const String ORDER_ID_NEW = "NEW";

    public const String ERROR_STRING_EMTPY = "Cannot be empty";
    public const String ERROR_QUANTITY_IS_0 = "quantity must be greater than 0";
    public const String ERROR_OPEN_QUANITY_GREATER_THAN_QUANITY = "openQuantity cannot be greater than original quantity";
    public const String ERROR_ORDER_PRICE_MARKET_NOT_0 = "orderPrice should be 0 for Market orders";
    public const String ERROR_ORDER_PRICE_IS_0 = "orderPrice must be greater than 0";

    public String OrderId { get; }
    public long CreatedTimestamp { get; }
    public String AccountId { get; }
    public OrderStatus Status { get; }
    public OrderAction Action { get; }
    public String Ticker { get; }
    public OrderType Type { get; }
    public int Quantity { get; }
    public int OpenQuantity { get; }
    public decimal OrderPrice { get; }
    public decimal StrikePrice { get; }
    public OrderTimeInForce TimeInForce { get; }
    public long ToBeCanceledTimestamp { get; }
    public long CanceledTimestamp { get; }
  }

  public class BaseOrder : IModelOrder {
    public String OrderId { get; }
    public long CreatedTimestamp { get; }
    public String AccountId { get; }
    public OrderStatus Status { get; }
    public OrderAction Action { get; }
    public String Ticker { get; }
    public OrderType Type { get; }
    public int Quantity { get; }
    public int OpenQuantity { get; }
    public decimal OrderPrice { get; }
    public decimal StrikePrice { get; }
    public OrderTimeInForce TimeInForce { get; }
    public long ToBeCanceledTimestamp { get; }
    public long CanceledTimestamp { get; }

    public virtual bool IsValid {
      get { return true; }
    }

    public BaseOrder(String orderId, long createdTimestamp, String accountId, OrderStatus status,
      OrderAction action, String ticker, OrderType type, int quantity, int openQuantity,
      decimal orderPrice, decimal strikePrice, OrderTimeInForce timeInForce,
      long toBeCanceledTimestamp, long canceledTimestamp) {
      if (String.IsNullOrWhiteSpace(orderId))
        throw new ArgumentException(IModelOrder.ERROR_STRING_EMTPY, paramName : nameof(orderId));
      if (String.IsNullOrWhiteSpace(accountId))
        throw new ArgumentException(IModelOrder.ERROR_STRING_EMTPY, paramName : nameof(accountId));
      if (String.IsNullOrWhiteSpace(ticker))
        throw new ArgumentException(IModelOrder.ERROR_STRING_EMTPY, paramName : nameof(ticker));
      if (quantity < 0)
        throw new ArgumentException(IModelOrder.ERROR_QUANTITY_IS_0);
      if (openQuantity > quantity)
        throw new ArgumentException(IModelOrder.ERROR_OPEN_QUANITY_GREATER_THAN_QUANITY);
      if (OrderType.MARKET.Equals(type)) {
        if (orderPrice != 0) {
          throw new ArgumentException(IModelOrder.ERROR_ORDER_PRICE_MARKET_NOT_0);
        }
      } else {
        if (orderPrice <= 0) {
          throw new ArgumentException(IModelOrder.ERROR_ORDER_PRICE_IS_0);
        }
      }

      OrderId = orderId;
      CreatedTimestamp = createdTimestamp;
      AccountId = accountId;
      Status = status;
      Action = action;
      Ticker = ticker;
      Type = type;
      Quantity = quantity;
      OpenQuantity = openQuantity;
      OrderPrice = orderPrice;
      StrikePrice = strikePrice;
      TimeInForce = timeInForce;
      ToBeCanceledTimestamp = toBeCanceledTimestamp;
      CanceledTimestamp = canceledTimestamp;
    }
  }

  public abstract class MarketOrder : BaseOrder {
    public MarketOrder(String accountId, OrderAction action, String ticker, int quantity):
      base(IModelOrder.ORDER_ID_NEW, 0, accountId, OrderStatus.OPEN, action, ticker,
        OrderType.MARKET, quantity, quantity, 0, 0, OrderTimeInForce.DAY, 0, 0) { }
  }

  /// <summary>
  /// Convenience class to instantiate a Buy Market Order.
  /// </summary>
  public class BuyMarketOrder : MarketOrder {
    public BuyMarketOrder(String accountId, String ticker, int quantity):
      base(accountId, OrderAction.BUY, ticker, quantity) { }
  }

  /// <summary>
  /// Convenience class to instantiate a Buy Limit Day Order.
  /// </summary>
  public class BuyLimitDayOrder : BaseOrder {
    public BuyLimitDayOrder(String accountId, String ticker, int quantity, decimal orderPrice):
      base(IModelOrder.ORDER_ID_NEW, 0, accountId, OrderStatus.OPEN, OrderAction.BUY, ticker,
        OrderType.LIMIT, quantity, quantity, orderPrice, orderPrice, OrderTimeInForce.DAY, 0, 0) { }
  }

  /// <summary>
  /// Convenience class to instantiate a Buy Limit GTC Order.
  /// </summary>
  public class BuyLimitGoodTilCancelOrder : BaseOrder {
    public BuyLimitGoodTilCancelOrder(String accountId, String ticker, int quantity,
        decimal orderPrice):
      base(IModelOrder.ORDER_ID_NEW, 0, accountId, OrderStatus.OPEN, OrderAction.BUY, ticker,
        OrderType.LIMIT, quantity, quantity, orderPrice, orderPrice,
        OrderTimeInForce.GOOD_TIL_CANCELED, 0, 0) { }
  }

  /// <summary>
  /// Convenience class to instantiate a Sell Market Order.
  /// </summary>
  public class SellMarketOrder : MarketOrder {
    public SellMarketOrder(String accountId, String ticker, int quantity):
      base(accountId, OrderAction.SELL, ticker, quantity) { }
  }

  /// <summary>
  /// Convenience class to instantiate a Sell Limit Day Order.
  /// </summary>
  public class SellLimitDayOrder : BaseOrder {
    public SellLimitDayOrder(String accountId, String ticker, int quantity, decimal orderPrice):
      base(IModelOrder.ORDER_ID_NEW, 0, accountId, OrderStatus.OPEN, OrderAction.SELL, ticker,
        OrderType.LIMIT, quantity, quantity, orderPrice, orderPrice, OrderTimeInForce.DAY, 0, 0) { }
  }

  /// <summary>
  /// Convenience class to instantiate a Sell Limit GTC Order.
  /// </summary>
  public class SellLimitGoodTilCancelOrder : BaseOrder {
    public SellLimitGoodTilCancelOrder(String accountId, String ticker, int quantity,
        decimal orderPrice):
      base(IModelOrder.ORDER_ID_NEW, 0, accountId, OrderStatus.OPEN, OrderAction.SELL, ticker,
        OrderType.LIMIT, quantity, quantity, orderPrice, orderPrice,
        OrderTimeInForce.GOOD_TIL_CANCELED, 0, 0) { }
  }

  public enum OrderStatus {
    OPEN, // Default
    COMPLETED,
    UPDATED,
    CANCELLED,
    DELETED
  }

  public enum OrderAction {
    BUY, // Default
    SELL
  }

  public enum OrderType {
    MARKET, // Default
    LIMIT,
    STOP_MARKET,
    STOP_LIMIT,
    TRAILING_STOP_MARKET,
    TRAILING_STOP_LIMIT,
    FILL_OR_KILL,
    IMMEDIATE_OR_CANCEL
  }

  public enum OrderTimeInForce {
    DAY, // Default
    GOOD_TIL_CANCELED,
    MARKET_CLOSE
  }

  /// <summary>
  /// Model for an account.
  /// </summary>
  public interface IModelAccount : IModel, IIsValid {
    public String AccountId { get; }
  }

  public class BaseAccount : IModelAccount {
    public String AccountId { get; }

    public virtual bool IsValid {
      get { return true; }
    }

    public BaseAccount(String accountId) {
      AccountId = accountId;
    }
  }

  /// <summary>
  /// Validators for models.
  /// </summary>
  public interface IValidator<IModel> {
    public bool IsValid { get; }
  }
}
