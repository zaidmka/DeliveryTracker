
using DeliveryTracker.Shared.Models;
using Microsoft.EntityFrameworkCore;
using DeliveryTracker.Server.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace DeliveryTracker.Server.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{
	}

	public DbSet<Shop> Shops => Set<Shop>();
	public DbSet<ItemType> ItemTypes => Set<ItemType>();
	public DbSet<Delivery> Deliveries => Set<Delivery>();
	public DbSet<DeliveryItem> DeliveryItems => Set<DeliveryItem>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Shop>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.HasIndex(x => x.Code).IsUnique();
			entity.HasIndex(x => x.QrToken).IsUnique();
		});

		modelBuilder.Entity<ItemType>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.HasIndex(x => x.Name).IsUnique();
		});

		modelBuilder.Entity<Delivery>(entity =>
		{
			entity.HasKey(x => x.Id);

			entity.HasOne(x => x.Shop)
				.WithMany(x => x.Deliveries)
				.HasForeignKey(x => x.ShopId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasIndex(x => new
			{
				x.ShopId,
				x.DeliveryDate
			});
		});

		modelBuilder.Entity<DeliveryItem>(entity =>
		{
			entity.HasKey(x => x.Id);

			entity.HasOne(x => x.Delivery)
				.WithMany(x => x.Items)
				.HasForeignKey(x => x.DeliveryId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(x => x.ItemType)
				.WithMany(x => x.DeliveryItems)
				.HasForeignKey(x => x.ItemTypeId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasIndex(x => new
			{
				x.DeliveryId,
				x.ItemTypeId
			}).IsUnique();

			entity.ToTable(t =>
				t.HasCheckConstraint(
					"CK_DeliveryItems_Quantity",
					"\"Quantity\" > 0"));
		});
	}
}
