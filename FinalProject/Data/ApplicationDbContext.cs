using Microsoft.EntityFrameworkCore;
using FinalProject.Models; // Make sure this namespace matches where you placed your model classes
using Pomelo.EntityFrameworkCore.MySql.Infrastructure; // Needed for MySQL specific configurations

namespace FinalProject.Data // Recommended namespace for DbContext and Migrations
{
    /// <summary>
    /// Represents the database context for the finaelibrary application.
    /// This class is the main entry point for interacting with the database
    /// using Entity Framework Core.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
        /// </summary>
        /// <param name="options">The options to be used by this DbContext.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define DbSet properties for each of your model classes.
        // These properties represent collections of entities that can be queried from the database.

        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Publisher> Publishers { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<BookFormat> BookFormats { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Announcement> Announcements { get; set; }

        /// <summary>
        /// Configures the model that was discovered by convention from the entity types
        /// exposed in DbSet properties on this context.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Call the base implementation to apply configurations from Data Annotations first.
            base.OnModelCreating(modelBuilder);

            // --- Fluent API Configurations (Optional but recommended for robustness) ---
            // You can use the Fluent API here to provide more detailed configuration
            // that might not be possible or as clean with Data Annotations alone.

            // Example: Configure the Book entity
            modelBuilder.Entity<Book>(entity =>
            {
                // Ensure ISBN is unique (Data Annotation [StringLength] is already there)
                // This adds a unique index at the database level.
                entity.HasIndex(b => b.Isbn).IsUnique();

                // Configure precision and scale for decimal properties if not already done by Data Annotations
                // Although you have Column(TypeName), Fluent API can also be used.
                // entity.Property(b => b.ListPrice).HasPrecision(10, 2);
                // entity.Property(b => b.Rating).HasPrecision(3, 2);
                // entity.Property(b => b.SaleDiscount).HasPrecision(5, 2);

                // Configure relationships if needed (though ForeignKey attributes handle this well)
                // entity.HasOne(b => b.Author)
                //       .WithMany(a => a.Books)
                //       .HasForeignKey(b => b.AuthorId);
            });

            // Example: Configure the Author entity
            modelBuilder.Entity<Author>(entity =>
            {
                // Configure the FullName as ignored (already done by [NotMapped])
                // entity.Ignore(a => a.FullName);
            });

            // Example: Configure the Member entity
            modelBuilder.Entity<Member>(entity =>
            {
                // Ensure MembershipId is unique
                entity.HasIndex(m => m.MembershipId).IsUnique();

                // Ensure Email is unique
                entity.HasIndex(m => m.Email).IsUnique();

                // Configure the FullName as ignored (already done by [NotMapped])
                // entity.Ignore(m => m.FullName);

                // Configure precision and scale for decimal properties
                // entity.Property(m => m.StackableDiscount).HasPrecision(5, 2);
            });

            // Example: Configure the Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                 // Configure precision and scale for decimal properties
                // entity.Property(o => o.TotalAmount).HasPrecision(10, 2);
                // entity.Property(o => o.DiscountApplied).HasPrecision(5, 2);

                // Ensure ClaimCode is unique (if that's a requirement)
                // entity.HasIndex(o => o.ClaimCode).IsUnique();
            });

             // Example: Configure the OrderItem entity
            modelBuilder.Entity<OrderItem>(entity =>
            {
                 // Configure precision and scale for decimal properties
                // entity.Property(oi => oi.UnitPrice).HasPrecision(10, 2);
                // entity.Property(oi => oi.Discount).HasPrecision(5, 2);

                // Define composite primary key if needed (not needed here as you have OrderItemId)
                // entity.HasKey(oi => new { oi.OrderId, oi.BookId });

                // Configure relationships if needed
                // entity.HasOne(oi => oi.Order)
                //       .WithMany(o => o.OrderItems)
                //       .HasForeignKey(oi => oi.OrderId);

                // entity.HasOne(oi => oi.Book)
                //       .WithMany(b => b.OrderItems)
                //       .HasForeignKey(oi => oi.BookId);
            });

            // Example: Configure the Review entity
            modelBuilder.Entity<Review>(entity =>
            {
                // Define composite unique index for MemberId and BookId to prevent duplicate reviews by the same member for the same book
                entity.HasIndex(r => new { r.MemberId, r.BookId }).IsUnique();

                // Configure relationships if needed
                // entity.HasOne(r => r.Member)
                //       .WithMany(m => m.Reviews)
                //       .HasForeignKey(r => r.MemberId);

                // entity.HasOne(r => r.Book)
                //       .WithMany(b => b.Reviews)
                //       .HasForeignKey(r => r.BookId);
            });

             // Example: Configure the Admin entity
            modelBuilder.Entity<Admin>(entity =>
            {
                // Ensure Username is unique
                entity.HasIndex(a => a.Username).IsUnique();

                 // Ensure Email is unique (if required)
                // entity.HasIndex(a => a.Email).IsUnique();

                // Configure the FullName as ignored (already done by [NotMapped])
                // entity.Ignore(a => a.FullName);
            });

            // Example: Configure the Announcement entity
             modelBuilder.Entity<Announcement>(entity =>
            {
                // No specific Fluent API configuration needed based on current model
            });


            // Add any other global configurations here if necessary
            // For MySQL, you might configure character set and collation globally:
            // modelBuilder.UseMySql("your_connection_string",
            //     new MySqlServerVersion(new Version(8, 0, 21)), // Specify your MySQL version
            //     options => options.CharSet(CharSet.Utf8mb4).Collation("utf8mb4_unicode_ci"));

            // Or per entity:
            // modelBuilder.Entity<Book>().HasCharSet(CharSet.Utf8mb4).UseCollation("utf8mb4_unicode_ci");
        }
    }
}
