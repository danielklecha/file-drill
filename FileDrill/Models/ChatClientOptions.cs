using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDrill.Models;
public class ChatClientOptions
{
    public ChatClientType Type { get; set; }
    public string? Url { get; set; }
    public string? Key { get; set; }
    public string? ModelName { get; set; }
}
