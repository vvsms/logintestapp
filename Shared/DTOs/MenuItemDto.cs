using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs;

public record MenuItemDto(int Id, string Title, string Url, string[] AllowedRoles, string[] AllowedPolicies, bool IsVisible, int Order);