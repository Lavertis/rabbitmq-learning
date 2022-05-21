using Common.BackgroundServices;
using Common.Config;
using Common.Controllers;
using Common.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var assembly = typeof(BankController).Assembly;
builder.Services.AddControllers().AddApplicationPart(assembly);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<MyConfig>(builder.Configuration.GetSection("MyConfig"));
builder.Services.AddSingleton<TransferService>();
builder.Services.AddSingleton<AccountService>();
builder.Services.AddHostedService<OutgoingTransferAckService>();
builder.Services.AddHostedService<IncomingTransferService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();