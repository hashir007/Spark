namespace SparkService.Models
{
    public interface IResponseModel
    {
        string Message { get; set; }
        bool Success { get; set; }
    }
    public class ResponseModel<T> : IResponseModel
    {
        public T? Data { get; set; }
        public string? Message { get; set; }
        public bool Success { get; set; }
    }
}
