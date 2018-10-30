namespace Swarm.Core.Common
{
    public class ApiResult
    {
        public const int SuccessCode = 200;
        public const int InternalError = 300;
        public const int SwarmError = 301;
        public const int DbError = 302;
        public const int ModelNotValid = 303;
        public const int Error = 304;

        public int Code { get; }

        public string Msg { get; }

        public dynamic Data { get; }

        public ApiResult(int code = SuccessCode, string msg = null, dynamic data = null)
        {
            Code = code;
            Msg = msg;
            Data = data;
        }
    }
}