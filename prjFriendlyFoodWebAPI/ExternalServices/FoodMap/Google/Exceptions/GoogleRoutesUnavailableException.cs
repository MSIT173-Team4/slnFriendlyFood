namespace prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Exceptions
{
    public class GoogleRoutesUnavailableException : Exception
    {
        public GoogleRoutesUnavailableException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
