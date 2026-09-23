namespace prjFriendlyFoodWebAPI.DTOs.Market
{
    public class UpdateProductStatusDto
    {
        /// <summary>0審核中/1販售中/2已售完/3未上架/4已違規</summary>
        public int Status { get; set; }
    }
}
