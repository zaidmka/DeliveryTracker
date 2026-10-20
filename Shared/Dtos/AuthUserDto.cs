using System;
using System.Collections.Generic;
using System.Text;

namespace DeliveryTracker.Shared.Dtos
{
	public class AuthUserDto
	{
		public string Id { get; set; } = string.Empty;
		public string? UserName { get; set; }
		public string? FullName { get; set; }
		public List<string> Roles { get; set; } = new();
	}
}
