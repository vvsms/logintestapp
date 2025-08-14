using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs;

public class MenuItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public List<string> Roles { get; set; } = new();
    public List<string> Policies { get; set; } = new();
}