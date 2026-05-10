namespace n3amco.Api.Common
{
    public class AppException : Exception
    {
        public string ErrorCode { get; }
        public int StatusCode { get; }

        public AppException(string errorCode, int statusCode = 400)
            : base(errorCode)  
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
}