using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDrill.Services;
public interface IDateTimeOffsetProvider
{
    DateTimeOffset Now { get; }
    DateTimeOffset UtcNow { get; }
}