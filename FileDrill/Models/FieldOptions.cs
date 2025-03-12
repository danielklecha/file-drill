using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDrill.Models;
public class FieldOptions
{
    public string? Description { get; set; }
    public FieldType Type { get; set; }
    public string[]? Enums { get; set; }
}