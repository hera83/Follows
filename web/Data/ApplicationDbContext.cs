using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data.Entities;

namespace web.Data
{
    /// <summary>
    /// Application database context for Identity, app settings, themes and file metadata
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // App settings
        public DbSet<AppSetting> AppSettings { get; set; } = null!;

        // Theme settings
        public DbSet<ThemeSetting> ThemeSettings { get; set; } = null!;

        // File metadata (files themselves stored in App_files/)
        public DbSet<FileMetadata> FileMetadata { get; set; } = null!;

        // SMS messages (sent and received via the SMS gateway service)
        public DbSet<SmsMessage> SmsMessages { get; set; } = null!;

        // Feeds: stories/updates, their media, comments and likes
        public DbSet<FeedPost> FeedPosts { get; set; } = null!;
        public DbSet<FeedMedia> FeedMedia { get; set; } = null!;
        public DbSet<FeedComment> FeedComments { get; set; } = null!;
        public DbSet<FeedLike> FeedLikes { get; set; } = null!;
        public DbSet<FeedPostTranslation> FeedPostTranslations { get; set; } = null!;
        public DbSet<FeedCommentTranslation> FeedCommentTranslations { get; set; } = null!;

        // Documents: folder-like groups of uploaded files
        public DbSet<DocumentGroup> DocumentGroups { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<DocumentTranslation> DocumentTranslations { get; set; } = null!;

        // Bulk UI-catalog translation (menu/Feed/Documents/Profil chrome) - see web/Infrastructure/UiTranslation/*
        public DbSet<UiTranslationEntry> UiTranslationEntries { get; set; } = null!;
        public DbSet<InstalledLanguage> InstalledLanguages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ThemePreference)
                    .HasMaxLength(10);

                entity.Property(e => e.PreferredLanguage)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasDefaultValue(AppLanguages.Default);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(e => e.Email)
                    .IsUnique();
            });

            // Configure AppSetting
            builder.Entity<AppSetting>(entity =>
            {
                entity.HasKey(e => e.Key);

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Value)
                    .IsRequired();
            });

            // Configure ThemeSetting
            builder.Entity<ThemeSetting>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ThemeMode)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasIndex(e => new { e.Name, e.ThemeMode })
                    .IsUnique();
            });

            // Configure FileMetadata
            builder.Entity<FileMetadata>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.OriginalFileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.StoredFileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.ContentType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Category)
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                // Optional: Relationship to ApplicationUser (owner)
                // entity.HasOne<ApplicationUser>()
                //     .WithMany()
                //     .HasForeignKey(e => e.OwnerId)
                //     .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure SmsMessage
            builder.Entity<SmsMessage>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Direction)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.PhoneNumber)
                    .IsRequired()
                    .HasMaxLength(32);

                entity.Property(e => e.Body)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.FailureReason)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(e => e.GatewayMessageId);
                entity.HasIndex(e => e.PhoneNumber);
                entity.HasIndex(e => e.CreatedAtUtc);
            });

            // Configure FeedPost
            builder.Entity<FeedPost>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.AuthorId)
                    .IsRequired();

                entity.Property(e => e.Caption)
                    .HasMaxLength(FeedLimits.MaxCaptionLength);

                entity.Property(e => e.OriginalLanguage)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasDefaultValue(AppLanguages.Default);

                entity.Property(e => e.IsLanguageVerified)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                // Restrict: a user's posts must be cleaned up explicitly (FeedService.DeletePostAsync)
                // before the user itself can be deleted, since deleting also removes physical files.
                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(e => e.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.CreatedAtUtc);
            });

            // Configure FeedPostTranslation
            builder.Entity<FeedPostTranslation>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.LanguageCode)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.TranslatedText)
                    .IsRequired();

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasOne(e => e.Post)
                    .WithMany(p => p.Translations)
                    .HasForeignKey(e => e.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.PostId, e.LanguageCode }).IsUnique();
            });

            // Configure FeedMedia
            builder.Entity<FeedMedia>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.MediaType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.TranscodeStatus)
                    .HasMaxLength(20);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasOne(e => e.Post)
                    .WithMany(p => p.Media)
                    .HasForeignKey(e => e.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Restrict: FileMetadata row + physical file are removed explicitly by
                // FeedService, never silently via cascade.
                entity.HasOne(e => e.File)
                    .WithMany()
                    .HasForeignKey(e => e.FileMetadataId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.PostId, e.SortOrder });
            });

            // Configure FeedComment
            builder.Entity<FeedComment>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.AuthorId)
                    .IsRequired();

                entity.Property(e => e.Body)
                    .IsRequired()
                    .HasMaxLength(FeedLimits.MaxCommentLength);

                entity.Property(e => e.OriginalLanguage)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasDefaultValue(AppLanguages.Default);

                entity.Property(e => e.IsLanguageVerified)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasOne(e => e.Post)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(e => e.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(e => e.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.PostId);
            });

            // Configure FeedCommentTranslation
            builder.Entity<FeedCommentTranslation>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.LanguageCode)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.TranslatedText)
                    .IsRequired();

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasOne(e => e.Comment)
                    .WithMany(c => c.Translations)
                    .HasForeignKey(e => e.CommentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.CommentId, e.LanguageCode }).IsUnique();
            });

            // Configure FeedLike
            builder.Entity<FeedLike>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserId)
                    .IsRequired();

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasOne(e => e.Post)
                    .WithMany(p => p.Likes)
                    .HasForeignKey(e => e.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.PostId, e.UserId }).IsUnique();
            });

            // Configure DocumentGroup
            builder.Entity<DocumentGroup>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(DocumentLimits.MaxGroupNameLength);

                entity.Property(e => e.Description)
                    .HasMaxLength(DocumentLimits.MaxGroupDescriptionLength);

                entity.Property(e => e.CreatedByUserId)
                    .IsRequired();

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                // Restrict: a user's groups must be cleaned up explicitly (DocumentService.DeleteGroupAsync)
                // before the user itself can be deleted, since deleting also removes physical files.
                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.CreatedAtUtc);
            });

            // Configure Document
            builder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(260);

                entity.Property(e => e.UploadedByUserId)
                    .IsRequired();

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasOne(e => e.Group)
                    .WithMany(g => g.Documents)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Restrict: FileMetadata row + physical file are removed explicitly by
                // DocumentService, never silently via cascade.
                entity.HasOne(e => e.File)
                    .WithMany()
                    .HasForeignKey(e => e.FileMetadataId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(e => e.UploadedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.GroupId, e.CreatedAtUtc });
            });

            // Configure DocumentTranslation
            builder.Entity<DocumentTranslation>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.LanguageCode)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.TranslatedMarkdown)
                    .IsRequired();

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasOne(e => e.Document)
                    .WithMany(d => d.Translations)
                    .HasForeignKey(e => e.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.DocumentId, e.LanguageCode }).IsUnique();
            });

            // Configure UiTranslationEntry
            builder.Entity<UiTranslationEntry>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.SourceTextHash)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(e => e.SourceText)
                    .IsRequired();

                entity.Property(e => e.LanguageCode)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.TranslatedText)
                    .IsRequired();

                entity.Property(e => e.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(e => new { e.SourceTextHash, e.LanguageCode }).IsUnique();
            });

            // Configure InstalledLanguage
            builder.Entity<InstalledLanguage>(entity =>
            {
                entity.HasKey(e => e.LanguageCode);

                entity.Property(e => e.LanguageCode)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.InstalledAtUtc)
                    .IsRequired();
            });
        }
    }
}
