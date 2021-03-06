using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Controllers;
using TradeExample.Annotations;

namespace TradeExample
{
    public class NearToMarketService : INearToMarketService
    {
        private readonly ITradeService _tradeService;

        public NearToMarketService([NotNull] ITradeService tradeService)
        {
            if (tradeService == null) throw new ArgumentNullException("tradeService");
            _tradeService = tradeService;
        }

        public IObservable<IChangeSet<Trade, long>> Query(Func<uint> percentFromMarket)
        {
            if (percentFromMarket == null) throw new ArgumentNullException("percentFromMarket");

            return Observable.Create<IChangeSet<Trade, long>>
                (observer =>
                 {
                     var filter = new FilterController<Trade>();
                     filter.Change(trade =>
                                   {
                                       var near = percentFromMarket();
                                       return Math.Abs(trade.PercentFromMarket) <= near;
                                   });

                     var reevaluator = Observable.Interval(TimeSpan.FromMilliseconds(250))
                         .Subscribe(_ => filter.Reevaluate());

                     var subscriber = _tradeService.Trades
                         .Connect(trade=>trade.Status==TradeStatus.Live)
                         .Filter(filter)
                         .SubscribeSafe(observer);

                     return new CompositeDisposable(subscriber, filter, reevaluator);
                 });
        }
    }
}