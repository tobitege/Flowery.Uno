namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class DateDisplayExamples : ScrollableExamplePage
    {
        public DateDisplayExamples()
        {
            InitializeComponent();
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        public DateTime Today => DateTime.Today;
        public DateTime OneMonthAgo => DateTime.Today.AddMonths(-1);
        public DateTime OneMonthAhead => DateTime.Today.AddMonths(1);
        public DateTime CurrentMonthStart => new(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime CurrentMonthEnd => new(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
    }
}
