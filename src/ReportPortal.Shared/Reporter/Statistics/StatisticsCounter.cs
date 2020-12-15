namespace ReportPortal.Shared.Reporter.Statistics
{
    /// <inheritdoc/>
    public class StatisticsCounter : IStatisticsCounter
    {
        private readonly object _lockObj = new object();

        private double _sum;

        /// <inheritdoc/>
        public double Min { get; private set; }

        /// <inheritdoc/>
        public double Max { get; private set; }

        /// <inheritdoc/>
        public double Avg
        {
            get
            {
                if (Count == 0)
                {
                    return 0;
                }
                else
                {
                    return _sum / Count;
                }
            }
        }

        /// <inheritdoc/>
        public long Count { get; private set; }

        /// <inheritdoc/>
        public void Measure(double duration)
        {
            lock (_lockObj)
            {
                if (Count == 0)
                {
                    Min = duration;
                    Max = duration;
                    _sum = duration;
                }
                else
                {
                    if (duration < Min)
                    {
                        Min = duration;
                    }
                    else if (duration > Max)
                    {
                        Max = duration;
                    }

                    _sum += duration;
                }

                Count++;
            }
        }

        /// <summary>
        /// Returns a string that represents the statistics counter.
        /// </summary>
        /// <returns>A string that represents the statistics counter.</returns>
        public override string ToString()
        {
            return $"Cnt {Count} Avg/Min/Max {Avg:0.##}/{Min:0.##}/{Max:0.##}s";
        }
    }
}
