namespace ReportPortal.Shared.Reporter.Statistics
{
    /// <inheritdoc/>
    public class StatisticsCounter : IStatisticsCounter
    {
        private readonly object _lockObj = new object();

        private long _count;

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
                if (_count == 0)
                {
                    return 0;
                }
                else
                {
                    return _sum / _count;
                }
            }
        }

        /// <inheritdoc/>
        public void Measure(double duration)
        {
            lock (_lockObj)
            {
                if (_count == 0)
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

                _count++;
            }
        }
    }
}
