using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDrill.Services;
public class DateTimeOffsetProvider : IDateTimeOffsetProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.Now;
    public DateTimeOffset Now => DateTimeOffset.Now;
}