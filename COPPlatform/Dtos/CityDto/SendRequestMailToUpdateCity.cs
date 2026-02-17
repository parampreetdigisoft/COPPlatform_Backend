using COPPlatform.Models;

namespace COPPlatform.Dtos.CityDto
{
    public class SendRequestMailToUpdateCity
    {
        public int UserID { get; set; }
        public int MailToUserID { get; set; }
        public int UserCityMappingID { get; set; }
    }
}
