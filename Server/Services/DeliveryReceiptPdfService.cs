
using DeliveryTracker.Shared.Dtos.Deliveries;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace DeliveryTracker.Server.Services;

public class DeliveryReceiptPdfService
{
	private const string Navy = "#101D35";
	private const string Blue = "#2563EB";
	private const string Cyan = "#0891B2";
	private const string Muted = "#64748B";
	private const string Light = "#F1F5F9";
	private const string Border = "#E2E8F0";
	private const string Green = "#15803D";

	public byte[] Generate(
		DeliveryDto delivery,
		string shopScanUrl)
	{
		using var qrGenerator = new QRCodeGenerator();
		using var qrData = qrGenerator.CreateQrCode(
			shopScanUrl,
			QRCodeGenerator.ECCLevel.Q);

		var qrPng = new PngByteQRCode(qrData)
			.GetGraphic(8);

		var total = delivery.Items.Sum(x => x.Quantity);
		var itemCount = delivery.Items.Count;

		var localDate = delivery.DeliveryDate
			.ToUniversalTime()
			.AddHours(3)
			.ToString("yyyy/MM/dd  |  HH:mm");

		var receiptNo = $"DT-{delivery.Id:D6}";

		var document = Document.Create(container =>
		{
			container.Page(page =>
			{
				page.Size(PageSizes.A4);
				page.Margin(0);
				page.PageColor(Colors.White);

				page.DefaultTextStyle(x => x
					.FontFamily("Noto Naskh Arabic")
					.FontSize(11)
					.FontColor(Navy));

				// HEADER
				page.Header()
					.Background(Navy)
					.Padding(30)
					.Column(col =>
					{
						col.Spacing(14);

						col.Item().Row(row =>
						{
							row.RelativeItem().Column(left =>
							{
								left.Item()
									.Text("DeliveryTracker")
									.FontSize(24)
									.Bold()
									.FontColor(Colors.White);

								left.Item()
									.Text("SMART DISTRIBUTION SYSTEM")
									.FontSize(9)
									.FontColor("#A8C4E5");
							});

							row.ConstantItem(130)
								.AlignRight()
								.Text("DELIVERY RECEIPT")
								.FontSize(10)
								.Bold()
								.FontColor("#67E8F9");
						});

						col.Item()
							.LineHorizontal(1)
							.LineColor("#38506C");

						col.Item().Row(row =>
						{
							row.RelativeItem().Column(left =>
							{
								left.Item()
									.Text(receiptNo)
									.FontSize(20)
									.Bold()
									.FontColor(Colors.White);

								left.Item()
									.Text("OFFICIAL DELIVERY RECORD")
									.FontSize(9)
									.FontColor("#A8C4E5");
							});

							row.RelativeItem()
								.AlignRight()
								.Column(right =>
								{
									right.Item()
										.AlignRight()
										.Text("وصل تسليم مواد")
										.FontSize(21)
										.Bold()
										.FontColor(Colors.White);

									right.Item()
										.AlignRight()
										.Text("تم تسجيل العملية بنجاح")
										.FontSize(10)
										.FontColor("#86EFAC");
								});
						});
					});

				// CONTENT
				page.Content()
					.PaddingHorizontal(30)
					.PaddingTop(22)
					.PaddingBottom(12)
					.Column(col =>
					{
						col.Spacing(18);

						// SHOP + QR
						col.Item().Border(1)
							.BorderColor(Border)
							.CornerRadius(12)
							.Padding(16)
							.Row(row =>
							{
								row.ConstantItem(105)
									.Column(qr =>
									{
										qr.Item()
											.Width(95)
											.Height(95)
											.Image(qrPng)
											.FitArea();

										qr.Item()
											.AlignCenter()
											.Text("QR المحل")
											.FontSize(9)
											.FontColor(Muted);
									});

								row.RelativeItem()
									.PaddingLeft(12)
									.AlignRight()
									.Column(info =>
									{
										info.Spacing(5);

										info.Item()
											.AlignRight()
											.Text("جهة الاستلام")
											.FontSize(10)
											.FontColor(Muted);

										info.Item()
											.AlignRight()
											.Text(delivery.ShopName)
											.FontSize(19)
											.Bold();

										info.Item()
											.AlignRight()
											.Text(
												$"كود المحل: {delivery.ShopCode}")
											.FontSize(11);

										info.Item()
											.AlignRight()
											.Text(
												$"التاريخ: {localDate}")
											.FontSize(10)
											.FontColor(Muted);
									});
							});

						// STATS
						col.Item().Row(row =>
						{
							row.Spacing(10);

							row.RelativeItem().Element(c =>
								StatCard(c, "أنواع المواد",
									itemCount.ToString()));

							row.RelativeItem().Element(c =>
								StatCard(c, "إجمالي القطع",
									total.ToString()));

							row.RelativeItem().Element(c =>
								StatCard(c, "حالة العملية",
									"مكتمل"));
						});

						// ITEMS TITLE
						col.Item()
							.AlignRight()
							.Text("تفاصيل المواد المسلّمة")
							.FontSize(15)
							.Bold();

						// ITEMS TABLE
						col.Item().Table(table =>
						{
							table.ColumnsDefinition(columns =>
							{
								columns.ConstantColumn(80);
								columns.RelativeColumn();
								columns.ConstantColumn(45);
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
									.Text("#");
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

						// TOTAL
						col.Item()
							.Background(Navy)
							.CornerRadius(10)
							.Padding(16)
							.Row(row =>
							{
								row.RelativeItem()
									.Text($"{total:N0} قطعة")
									.FontSize(19)
									.Bold()
									.FontColor(Colors.White);

								row.RelativeItem()
									.AlignRight()
									.Text("إجمالي المواد المسلّمة")
									.FontSize(13)
									.Bold()
									.FontColor(Colors.White);
							});

						// NOTES
						if (!string.IsNullOrWhiteSpace(
							delivery.Notes))
						{
							col.Item()
								.Background(Light)
								.CornerRadius(8)
								.Padding(13)
								.Column(notes =>
								{
									notes.Item()
										.AlignRight()
										.Text("ملاحظات التسليم")
										.Bold()
										.FontColor(Cyan);

									notes.Item()
										.AlignRight()
										.Text(delivery.Notes);
								});
						}

						// SIGNATURES
						col.Item()
							.PaddingTop(28)
							.Row(row =>
							{
								row.RelativeItem()
									.PaddingRight(20)
									.Column(sign =>
									{
										sign.Item()
											.AlignCenter()
											.Text("توقيع المجهز");

										sign.Item()
											.PaddingTop(38)
											.LineHorizontal(1)
											.LineColor(Border);
									});

								row.RelativeItem()
									.PaddingLeft(20)
									.Column(sign =>
									{
										sign.Item()
											.AlignCenter()
											.Text("توقيع المستلم");

										sign.Item()
											.PaddingTop(38)
											.LineHorizontal(1)
											.LineColor(Border);
									});
							});
					});

				// FOOTER
				page.Footer()
					.PaddingHorizontal(30)
					.PaddingBottom(18)
					.Column(footer =>
					{
						footer.Item()
							.LineHorizontal(1)
							.LineColor(Border);

						footer.Item()
							.PaddingTop(8)
							.Row(row =>
							{
								row.RelativeItem()
									.Text("Generated by DeliveryTracker")
									.FontSize(9)
									.FontColor(Muted);

								row.RelativeItem()
									.AlignRight()
									.Text(t =>
									{
										t.Span("Page ");
										t.CurrentPageNumber();
										t.Span(" / ");
										t.TotalPages();
									});
							});
					});
			});
		});

		return document.GeneratePdf();
	}

	private static void StatCard(
		IContainer container,
		string label,
		string value)
	{
		container
			.Background(Light)
			.CornerRadius(9)
			.Padding(12)
			.Column(col =>
			{
				col.Item()
					.AlignCenter()
					.Text(value)
					.FontSize(17)
					.Bold()
					.FontColor(Blue);

				col.Item()
					.AlignCenter()
					.Text(label)
					.FontSize(9)
					.FontColor(Muted);
			});
	}

	private static IContainer TableHeader(
		IContainer container)
	{
		return container
			.Background(Navy)
			.Padding(10)
			.AlignCenter()
			.DefaultTextStyle(x => x
				.FontColor(Colors.White)
				.Bold());
	}

	private static IContainer TableCell(
		IContainer container)
	{
		return container
			.BorderBottom(1)
			.BorderColor(Border)
			.Padding(10)
			.AlignCenter();
	}
}
