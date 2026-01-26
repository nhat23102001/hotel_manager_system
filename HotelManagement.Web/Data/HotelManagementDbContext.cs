using HotelManagement.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Data
{
    public class HotelManagementDbContext : DbContext
    {
        public HotelManagementDbContext(DbContextOptions<HotelManagementDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<RoomType> RoomTypes => Set<RoomType>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Blog> Blogs => Set<Blog>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingDetail> BookingDetails => Set<BookingDetail>();
        public DbSet<Contact> Contacts => Set<Contact>();
        public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<BookingService> BookingServices => Set<BookingService>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasMany(u => u.Blogs)
                    .WithOne(b => b.Author)
                    .HasForeignKey(b => b.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(u => u.Profile)
                    .WithOne(p => p.User)
                    .HasForeignKey<UserProfile>(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(u => u.Bookings)
                    .WithOne(b => b.User)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(u => u.RepliedContacts)
                    .WithOne(c => c.RepliedByUser)
                    .HasForeignKey(c => c.RepliedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.ToTable("UserProfiles");
            });

            modelBuilder.Entity<RoomType>(entity =>
            {
                entity.ToTable("RoomTypes");
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.ToTable("Rooms");
                entity.HasIndex(r => r.RoomCode).IsUnique();
                entity.Property(r => r.PricePerNight).HasColumnType("decimal(18,2)");
                entity.HasOne(r => r.RoomType)
                    .WithMany(rt => rt.Rooms)
                    .HasForeignKey(r => r.RoomTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Blog>(entity =>
            {
                entity.ToTable("Blogs");
            });

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.ToTable("Bookings");
                entity.Property(b => b.SubTotal).HasColumnType("decimal(18,2)");
                entity.Property(b => b.VAT).HasColumnType("decimal(18,2)");
                entity.Property(b => b.TotalAmount).HasColumnType("decimal(18,2)");
                entity.HasMany(b => b.Details)
                    .WithOne(d => d.Booking)
                    .HasForeignKey(d => d.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(b => b.Services)
                    .WithOne(s => s.Booking)
                    .HasForeignKey(s => s.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BookingDetail>(entity =>
            {
                entity.ToTable("BookingDetails");
                entity.Property(d => d.PricePerNight).HasColumnType("decimal(18,2)");
                entity.HasOne(d => d.Room)
                    .WithMany(r => r.BookingDetails)
                    .HasForeignKey(d => d.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Contact>(entity =>
            {
                entity.ToTable("Contacts");
            });

            modelBuilder.Entity<ServiceType>(entity =>
            {
                entity.ToTable("ServiceTypes");
            });

            modelBuilder.Entity<Service>(entity =>
            {
                entity.ToTable("Services");
                entity.Property(s => s.UnitPrice).HasColumnType("decimal(18,2)");
                entity.HasOne(s => s.ServiceType)
                    .WithMany(st => st.Services)
                    .HasForeignKey(s => s.ServiceTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BookingService>(entity =>
            {
                entity.ToTable("BookingServices");
                entity.Property(s => s.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(s => s.TotalPrice).HasColumnType("decimal(18,2)");
            });
        }
    }
}
