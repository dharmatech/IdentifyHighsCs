using CsvHelper;
using CsvHelper.Configuration;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using System;
using System.Globalization;
using Microsoft.FSharp.Core;

namespace IdentifyHighsCs
{
    public class CsvRow
    {
        [CsvHelper.Configuration.Attributes.Index(0)] public long Time { get; set; }
        [CsvHelper.Configuration.Attributes.Index(1)] public decimal Open { get; set; }
        [CsvHelper.Configuration.Attributes.Index(2)] public decimal High { get; set; }
        [CsvHelper.Configuration.Attributes.Index(3)] public decimal Low { get; set; }
        [CsvHelper.Configuration.Attributes.Index(4)] public decimal Close { get; set; }
    }

    public class Candle
    {
        public DateTime DateTime { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { };

            // From TradingView CSV export

            var csv_items =
                new CsvReader(new StreamReader(@"..\..\..\SP_SPX, 1W.csv"), config)
                .GetRecords<CsvRow>()
                .ToList();

            var items = csv_items.Select(item =>
                new Candle()
                {
                    //DateTime = DateTimeOffset.FromUnixTimeSeconds(item.Time).UtcDateTime.Date,
                    DateTime = DateTimeOffset.FromUnixTimeSeconds(item.Time).UtcDateTime,
                    Open = item.Open,
                    High = item.High,
                    Low = item.Low,
                    Close = item.Close
                });

            List<Candle> identify_highs(List<Candle> rows, TimeSpan threshold)
            {
                var ls = new List<Candle>();

                var candidate = rows.First();
                                
                foreach (var row in rows)
                {
                    if (row.High > candidate.High)
                    {
                        var duration = (row.DateTime - candidate.DateTime).Duration();

                        if ((row.DateTime - candidate.DateTime).Duration() > threshold)
                        {
                            ls.Add(candidate);

                            candidate = row;
                        }
                        else
                        {
                            candidate = row;
                        }
                    }
                }

                ls.Add(candidate);

                return ls;
            }
                        
            var highs = identify_highs(items.ToList(), TimeSpan.FromDays(175));

            Console.WriteLine("DATE                  HIGH    DAYS SINCE LAST");

            Candle prev = null;

            foreach (var candle in highs)
            {
                Console.WriteLine("{0:yyyy-MM-dd HH:mm} {2,9:0.00}    {1,15:0}", 
                    candle.DateTime,
                    prev != null ? (candle.DateTime - prev.DateTime).Duration().TotalDays : 0,
                    candle.High
                    );

                prev = candle;
            }    

            var annotations = highs.Select(candle =>

                Annotation.init<DateTime, decimal, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible>
                                (
                                    X: candle.DateTime,
                                    Y: candle.High,
                                    null,
                                    ArrowColor: Color.fromString("black"),
                                    ArrowHead: StyleParam.ArrowHead.Barbed,
                                    null,
                                    ArrowSize: 2.0,
                                    null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
                                    null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
                                    null, null, null
                                )
            );
                                    
            var seq = items.Select(elt =>
                Tuple.Create(
                    elt.DateTime,
                    StockData.Create((double)elt.Open, (double)elt.High, (double)elt.Low, (double)elt.Close)));

            Chart2D.Chart.Candlestick<string>(seq)

                .WithYAxis(LinearAxis.init<IConvertible, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible>(
                    FixedRange: false))

                .WithConfig(Config.init(Responsive: true))
                .WithSize(1800, 900)

                .WithAnnotations(annotations)

                .WithTitle("SPX")

                .Show();
        }
    }
}