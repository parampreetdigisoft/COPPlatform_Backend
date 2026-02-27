using COPPlatform.Models;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace COPPlatform.Backgroundjob
{
    public class Download
    {
        private readonly ChannelService channelService;
        public Download(ChannelService channelService) 
        {
            this.channelService = channelService;
        }
        public string Type { get; set; } = string.Empty;
        public int? UserID { get; set; }
        public int? PillarID { get; set; }
        public string InsertAnalyticalLayerResults(int pillarID = 0)
        {
            PillarID = pillarID;
            Type = "InsertAnalyticalLayerResults";
            channelService.Write(this);
            return "Execution has been started";
        }
    }
}
