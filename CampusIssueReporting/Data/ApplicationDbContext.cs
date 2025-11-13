using CampusIssueReporting.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>

{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
    {

    }
    public DbSet<Issue> Issues { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<FileRecord> FileRecords { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Issue - ApplicationUser (Reporter) relationship
        builder.Entity<Issue>()
            .HasOne(i => i.Reporter)
            .WithMany(u => u.Issues)
            .HasForeignKey(i => i.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Issue - ApplicationUser (AssignedAdmin) relationship
        builder.Entity<Issue>()
            .HasOne(i => i.AssignedAdmin)
            .WithMany()
            .HasForeignKey(i => i.AssignedAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // Issue - IssueCategory relationship
        builder.Entity<Issue>()
            .HasOne(i => i.Category)
            .WithMany(c => c.Issues)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Comment - Issue relationship
        builder.Entity<Comment>()
            .HasOne(c => c.Issue)
            .WithMany(i => i.Comments)
            .HasForeignKey(c => c.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        // Comment - ApplicationUser relationship
        builder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // FileRecord - ApplicationUser relationship
        builder.Entity<FileRecord>()
            .HasOne(f => f.UploadedBy)
            .WithMany(u => u.UploadedFiles)
            .HasForeignKey(f => f.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }

}