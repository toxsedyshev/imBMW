using System;
using Microsoft.SPOT;

namespace imBMW.Features.CanBus
{
    public class CanException : Exception
    {
        public CanException(string message) :
            base(message)
        { }
    }
}
