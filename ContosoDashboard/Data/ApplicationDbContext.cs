using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Models;

namespace ContosoDashboard.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<TaskItem> Tasks { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<TaskComment> TaskComments { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<ProjectMember> ProjectMembers { get; set; } = null!;
    public DbSet<Announcement> Announcements { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<DocumentShare> DocumentShares { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User relationships
        modelBuilder.Entity<User>()
            .HasMany(static u => u.AssignedTasks)
            .WithOne(static t => t.AssignedUser)
            .HasForeignKey(static t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(static u => u.CreatedTasks)
            .WithOne(static t => t.CreatedByUser)
            .HasForeignKey(static t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(static u => u.ManagedProjects)
            .WithOne(static p => p.ProjectManager)
            .HasForeignKey(static p => p.ProjectManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure indexes for performance
        modelBuilder.Entity<TaskItem>()
            .HasIndex(static t => t.AssignedUserId);

        modelBuilder.Entity<TaskItem>()
            .HasIndex(static t => t.Status);

        modelBuilder.Entity<TaskItem>()
            .HasIndex(static t => t.DueDate);

        modelBuilder.Entity<Project>()
            .HasIndex(static p => p.ProjectManagerId);

        modelBuilder.Entity<Project>()
            .HasIndex(static p => p.Status);

        modelBuilder.Entity<Notification>()
            .HasIndex(static n => new { n.UserId, n.IsRead });

        modelBuilder.Entity<User>()
            .HasIndex(static u => u.Email)
            .IsUnique();

        // Configure Document relationships
        modelBuilder.Entity<Document>()
            .HasOne(static d => d.UploadedBy)
            .WithMany(static u => u.AssignedTasks.Cast<Document>()) // Reuse existing navigation - documents not explicitly mapped yet
            .HasForeignKey(static d => d.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasOne(static d => d.Project)
            .WithMany() // Project doesn't have Documents collection yet
            .HasForeignKey(static d => d.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure DocumentShare relationships
        modelBuilder.Entity<DocumentShare>()
            .HasOne(static ds => ds.Document)
            .WithMany(static d => d.Shares)
            .HasForeignKey(static ds => ds.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DocumentShare>()
            .HasOne(static ds => ds.SharedWith)
            .WithMany()
            .HasForeignKey(static ds => ds.SharedWithUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DocumentShare>()
            .HasOne(static ds => ds.SharedBy)
            .WithMany()
            .HasForeignKey(static ds => ds.SharedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure indexes for Document
        modelBuilder.Entity<Document>()
            .HasIndex(static d => d.UploadedById);

        modelBuilder.Entity<Document>()
            .HasIndex(static d => d.ProjectId);

        modelBuilder.Entity<Document>()
            .HasIndex(static d => d.Category);

        modelBuilder.Entity<Document>()
            .HasIndex(static d => d.CreatedAt);

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed an admin user
        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserId = 1,
                Email = "admin@contoso.com",
                DisplayName = "System Administrator",
                Department = "IT",
                JobTitle = "Administrator",
                Role = UserRole.Administrator,
                AvailabilityStatus = AvailabilityStatus.Available,
                CreatedDate = DateTime.UtcNow,
                EmailNotificationsEnabled = true,
                InAppNotificationsEnabled = true
            },
            new User
            {
                UserId = 2,
                Email = "camille.nicole@contoso.com",
                DisplayName = "Camille Nicole",
                Department = "Engineering",
                JobTitle = "Project Manager",
                Role = UserRole.ProjectManager,
                AvailabilityStatus = AvailabilityStatus.Available,
                CreatedDate = DateTime.UtcNow,
                EmailNotificationsEnabled = true,
                InAppNotificationsEnabled = true
            },
            new User
            {
                UserId = 3,
                Email = "floris.kregel@contoso.com",
                DisplayName = "Floris Kregel",
                Department = "Engineering",
                JobTitle = "Team Lead",
                Role = UserRole.TeamLead,
                AvailabilityStatus = AvailabilityStatus.Available,
                CreatedDate = DateTime.UtcNow,
                EmailNotificationsEnabled = true,
                InAppNotificationsEnabled = true
            },
            new User
            {
                UserId = 4,
                Email = "ni.kang@contoso.com",
                DisplayName = "Ni Kang",
                Department = "Engineering",
                JobTitle = "Software Engineer",
                Role = UserRole.Employee,
                AvailabilityStatus = AvailabilityStatus.Available,
                CreatedDate = DateTime.UtcNow,
                EmailNotificationsEnabled = true,
                InAppNotificationsEnabled = true
            }
        );

        // Seed a sample project
        modelBuilder.Entity<Project>().HasData(
            new Project
            {
                ProjectId = 1,
                Name = "ContosoDashboard Development",
                Description = "Internal employee productivity dashboard",
                ProjectManagerId = 2,
                StartDate = DateTime.UtcNow.AddDays(-30),
                TargetCompletionDate = DateTime.UtcNow.AddDays(60),
                Status = ProjectStatus.Active,
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                UpdatedDate = DateTime.UtcNow
            }
        );

        // Seed sample tasks
        modelBuilder.Entity<TaskItem>().HasData(
            new TaskItem
            {
                TaskId = 1,
                Title = "Design database schema",
                Description = "Create entity relationship diagram and database design",
                Priority = TaskPriority.High,
                Status = Models.TaskStatus.Completed,
                DueDate = DateTime.UtcNow.AddDays(-20),
                AssignedUserId = 4,
                CreatedByUserId = 2,
                ProjectId = 1,
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                UpdatedDate = DateTime.UtcNow.AddDays(-20)
            },
            new TaskItem
            {
                TaskId = 2,
                Title = "Implement authentication",
                Description = "Set up Microsoft Entra ID authentication",
                Priority = TaskPriority.Critical,
                Status = Models.TaskStatus.InProgress,
                DueDate = DateTime.UtcNow.AddDays(5),
                AssignedUserId = 4,
                CreatedByUserId = 2,
                ProjectId = 1,
                CreatedDate = DateTime.UtcNow.AddDays(-25),
                UpdatedDate = DateTime.UtcNow
            },
            new TaskItem
            {
                TaskId = 3,
                Title = "Create UI mockups",
                Description = "Design user interface mockups for all main pages",
                Priority = TaskPriority.Medium,
                Status = Models.TaskStatus.NotStarted,
                DueDate = DateTime.UtcNow.AddDays(10),
                AssignedUserId = 4,
                CreatedByUserId = 2,
                ProjectId = 1,
                CreatedDate = DateTime.UtcNow.AddDays(-20),
                UpdatedDate = DateTime.UtcNow.AddDays(-20)
            }
        );

        // Seed project members
        modelBuilder.Entity<ProjectMember>().HasData(
            new ProjectMember
            {
                ProjectMemberId = 1,
                ProjectId = 1,
                UserId = 3,
                Role = "TeamLead",
                AssignedDate = DateTime.UtcNow.AddDays(-30)
            },
            new ProjectMember
            {
                ProjectMemberId = 2,
                ProjectId = 1,
                UserId = 4,
                Role = "Developer",
                AssignedDate = DateTime.UtcNow.AddDays(-30)
            }
        );

        // Seed announcement
        modelBuilder.Entity<Announcement>().HasData(
            new Announcement
            {
                AnnouncementId = 1,
                Title = "Welcome to ContosoDashboard",
                Content = "Welcome to the new ContosoDashboard application. This platform will help you manage your tasks and projects more efficiently.",
                CreatedByUserId = 1,
                PublishDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            }
        );
    }
}
