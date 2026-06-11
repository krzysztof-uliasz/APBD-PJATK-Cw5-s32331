using Microsoft.EntityFrameworkCore;
using HospitalApi.Infrastructure;
using HospitalApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddOpenApi();

// Register DbContext
builder.Services.AddDbContext<HospitalContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Register services (add after you create them in Step 8)
builder.Services.AddScoped<IPatientService, PatientService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(opt => opt.SwaggerEndpoint("/openapi/v1.json", "Hospital API V1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();