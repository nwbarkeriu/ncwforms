using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using JobCompare.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
// Email service configuration for JobCompare EmailService
builder.Services.Configure<JobCompare.Services.EmailSettings>(
    builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<JobCompare.Services.EmailService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}



app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
