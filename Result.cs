using System;

namespace SomeNamespace.Models
{
    public class Result<T> 
    {
        public bool IsOk { get; set; }
        public string ErrorMessage { get; set; }
        public T Data { get; set; }

        public Result()
        {
            IsOk = true;
            var theType = typeof(T);
            var constructor = theType.GetConstructor(Type.EmptyTypes);
            if (constructor != null)
            {
                Data = Activator.CreateInstance<T>();
            }
        }
    }
}