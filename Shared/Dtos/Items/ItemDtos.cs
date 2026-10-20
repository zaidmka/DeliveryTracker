
using System.ComponentModel.DataAnnotations;

namespace DeliveryTracker.Shared.Dtos.Items;

public class ItemDto
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
	public string? Description { get; set; }
	public bool IsActive { get; set; }
}

public class SaveItemRequest
{
	[Required(ErrorMessage = "اسم المادة مطلوب")]
	[StringLength(150, ErrorMessage = "اسم المادة طويل جداً")]
	public string Name { get; set; } = "";

	[StringLength(300, ErrorMessage = "الوصف طويل جداً")]
	public string? Description { get; set; }

	public bool IsActive { get; set; } = true;
}
