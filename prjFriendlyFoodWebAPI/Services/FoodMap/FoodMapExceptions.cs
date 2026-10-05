namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    // 找不到資源（採買清單、行程）→ Controller 回 404
    public class FoodMapNotFoundException : Exception
    {
        public FoodMapNotFoundException(string message) : base(message)
        {
        }
    }

    // 資源存在但不屬於目前登入的使用者 → Controller 回 403
    public class FoodMapForbiddenException : Exception
    {
        public FoodMapForbiddenException(string message) : base(message)
        {
        }
    }

    internal static class GeoDistance
    {
        // Haversine 公式：兩個經緯度之間的直線距離（公尺），不花 Google API 額度
        public static double Meters(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusMeters = 6371000;

            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                    + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                    * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            return earthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    }
}
