using Microsoft.EntityFrameworkCore;
using Laboratorio.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    }); // <-- ¡Esta es la clave para que lea el TenantsController!
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Puente de conexión a PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configurar el entorno de desarrollo y Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers(); // <-- ¡Esta línea mapea las rutas de los controladores!

// ¡Aquí construiremos los verdaderos endpoints de BitCore más adelante!

app.Run();