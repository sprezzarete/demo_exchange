using System;
using System.Collections.Generic;
using DemoExchange.Interface;
using static Utils.Preconditions;

namespace DemoExchange.Models {
  /// <summary>
  /// Base Class representing an Order.
  /// </summary>
  public abstract class Order : IModelOrder {
    public const int TIME_IN_FORCE_TO_BE_CANCELLED_DAYS = 90;

    public const String ERROR_STATUS_NOT_OPEN = "Error Cancel: Status is not OPEN OrderId: {0}";

    public String OrderId { get; protected set; }
    public long CreatedTimestamp { get; protected set; }
    public DateTime CreatedDateTime {
      get { return new DateTime(CreatedTimestamp); }
    }
    public String AccountId { get; protected set; }
    public OrderStatus Status { get; set; }
    public bool IsOpen {
      get { return Status.Equals(OrderStatus.OPEN); }
    }
    public bool IsCompleted {
      get { return Status.Equals(OrderStatus.COMPLETED); }
    }
    public bool IsUpdated {
      get { return Status.Equals(OrderStatus.UPDATED); }
    }
    public bool IsCancelled {
      get { return Status.Equals(OrderStatus.CANCELLED); }
    }
    public bool IsDeleted {
      get { return Status.Equals(OrderStatus.DELETED); }
    }
    public OrderAction Action { get; protected set; }
    public bool IsBuyOrder {
      get { return Action.Equals(OrderAction.BUY); }
    }
    public bool IsSellOrder {
      get { return Action.Equals(OrderAction.SELL); }
    }
    public String Ticker { get; protected set; }
    public OrderType Type { get; protected set; }
    public bool IsMarketOrder {
      get { return Type.Equals(OrderType.MARKET); }
    }
    public bool IsLimitOrder {
      get { return Type.Equals(OrderType.LIMIT); }
    }
    public bool IsStopMarketOrder {
      get { return Type.Equals(OrderType.STOP_MARKET); }
    }
    public bool IsStopLimitOrder {
      get { return Type.Equals(OrderType.STOP_LIMIT); }
    }
    public bool IsTrailingStopMarketOrder {
      get { return Type.Equals(OrderType.TRAILING_STOP_MARKET); }
    }
    public bool IsTrailingStopLimitOrder {
      get { return Type.Equals(OrderType.TRAILING_STOP_LIMIT); }
    }
    public bool IsFillOrKillOrder {
      get { return Type.Equals(OrderType.FILL_OR_KILL); }
    }
    public bool IsImmediateOrCancelOrder {
      get { return Type.Equals(OrderType.IMMEDIATE_OR_CANCEL); }
    }
    public int Quantity { get; protected set; }
    public int OpenQuantity { get; set; }
    public bool IsFilled {
      get { return OpenQuantity == 0; }
    }
    // QUESTION: Should set be protected? What are the needs of Trailing?
    public decimal StrikePrice { get; set; }
    public OrderTimeInForce TimeInForce { get; protected set; }
    public bool IsDayOrder {
      get { return TimeInForce.Equals(OrderTimeInForce.DAY); }
    }
    public bool IsGoodTillCanceledOrder {
      get { return TimeInForce.Equals(OrderTimeInForce.GOOD_TIL_CANCELED); }
    }
    public long ToBeCanceledTimestamp { get; protected set; }
    public DateTime ToBeCanceledDateTime {
      get { return new DateTime(ToBeCanceledTimestamp); }
    }
    public long CanceledTimestamp { get; private set; }
    public DateTime CanceledDateTime {
      get { return new DateTime(CanceledTimestamp); }
    }

    protected Order() { }

    /// <summary>
    /// <c>Order</c> constructor.
    /// <br><c>Id</c>: Auto-gen GUID</br>
    /// <br><c>CreatedTimestamp</c>: Auto-gen UTC high fidelity timestamp</br>
    /// <br><c>Status</c>: Defaults to OPEN</br>
    /// <br><c>OpenQuantity</c>: Defaults to Quantity</br>
    /// <br><c>ToBeCanceledTimestamp</c>: Defaults to 0 for Market orders, end of day for Day orders, and <c>TIME_IN_FORCE_TO_BE_CANCELLED_DAYS</c> for Good Til Canceled orders.</br>
    /// </summary>
    protected Order(String accountId, OrderAction action, String ticker, OrderType type,
      int quantity, decimal strikePrice, OrderTimeInForce timeInForce) {
      CheckNotNullOrWhitespace(accountId, paramName: "AccountId");
      CheckNotNullOrWhitespace(ticker, paramName: "Ticker");
      CheckArgument(quantity > 0, message: "Quantity must be greater than 0");
      if (OrderType.MARKET.Equals(type)) {
        if (strikePrice != 0) {
          throw new ArgumentException("StrikePrice should be 0 for Market orders");
        }
      } else {
        if (strikePrice <= 0) {
          throw new ArgumentException("StrikePrice must be greater than 0");
        }
      }

      OrderId = Guid.NewGuid().ToString();
      CreatedTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
      AccountId = accountId;
      Status = OrderStatus.OPEN;
      Action = action;
      Ticker = ticker;
      Type = type;
      Quantity = quantity;
      OpenQuantity = quantity;
      StrikePrice = strikePrice;
      TimeInForce = timeInForce;
      if (OrderType.MARKET.Equals(type)) {
        ToBeCanceledTimestamp = 0;
      } else {
        // TODO: Calculate timestamp for DAY
        ToBeCanceledTimestamp = timeInForce.Equals(OrderTimeInForce.DAY) ?
          0 :
          CreatedTimestamp + (TimeSpan.TicksPerDay * TIME_IN_FORCE_TO_BE_CANCELLED_DAYS);
      }
    }

    public bool IsValid() {
      return true; // TODO: throw new NotImplementedException();
    }

    public void Cancel() {
      CheckArgument(OrderStatus.OPEN.Equals(Status),
        String.Format(ERROR_STATUS_NOT_OPEN, OrderId));

      Status = OrderStatus.CANCELLED;
      CanceledTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
    }

    public Order ShallowCopy() {
      return (Order)this.MemberwiseClone();
    }

    public override String ToString() {
      return "{OrderId: " + OrderId + ", " +
        "CreatedTimestamp: " + CreatedTimestamp + ", " +
        "AccountId: " + AccountId + ", " +
        "Status: " + Status + ", " +
        "Action: " + Action + ", " +
        "Ticker: " + Ticker + ", " +
        "Type: " + Type + ", " +
        "Quantity: " + Quantity + ", " +
        "OpenQuantity: " + OpenQuantity + ", " +
        "StrikePrice: " + AppConstants.FormatPrice(StrikePrice) + ", " +
        "TimeInForce: " + TimeInForce + ", " +
        "ToBeCanceledTimestamp: " + ToBeCanceledTimestamp + ", " +
        "}";
    }

    public override bool Equals(object obj) {
      if (obj == null || GetType() != obj.GetType()) {
        return false;
      }

      return this.ToString().Equals(obj.ToString());
    }

    public override int GetHashCode() {
      return HashCode.Combine(ToString());
    }
  }

  /// <summary>
  /// For persistence of an <c>Order</c>.
  /// <br><c>readonly</c> parameters are exposed.</br>
  /// </summary>
  // TODO: Add EntityFramework and hook up to db
  public class OrderEntity : Order {
    new public String OrderId {
      get { return base.OrderId; }
      set { base.OrderId = value; }
    }
    new public long CreatedTimestamp {
      get { return base.CreatedTimestamp; }
      set { base.CreatedTimestamp = value; }
    }
    new public String AccountId {
      get { return base.AccountId; }
      set { base.AccountId = value; }
    }
    new public OrderStatus Status {
      get { return base.Status; }
      set { base.Status = value; }
    }
    new public OrderAction Action {
      get { return base.Action; }
      set { base.Action = value; }
    }
    new public String Ticker {
      get { return base.Ticker; }
      set { base.Ticker = value; }
    }
    new public OrderType Type {
      get { return base.Type; }
      set { base.Type = value; }
    }
    new public int Quantity {
      get { return base.Quantity; }
      set { base.Quantity = value; }
    }
    new public int OpenQuantity {
      get { return base.OpenQuantity; }
      set { base.OpenQuantity = value; }
    }
    new public decimal StrikePrice {
      get { return base.StrikePrice; }
      set { base.StrikePrice = value; }
    }
    new public OrderTimeInForce TimeInForce {
      get { return base.TimeInForce; }
      set { base.TimeInForce = value; }
    }
    new public long ToBeCanceledTimestamp {
      get { return base.ToBeCanceledTimestamp; }
      set { base.ToBeCanceledTimestamp = value; }
    }

    public OrderEntity() { }
  }

  /// <summary>
  /// Convenience class to instantiate a Buy Market Order.
  /// </summary>
  public class BuyMarketOrder : Order {
    public BuyMarketOrder(String accountId, String ticker, int quantity) : base(accountId,
      OrderAction.BUY, ticker, OrderType.MARKET, quantity, 0, OrderTimeInForce.DAY) { }
  }

  /// <summary>
  /// Convenience class to instantiate a Buy Limit Day Order.
  /// </summary>
  public class BuyLimitDayOrder : Order {
    public BuyLimitDayOrder(String accountId, String ticker, int quantity, decimal strikePrice):
      base(accountId, OrderAction.BUY, ticker, OrderType.LIMIT, quantity, strikePrice,
        OrderTimeInForce.DAY) { }
  }

  /// <summary>
  /// Convenience class to instantiate a Buy Limit GTC Order.
  /// </summary>
  public class BuyLimitGoodTilCanceledOrder : Order {
    public BuyLimitGoodTilCanceledOrder(String accountId, String ticker, int quantity,
        decimal strikePrice):
      base(accountId, OrderAction.BUY, ticker, OrderType.LIMIT, quantity, strikePrice,
        OrderTimeInForce.GOOD_TIL_CANCELED) { }
  }

  /// <summary>
  /// Convenience class to instantiate a Sell Market Order.
  /// </summary>
  public class SellMarketOrder : Order {
    public SellMarketOrder(String accountId, String ticker, int quantity) : base(accountId,
      OrderAction.SELL, ticker, OrderType.MARKET, quantity, 0, OrderTimeInForce.DAY) { }
  }

  /// <summary>
  /// Convenience class to instantiate a Sell Limit Day Order.
  /// </summary>
  public class SellLimitDayOrder : Order {
    public SellLimitDayOrder(String accountId, String ticker, int quantity, decimal strikePrice):
      base(accountId, OrderAction.SELL, ticker, OrderType.LIMIT, quantity, strikePrice,
        OrderTimeInForce.DAY) { }
  }

  /// <summary>
  /// Convenience class to instantiate a Sell Limit GTC Order.
  /// </summary>
  public class SellLimitGoodTilCanceledOrder : Order {
    public SellLimitGoodTilCanceledOrder(String accountId, String ticker, int quantity,
        decimal strikePrice):
      base(accountId, OrderAction.SELL, ticker, OrderType.LIMIT, quantity, strikePrice,
        OrderTimeInForce.GOOD_TIL_CANCELED) { }
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
    GOOD_TIL_CANCELED
  }

  /// <summary>
  /// Convenience methods for <c>Order</c>.
  /// </summary>
  public class Orders {
    /// <summary>
    /// Use for SELL order books.
    /// </summary>
    public static readonly Comparer<Order> STRIKE_PRICE_ASCENDING_COMPARER =
      new StrikePriceAscendingComparer();
    /// <summary>
    /// Use for BUY order books.
    /// </summary>
    public static readonly Comparer<Order> STRIKE_PRICE_DESCENDING_COMPARER =
      new StrikePriceDescendingComparer();

    public static Predicate<Order> ById(String orderId) {
      return order => order.OrderId.Equals(orderId);
    }

    private Orders() {
      // Prevent instantiation;
    }

    /// <summary>
    /// Price-Time ascending <c>Comparer</c>.
    /// </summary>
    private class StrikePriceAscendingComparer : Comparer<Order> {
      public override int Compare(Order o1, Order o2) {
        if (o1.StrikePrice > o2.StrikePrice)
          return 1;
        if (o1.StrikePrice < o2.StrikePrice)
          return -1;
        if (o1.CreatedTimestamp > o2.CreatedTimestamp)
          return 1;
        if (o1.CreatedTimestamp < o2.CreatedTimestamp)
          return -1;

        return 0;
      }
    }

    /// <summary>
    /// Price-Time descending <c>Comparer</c>.
    /// </summary>
    private class StrikePriceDescendingComparer : Comparer<Order> {
      public override int Compare(Order o1, Order o2) {
        if (o1.StrikePrice < o2.StrikePrice)
          return 1;
        if (o1.StrikePrice > o2.StrikePrice)
          return -1;
        if (o1.CreatedTimestamp > o2.CreatedTimestamp)
          return 1;
        if (o1.CreatedTimestamp < o2.CreatedTimestamp)
          return -1;

        return 0;
      }
    }
  }

  /// <summary>
  /// Market <c>Order</c> validator.
  /// </summary>
  public class MarketOrderValidator : IValidator<Order> {
    public bool IsValid() {
      throw new NotImplementedException();
    }
  }
}
