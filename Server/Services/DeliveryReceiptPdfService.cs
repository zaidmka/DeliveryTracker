
using DeliveryTracker.Shared.Dtos.Deliveries;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace DeliveryTracker.Server.Services;

public class DeliveryReceiptPdfService
{
	private const string Navy = "#14243B";
	private const string Blue = "#2563EB";
	private const string Muted = "#64748B";
	private const string Border = "#E2E8F0";
	private const string Soft = "#F4F7FB";
	private const string Green = "#16804A";

	public byte[] Generate(
		DeliveryDto delivery,
		string shopScanUrl)
	{
		using var qrGenerator = new QRCodeGenerator();
		using var qrData = qrGenerator.CreateQrCode(
			shopScanUrl,
			QRCodeGenerator.ECCLevel.Q);

		byte[] qrImage = new PngByteQRCode(qrData).GetGraphic(10);

		int total = delivery.Items.Sum(x => x.Quantity);
		int types = delivery.Items.Count;

		string receiptNo = $"DT-{delivery.Id:D6}";

		string date = delivery.DeliveryDate
			.ToUniversalTime()
			.AddHours(3)
			.ToString("yyyy/MM/dd - hh:mm tt");

		var document = Document.Create(container =>
		{
			container.Page(page =>
			{
				page.Size(PageSizes.A4);
				page.MarginHorizontal(30);
				page.MarginVertical(25);
				page.PageColor(Colors.White);

				page.DefaultTextStyle(x => x
					.FontFamily("Noto Naskh Arabic")
					.FontSize(11)
					.FontColor(Navy));

				// رأس الوصل
				page.Header().Column(header =>
				{
					header.Item().Row(row =>
					{
						row.RelativeItem().Column(brand =>
						{
							brand.Spacing(2);

							brand.Item()
								.Text("DeliveryTracker")
								.FontSize(23)
								.Bold()
								.FontColor(Navy);

							brand.Item()
								.Text("SMART DELIVERY MANAGEMENT")
								.FontSize(8)
								.FontColor(Muted);
						});

						row.RelativeItem()
							.AlignRight()
							.Column(title =>
							{
								title.Spacing(2);

								title.Item()
									.AlignRight()
									.Text("وصل تسليم مواد")
									.FontSize(19)
									.Bold();

								title.Item()
									.AlignRight()
									.Text(receiptNo)
									.FontSize(12)
									.Bold()
									.FontColor(Blue);
							});
					});

					header.Item()
						.PaddingTop(12)
						.LineHorizontal(2)
						.LineColor(Blue);
				});

				// محتوى الوصل
				page.Content()
					.PaddingTop(16)
					.Column(col =>
					{
						col.Spacing(12);

						// معلومات المحل والـ QR
						col.Item()
							.Background(Soft)
							.CornerRadius(8)
							.Padding(13)
							.Row(row =>
							{
								row.ConstantItem(94)
									.Column(qr =>
									{
										qr.Item()
											.Width(85)
											.Height(85)
											.Image(qrImage)
											.FitArea();

										qr.Item()
											.AlignCenter()
											.Text("رمز المحل")
											.FontSize(8)
											.FontColor(Muted);
									});

								row.RelativeItem()
									.PaddingLeft(15)
									.AlignRight()
									.Column(info =>
									{
										info.Spacing(4);

										info.Item()
											.AlignRight()
											.Text("بيانات جهة الاستلام")
											.FontSize(9)
											.FontColor(Muted);

										info.Item()
											.AlignRight()
											.Text(delivery.ShopName)
											.FontSize(17)
											.Bold();

										info.Item()
											.AlignRight()
											.Text(
												$"كود المحل: {delivery.ShopCode}");

										info.Item()
											.AlignRight()
											.Text($"التاريخ: {date}")
											.FontSize(10)
											.FontColor(Muted);

										info.Item()
											.AlignRight()
											.Text("● تم التسليم")
											.FontSize(10)
											.Bold()
											.FontColor(Green);
									});
							});

						// عنوان الجدول + الإحصائيات
						col.Item().Row(row =>
						{
							row.RelativeItem()
								.Text(
									$"{types} أنواع  |  {total:N0} قطعة")
								.FontSize(10)
								.FontColor(Muted);

							row.RelativeItem()
								.AlignRight()
								.Text("تفاصيل المواد المسلّمة")
								.FontSize(14)
								.Bold();
						});

						// جدول المواد
						col.Item().Table(table =>
						{
							table.ColumnsDefinition(columns =>
							{
								columns.ConstantColumn(85);
								columns.RelativeColumn();
								columns.ConstantColumn(40);
							});

							table.Header(header =>
							{
								header.Cell()
									.Element(TableHeader)
									.Text("الكمية");

								header.Cell()
									.Element(TableHeader)
									.AlignRight()
									.Text("اسم المادة");

								header.Cell()
									.Element(TableHeader)
									.Text("ت");
							});

							int index = 1;

							foreach (var item in delivery.Items)
							{
								table.Cell()
									.Element(TableCell)
									.Text(item.Quantity.ToString())
									.Bold()
									.FontColor(Blue);

								table.Cell()
									.Element(TableCell)
									.AlignRight()
									.Text(item.ItemName);

								table.Cell()
									.Element(TableCell)
									.Text((index++).ToString())
									.FontColor(Muted);
							}
						});

						// الإجمالي
						col.Item()
							.Background(Navy)
							.CornerRadius(6)
							.PaddingHorizontal(15)
							.PaddingVertical(10)
							.Row(row =>
							{
								row.RelativeItem()
									.Text($"{total:N0} قطعة")
									.FontSize(16)
									.Bold()
									.FontColor(Colors.White);

								row.RelativeItem()
									.AlignRight()
									.Text("إجمالي الكمية المسلّمة")
									.FontSize(12)
									.Bold()
									.FontColor(Colors.White);
							});

						// الملاحظات
						if (!string.IsNullOrWhiteSpace(delivery.Notes))
						{
							col.Item()
								.Border(1)
								.BorderColor(Border)
								.CornerRadius(6)
								.Padding(10)
								.Column(notes =>
								{
									notes.Spacing(3);

									notes.Item()
										.AlignRight()
										.Text("ملاحظات")
										.Bold()
										.FontSize(10);

									notes.Item()
										.AlignRight()
										.Text(delivery.Notes)
										.FontSize(10);
								});
						}

						// التواقيع
						col.Item()
							.PaddingTop(18)
							.Row(row =>
							{
								row.RelativeItem()
									.PaddingRight(22)
									.Column(sign =>
									{
										sign.Item()
											.AlignCenter()
											.Text("توقيع المجهز")
											.FontSize(10);

										sign.Item()
											.PaddingTop(28)
											.LineHorizontal(1)
											.LineColor(Border);
									});

								row.RelativeItem()
									.PaddingLeft(22)
									.Column(sign =>
									{
										sign.Item()
											.AlignCenter()
											.Text("توقيع المستلم")
											.FontSize(10);

										sign.Item()
											.PaddingTop(28)
											.LineHorizontal(1)
											.LineColor(Border);
									});
							});
					});

				// تذييل بسيط
				page.Footer().Column(footer =>
				{
					footer.Item()
						.LineHorizontal(1)
						.LineColor(Border);

					footer.Item()
						.PaddingTop(6)
						.Row(row =>
						{
							row.RelativeItem()
								.Text("DeliveryTracker • Digital Receipt")
								.FontSize(8)
								.FontColor(Muted);

							row.RelativeItem()
								.AlignRight()
								.Text(text =>
								{
									text.Span("Page ");
									text.CurrentPageNumber();
									text.Span(" / ");
									text.TotalPages();
								});
						});
				});
			});
		});

		return document.GeneratePdf();
	}

	private static IContainer TableHeader(IContainer container)
	{
		return container
			.Background(Navy)
			.PaddingVertical(8)
			.PaddingHorizontal(7)
			.AlignCenter()
			.DefaultTextStyle(x => x
				.FontColor(Colors.White)
				.Bold()
				.FontSize(10));
	}

	private static IContainer TableCell(IContainer container)
	{
		return container
			.BorderBottom(1)
			.BorderColor(Border)
			.PaddingVertical(7)
			.PaddingHorizontal(7)
			.AlignCenter();
	}
}
