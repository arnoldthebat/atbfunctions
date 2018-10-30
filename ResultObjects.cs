using System;

namespace uk.co.arnoldthebat.functions
{
    public abstract class ResultBase
    {
        public string MethodName { get; set;}
        public int GetBitsLeft { get; set;}
        public string GetHashedAPIKey { get; set;}
        public string GetSignature { get; set;}
    }

    public class DoubleResult : ResultBase
    {
        public double[] DecimalResults { get; set;}
    }

    public class IntResult : ResultBase
    {
        public int[] IntResults { get; set;}   
    }
}