namespace COPPlatform.Dtos.kpiDto
{
    public class GetKpiLayerChartResponseDto
    {
        public List<string> Categories { get; set; } = new();
        public List<KpiLayerChartSeriesDto> Series { get; set; } = new();
        public List<GetAnalyticalLayerResultDto> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public decimal AverageScore { get; set; }
    }

    public class KpiLayerChartSeriesDto
    {
        public string Name { get; set; } = string.Empty;
        public List<decimal> Data { get; set; } = new();
    }
}
