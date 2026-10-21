using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using News.Domain;

namespace News.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(NewsDbContext db, IPasswordHasher<User> hasher, IConfiguration config, bool allowDemo)
    {
        foreach (var role in new[] {
            new RoleDefinition { Id = Roles.Reader, Name = "Độc giả", BaseRole = Roles.Reader },
            new RoleDefinition { Id = Roles.Admin, Name = "Quản trị viên", BaseRole = Roles.Admin },
            new RoleDefinition { Id = Roles.Editor, Name = "Biên tập viên", BaseRole = Roles.Editor },
            new RoleDefinition { Id = Roles.Author, Name = "Tác giả", BaseRole = Roles.Author },
        })
            if (!await db.RoleDefinitions.AnyAsync(r => r.Id == role.Id)) db.RoleDefinitions.Add(role);
        await db.SaveChangesAsync();
        // Bổ sung chuyên mục còn thiếu cả khi DB đã có dữ liệu, giữ các danh mục tự tạo.
        var existingSlugs = (await db.Categories.Select(c => c.Slug).ToArrayAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        db.Categories.AddRange(new[] {
            new Category { Slug = "thoi-su", Name = "Thời sự" }, new Category { Slug = "the-gioi", Name = "Thế giới" },
            new Category { Slug = "kinh-te", Name = "Kinh tế" }, new Category { Slug = "phap-luat", Name = "Pháp luật" },
            new Category { Slug = "suc-khoe", Name = "Sức khỏe" }, new Category { Slug = "khoa-hoc", Name = "Khoa học" },
            new Category { Slug = "giai-tri", Name = "Giải trí" }, new Category { Slug = "xe", Name = "Xe" },
            new Category { Slug = "bat-dong-san", Name = "Bất động sản" },
            new Category { Slug = "cong-nghe", Name = "Công nghệ" }, new Category { Slug = "doi-song", Name = "Đời sống" },
            new Category { Slug = "du-lich", Name = "Du lịch" }, new Category { Slug = "van-hoa", Name = "Văn hóa" },
            new Category { Slug = "giao-duc", Name = "Giáo dục" }, new Category { Slug = "the-thao", Name = "Thể thao" },
        }.Where(category => !existingSlugs.Contains(category.Slug)));
        await db.SaveChangesAsync();
        if (allowDemo && bool.TryParse(config["Seed:DemoData"], out var demoData) && demoData)
        {
            await AddUser("admin@tin24.local", "Quản trị viên", Roles.Admin, "Tin24@Admin2026!");
            await AddUser("editor@tin24.local", "Biên tập viên", Roles.Editor, "Tin24@Editor2026!");
            var author = await AddUser("author@tin24.local", "Tác giả", Roles.Author, "Tin24@Author2026!");
            if (!await db.Articles.AnyAsync())
            {
                var articles = DemoArticles.Create();
                foreach (var article in articles)
                {
                    article.AssignSeedAuthor(author.Id);
                    // Chuyển seed text thành HTML đã encode; giữ ID 1..12 để URL mẫu ổn định.
                    var entry = db.Entry(article);
                    entry.Property(a => a.Content).CurrentValue = string.Join("", article.Content.Split("\n\n").Select(p => $"<p>{WebUtility.HtmlEncode(p)}</p>"));
                    if (db.Database.IsSqlServer()) entry.Property(a => a.Id).CurrentValue = 0;
                    db.Articles.Add(article);
                }
                await db.SaveChangesAsync();
            }
        }
        else if (await db.Users.CountAsync() == 0)
        {
            var email = config["Seed:AdminEmail"];
            var password = config["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(email) || email.Length > 254
                || !System.Net.Mail.MailAddress.TryCreate(email, out var adminAddress) || adminAddress.Address != email
                || string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 200)
                throw new InvalidOperationException("Lần khởi tạo đầu cần Seed:AdminEmail hợp lệ và Seed:AdminPassword (12–200 ký tự). Không có tài khoản mặc định trong Production.");
            await AddUser(email, "Quản trị viên", Roles.Admin, password);
        }

        // Tạo nhân sự lần đầu từ cấu hình riêng; không ghi đè mật khẩu/quyền đã tồn tại.
        foreach (var staff in config.GetSection("Seed:StaffUsers").GetChildren())
        {
            var email = staff["Email"]?.Trim().ToLowerInvariant();
            var password = staff["Password"];
            var role = staff["Role"];
            var name = staff["Name"]?.Trim();
            if (string.IsNullOrWhiteSpace(email) || email.Length > 254
                || !System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email
                || string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 200
                || string.IsNullOrWhiteSpace(name) || name.Length > 100
                || role is not (Roles.Admin or Roles.Editor or Roles.Author))
                throw new InvalidOperationException("Seed:StaffUsers cần email hợp lệ, name tối đa 100 ký tự, password từ 12 đến 200 ký tự và role Admin/Editor/Author.");
            await AddUser(email, name, role, password);
        }

        async Task<User> AddUser(string email, string name, string role, string password)
        {
            email = email.Trim().ToLowerInvariant();
            var existing = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
            if (existing is not null) return existing;
            var user = new User { Email = email, Name = name, Role = role, RoleDefinitionId = role };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }
    }
}
