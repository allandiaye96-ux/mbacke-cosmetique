using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Models;
using ErpCosmetiqueApi.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpCosmetiqueApi.Endpoints;

public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/invoices").WithTags("Facturation").RequireAuthorization();

        group.MapGet("/", async (InvoiceService invoices, DateOnly? from, DateOnly? to) =>
            await invoices.GetInvoicesAsync(from, to));

        group.MapGet("/{id:int}", async (int id, InvoiceService invoices) =>
        {
            var inv = await invoices.GetInvoiceAsync(id);
            return inv is null ? Results.NotFound() : Results.Ok(inv);
        });

        group.MapGet("/sale/{saleId:int}", async (int saleId, InvoiceService invoices) =>
        {
            var inv = await invoices.GetInvoiceBySaleAsync(saleId);
            return inv is null ? Results.NotFound() : Results.Ok(inv);
        });

        group.MapPost("/from-sale/{saleId:int}", async (int saleId, InvoiceService invoices) =>
        {
            try
            {
                var inv = await invoices.CreateFromSaleAsync(saleId);
                return Results.Created($"/api/invoices/{inv.Id}", inv);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        });

        group.MapPost("/{id:int}/cancel", async (int id, InvoiceService invoices) =>
        {
            try
            {
                var inv = await invoices.CancelInvoiceAsync(id);
                return Results.Ok(inv);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
            { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapGet("/{id:int}/pdf", async (int id, InvoiceService invoices) =>
        {
            try
            {
                var pdf = await invoices.GeneratePdfAsync(id);
                return Results.File(pdf, "application/pdf", $"facture-{id}.pdf");
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });
    }
}

public static class CashEndpoints
{
    public static void MapCashEndpoints(this WebApplication app)
    {
        var boxes = app.MapGroup("/api/cash-boxes").WithTags("Caisse").RequireAuthorization();
        boxes.MapGet("/", async (CashService cash) => await cash.GetCashBoxesAsync());
        boxes.MapPost("/", async (CreateCashBoxDto dto, CashService cash) =>
        {
            var box = await cash.CreateCashBoxAsync(dto);
            return Results.Created($"/api/cash-boxes/{box.Id}", box);
        });
        boxes.MapGet("/{id:int}/balance", async (int id, CashService cash) =>
        {
            try
            {
                var balance = await cash.GetBalanceAsync(id);
                return Results.Ok(new { cashBoxId = id, balance });
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });

        var expenses = app.MapGroup("/api/expenses").WithTags("Dépenses").RequireAuthorization();
        expenses.MapGet("/", async (CashService cash, int? cashBoxId, DateOnly? from, DateOnly? to) =>
            await cash.GetTransactionsAsync(cashBoxId, CashTransactionKind.Expense, from, to));
        expenses.MapPost("/", async (CreateCashExpenseDto dto, CashService cash) =>
        {
            try
            {
                var tx = await cash.CreateExpenseAsync(dto);
                return Results.Created($"/api/expenses/{tx.Id}", tx);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        });
        expenses.MapPut("/{id:long}", async (long id, CreateCashExpenseDto dto, CashService cash) =>
        {
            var tx = await cash.UpdateExpenseAsync(id, dto);
            return tx is null ? Results.NotFound() : Results.Ok(tx);
        });
        expenses.MapDelete("/{id:long}", async (long id, CashService cash) =>
        {
            var ok = await cash.DeleteTransactionAsync(id);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        var transactions = app.MapGroup("/api/cash-transactions").WithTags("Transactions caisse").RequireAuthorization();
        transactions.MapGet("/", async (CashService cash, int? cashBoxId, CashTransactionKind? kind, DateOnly? from, DateOnly? to) =>
            await cash.GetTransactionsAsync(cashBoxId, kind, from, to));
    }
}

public static class PersonnelEndpoints
{
    public static void MapPersonnelEndpoints(this WebApplication app)
    {
        var roles = app.MapGroup("/api/employee-roles").WithTags("Rôles").RequireAuthorization();
        roles.MapGet("/", async (ErpDbContext db) =>
            await db.EmployeeRoles.Select(x => new EmployeeRoleDto(x.Id, x.Name)).ToListAsync());
        roles.MapPost("/", async (CreateEmployeeRoleDto dto, ErpDbContext db) =>
        {
            var role = new EmployeeRole { Name = dto.Name };
            db.EmployeeRoles.Add(role);
            await db.SaveChangesAsync();
            return Results.Created($"/api/employee-roles/{role.Id}", new EmployeeRoleDto(role.Id, role.Name));
        });
        roles.MapPut("/{id:int}", async (int id, CreateEmployeeRoleDto dto, ErpDbContext db) =>
        {
            var role = await db.EmployeeRoles.FindAsync(id);
            if (role is null) return Results.NotFound();
            role.Name = dto.Name;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
        roles.MapDelete("/{id:int}", async (int id, ErpDbContext db) =>
        {
            var role = await db.EmployeeRoles.Include(x => x.Employees).FirstOrDefaultAsync(x => x.Id == id);
            if (role is null) return Results.NotFound();
            if (role.Employees.Any())
                return Results.BadRequest(new { error = "Ce rôle est utilisé par des employés" });
            db.EmployeeRoles.Remove(role);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        var employees = app.MapGroup("/api/employees").WithTags("Employés").RequireAuthorization();
        employees.MapGet("/", async (ErpDbContext db) =>
        {
            return await db.Employees
                .Include(x => x.Role)
                .Select(x => new EmployeeDto(
                    x.Id, x.Name, x.Phone, x.Email,
                    x.RoleId, x.Role.Name, x.MonthlySalary, x.IsActive, x.HiredAt))
                .ToListAsync();
        });
        employees.MapPost("/", async (CreateEmployeeDto dto, ErpDbContext db) =>
        {
            var emp = new Employee
            {
                Name = dto.Name, Phone = dto.Phone, Email = dto.Email,
                RoleId = dto.RoleId, MonthlySalary = dto.MonthlySalary
            };
            db.Employees.Add(emp);
            await db.SaveChangesAsync();
            var role = await db.EmployeeRoles.FindAsync(emp.RoleId);
            return Results.Created($"/api/employees/{emp.Id}",
                new EmployeeDto(emp.Id, emp.Name, emp.Phone, emp.Email,
                    emp.RoleId, role!.Name, emp.MonthlySalary, emp.IsActive, emp.HiredAt));
        });
        employees.MapPut("/{id:int}", async (int id, CreateEmployeeDto dto, ErpDbContext db) =>
        {
            var emp = await db.Employees.FindAsync(id);
            if (emp is null) return Results.NotFound();
            emp.Name = dto.Name; emp.Phone = dto.Phone; emp.Email = dto.Email;
            emp.RoleId = dto.RoleId; emp.MonthlySalary = dto.MonthlySalary;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
        employees.MapDelete("/{id:int}", async (int id, ErpDbContext db) =>
        {
            var emp = await db.Employees.FindAsync(id);
            if (emp is null) return Results.NotFound();
            emp.IsActive = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        var attendance = app.MapGroup("/api/attendance").WithTags("Présences").RequireAuthorization();
        attendance.MapGet("/", async (ErpDbContext db, int? employeeId, DateOnly? date) =>
        {
            var query = db.Attendances.Include(x => x.Employee).AsQueryable();
            if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);
            if (date.HasValue) query = query.Where(x => x.Date == date.Value);
            return await query.Select(x => new AttendanceDto(
                x.Id, x.EmployeeId, x.Employee.Name, x.Date, x.Present, x.Note)).ToListAsync();
        });
        attendance.MapPost("/", async (CreateAttendanceDto dto, ErpDbContext db) =>
        {
            var att = new Attendance
            {
                EmployeeId = dto.EmployeeId, Date = dto.Date,
                Present = dto.Present, Note = dto.Note
            };
            db.Attendances.Add(att);
            await db.SaveChangesAsync();
            var emp = await db.Employees.FindAsync(att.EmployeeId);
            return Results.Created($"/api/attendance/{att.Id}",
                new AttendanceDto(att.Id, att.EmployeeId, emp!.Name, att.Date, att.Present, att.Note));
        });
        attendance.MapPut("/{id:long}", async (long id, CreateAttendanceDto dto, ErpDbContext db) =>
        {
            var att = await db.Attendances.FindAsync(id);
            if (att is null) return Results.NotFound();
            att.EmployeeId = dto.EmployeeId;
            att.Date = dto.Date;
            att.Present = dto.Present;
            att.Note = dto.Note;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
        attendance.MapDelete("/{id:long}", async (long id, ErpDbContext db) =>
        {
            var att = await db.Attendances.FindAsync(id);
            if (att is null) return Results.NotFound();
            db.Attendances.Remove(att);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        var advances = app.MapGroup("/api/salary-advances").WithTags("Avances salaire").RequireAuthorization();
        advances.MapGet("/", async (ErpDbContext db, int? employeeId) =>
        {
            var query = db.SalaryAdvances.Include(x => x.Employee).AsQueryable();
            if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);
            return await query.Select(x => new SalaryAdvanceDto(
                x.Id, x.EmployeeId, x.Employee.Name, x.Amount, x.CreatedAt, x.Motive)).ToListAsync();
        });
        advances.MapPost("/", async (CreateSalaryAdvanceDto dto, ErpDbContext db) =>
        {
            var adv = new SalaryAdvance
            {
                EmployeeId = dto.EmployeeId, Amount = dto.Amount, Motive = dto.Motive
            };
            db.SalaryAdvances.Add(adv);
            await db.SaveChangesAsync();
            var emp = await db.Employees.FindAsync(adv.EmployeeId);
            return Results.Created($"/api/salary-advances/{adv.Id}",
                new SalaryAdvanceDto(adv.Id, adv.EmployeeId, emp!.Name, adv.Amount, adv.CreatedAt, adv.Motive));
        });
        advances.MapPut("/{id:long}", async (long id, CreateSalaryAdvanceDto dto, ErpDbContext db) =>
        {
            var adv = await db.SalaryAdvances.FindAsync(id);
            if (adv is null) return Results.NotFound();
            adv.EmployeeId = dto.EmployeeId;
            adv.Amount = dto.Amount;
            adv.Motive = dto.Motive;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
        advances.MapDelete("/{id:long}", async (long id, ErpDbContext db) =>
        {
            var adv = await db.SalaryAdvances.FindAsync(id);
            if (adv is null) return Results.NotFound();
            db.SalaryAdvances.Remove(adv);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        var dashboard = app.MapGroup("/api/dashboard").WithTags("Tableau de bord").RequireAuthorization();
        dashboard.MapGet("/", async (DashboardService dashboard) => await dashboard.GetDashboardAsync());

        var reports = app.MapGroup("/api/reports").WithTags("Rapports").RequireAuthorization();
        reports.MapGet("/daily", async (ReportService reports, DateOnly? date) =>
            await reports.GetDailyReportAsync(date));
        reports.MapGet("/weekly", async (ReportService reports) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var from = today.AddDays(-(int)today.DayOfWeek);
            return await reports.GetPeriodReportAsync(from, today);
        });
        reports.MapGet("/monthly", async (ReportService reports, int? year, int? month) =>
        {
            var now = DateTime.UtcNow;
            var y = year ?? now.Year;
            var m = month ?? now.Month;
            var from = new DateOnly(y, m, 1);
            var to = from.AddMonths(1).AddDays(-1);
            return await reports.GetPeriodReportAsync(from, to);
        });
        reports.MapGet("/yearly", async (ReportService reports, int? year) =>
        {
            var y = year ?? DateTime.UtcNow.Year;
            return await reports.GetPeriodReportAsync(new DateOnly(y, 1, 1), new DateOnly(y, 12, 31));
        });
        reports.MapGet("/period", async (ReportService reports, DateOnly from, DateOnly to) =>
            await reports.GetPeriodReportAsync(from, to));
        reports.MapGet("/sales-by-day", async (ReportService reports, DateOnly from, DateOnly to) =>
            await reports.GetSalesByDayAsync(from, to));
        reports.MapGet("/finance", async (ReportService reports, DateOnly? from, DateOnly? to) =>
            await reports.GetFinanceSummaryAsync(from, to));
    }
}
