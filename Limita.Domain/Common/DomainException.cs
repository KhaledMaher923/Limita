using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Domain.Common
{
    /// <summary>Thrown when a domain invariant is violated (for example, a negative transfer amount).</summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message)
        {    
        }
    }
}
