using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using proyecto_inkamanu_net.Data;
using proyecto_inkamanu_net.Models.DTO;
using proyecto_inkamanu_net.Models.Validator;
/*LIBRERIAS PARA LA PAGINACION DE LISTAR PRODUCTOS */
using X.PagedList;

/*LIBRERIAS PARA SUBR IMAGENES */
using Firebase.Auth;
using Firebase.Storage;
using System.Web.WebPages;

/*LIBRERIAS NECESARIAS PARA EXPORTAR */
using DinkToPdf;
using DinkToPdf.Contracts;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using OfficeOpenXml.Table;
using proyecto_inkamanu_net.Models;
using proyecto_inkamanu_net.Models.Entity;
namespace proyecto_ecommerce_deportivo_net.Controllers
{
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;


        // Objeto para la exportación
        private readonly IConverter _converter;

        public AdminController(ILogger<AdminController> logger, ApplicationDbContext context, IConverter converter)
        {
            _logger = logger;
            _context = context;
            ModelState.Clear();

            _converter = converter; // PARA EXPORTAR
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

        [HttpGet]
        public IActionResult AgregarProducto()
        {
            return View("AgregarProducto");
        }


        [HttpPost]
        public async Task<IActionResult> GuardarProducto(ProductoDTO productoDTO)
        {
            ProductoValidator validator = new ProductoValidator();
            ValidationResult result = validator.Validate(productoDTO);

            // Si se cumple con la validacion
            if (result.IsValid)
            {
                Producto producto = new();

                producto.Nombre = productoDTO.Nombre;
                producto.Descripcion = productoDTO.Descripcion;
                string urlImagen = await SubirStorage(productoDTO.Imagen.OpenReadStream(), productoDTO.Imagen.FileName);
                producto.Imagen = urlImagen;
                producto.Precio = productoDTO.Precio;
                producto.Stock = productoDTO.Stock;
                producto.GraduacionAlcoholica = productoDTO.GraduacionAlcoholica;
                producto.TipoCerveza = productoDTO.TipoCerveza;
                producto.Volumen = productoDTO.Volumen;
                producto.TipoEnvase = productoDTO.TipoEnvase;
                producto.fechaCreacion = DateTime.Now.ToUniversalTime(); ;
                producto.fechaActualizacion = null;

                TempData["MessageRegistrandoProducto"] = "Se Registraron exitosamente los datos.";

                _context.DataProducto.Add(producto);
                _context.SaveChanges();

                return RedirectToAction("AgregarProducto");
            }
            else
            {
                foreach (var failure in result.Errors)
                {
                    ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
                }
            }
            // Si hay campos que no cumplen con la validacion
            return View("AgregarProducto");
        }

        public async Task<string> SubirStorage(Stream archivo, string nombre)
        {

            //INGRESA AQUÍ TUS PROPIAS CREDENCIALES
            string email = "athletix@gmail.com";
            string clave = "codigo123";
            string ruta = "athletix-app.appspot.com";
            string api_key = "AIzaSyAg3WiFrCupnLMrv0CHs8XxJIodiX52XqU";

            var auth = new FirebaseAuthProvider(new FirebaseConfig(api_key));
            var a = await auth.SignInWithEmailAndPasswordAsync(email, clave);

            var cancellation = new CancellationTokenSource();

            var task = new FirebaseStorage(
                ruta,
                new FirebaseStorageOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(a.FirebaseToken),
                    ThrowOnCancel = true
                })
                .Child("AthletiX")
                .Child(nombre)
                .PutAsync(archivo, cancellation.Token);


            var downloadURL = await task;

            return downloadURL;
        }

        public ActionResult ListaDeProductos(int? page)
        {
            int pageNumber = (page ?? 1); // Si no se especifica la página, asume la página 1
            int pageSize = 7; // maximo 7 productos por pagina


            pageNumber = Math.Max(pageNumber, 1);// Con esto se asegura de que pageNumber nunca sea menor que 1

            IPagedList listaPaginada = _context.DataProducto.ToPagedList(pageNumber, pageSize);

            return View("ListaDeProductos", listaPaginada);
        }

        public async Task<ActionResult> EditarProducto(int? id)
        {

            Producto? producto = await _context.DataProducto.FindAsync(id);

            if (producto == null)
            {
                return NotFound();
            }

            ProductoDTO productoDTO = new ProductoDTO();
            productoDTO.Id = producto.id;
            productoDTO.Nombre = producto.Nombre;
            productoDTO.Descripcion = producto.Descripcion;
            productoDTO.Precio = producto.Precio;
            productoDTO.Stock = producto.Stock;
            productoDTO.GraduacionAlcoholica = producto.GraduacionAlcoholica;
            productoDTO.TipoCerveza = producto.TipoCerveza;
            productoDTO.Volumen = producto.Volumen;
            productoDTO.TipoEnvase = producto.TipoEnvase;

            return View("EditarProducto", productoDTO);

        }

        [HttpPost]
        public async Task<ActionResult> GuardarProductoEditado(int id, ProductoDTO productoDTO)
        {
            ProductoValidator validator = new ProductoValidator();
            ValidationResult result = validator.Validate(productoDTO);
            System.Console.Write(productoDTO.Nombre);
            if (result.IsValid)
            {
                Producto? producto = _context.DataProducto.Find(id);

                producto.Nombre = productoDTO.Nombre;
                producto.Descripcion = productoDTO.Descripcion;
                producto.fechaActualizacion = DateTime.Now.ToUniversalTime();
                producto.Precio = productoDTO.Precio;
                producto.Stock = productoDTO.Stock;
                producto.GraduacionAlcoholica = productoDTO.GraduacionAlcoholica;
                producto.TipoCerveza = productoDTO.TipoCerveza;
                producto.Volumen = productoDTO.Volumen;
                producto.TipoEnvase = productoDTO.TipoEnvase;

                if (productoDTO.Imagen != null)
                {
                    string urlImagen = await SubirStorage(productoDTO.Imagen.OpenReadStream(), productoDTO.Imagen.FileName);
                    producto.Imagen = urlImagen;
                }

                TempData["MessageActualizandoProducto"] = "Se Actualizaron exitosamente los datos.";
                _context.DataProducto.Update(producto);
                _context.SaveChanges();

                return RedirectToAction("EditarProducto", new { id = producto.id });

            }
            return View("EditarProducto");
        }


        /* metodos para exportar en pdf y excel desde aqui para abajo */
        public IActionResult ExportarProductosEnPDF()
        {
            try
            {
                var products = _context.DataProducto.ToList();
                var html = @"
            <html>
                <head>
                <meta charset='UTF-8'>
                    <style>
                        table {
                            width: 100%;
                            border-collapse: collapse;
                        }
                        th, td {
                            border: 1px solid black;
                            padding: 8px;
                            text-align: left;
                        }
                        th {
                            background-color: #f2f2f2;
                        }
                        img.logo {
                            position: absolute;
                            top: 0;
                            right: 0;
                            border-radius:50%;
                            height:3.3rem;
                            width:3.3rem;
                        }

                        h1 {
                            color: #40E0D0; /* Color celeste */
                        }
                    </style>
                </head>
                <body>
                    <img src='https://firebasestorage.googleapis.com/v0/b/proyectos-cb445.appspot.com/o/img_logo_inkamanu.jpeg?alt=media&token=3b834c39-f2ee-4555-8770-4f5a2bc88066&_gl=1*gxgr9z*_ga*MTcyOTkyMjIwMS4xNjk2NDU2NzU2*_ga_CW55HF8NVT*MTY5NjQ1Njc1NS4xLjEuMTY5NjQ1NzkyMy40OC4wLjA.' alt='Logo' width='100' class='logo'/>
                    <h1>Reporte de Productos</h1>
                    <table>
                        <tr>
                            <th>ID</th>
                            <th>Nombre</th>
                            <th>Descripción</th>
                            <th>Imagen</th>
                            <th>Precio</th>
                            <th>Stock</th>
                            <th>Fecha de Creación</th>
                            <th>Fecha de Actualización</th>
                        </tr>";

                foreach (var product in products)
                {
                    // Verifica si la URL de la imagen es válida.
                    var imageUrl = Uri.IsWellFormedUriString(product.Imagen, UriKind.Absolute) ? product.Imagen : "URL de imagen no válida";

                    html += $@"
                <tr>
                    <td>{product.id}</td>
                    <td>{product.Nombre}</td>
                    <td>{product.Descripcion}</td>
                    <td><img src='{imageUrl}' alt='Imagen del producto' width='40' onerror='this.onerror=null;this.src='https://firebasestorage.googleapis.com/v0/b/athletix-app.appspot.com/o/AthletiX%2FWhatsApp%20Image%202023-09-29%20at%206.58.13%20PM%20%281%29.jpeg?alt=media&token=2a97c125-f96c-413c-ba2b-c0c010cb1139';'/></td>
                    <td>{product.Precio}</td>
                    <td>{product.Stock}</td>
                    <td>{product.fechaCreacion}</td>
                    <td>{product.fechaActualizacion}</td>
                </tr>";
                }

                html += @"
                    </table>
                </body>
            </html>";

                var globalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                };
                var objectSettings = new ObjectSettings { HtmlContent = html };
                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = globalSettings,
                    Objects = { objectSettings }
                };
                var file = _converter.Convert(pdf);

                return File(file, "application/pdf", "Productos.pdf");

            }
            catch (Exception ex)
            {
                // Loguear el error para obtener más detalles
                _logger.LogError(ex, "Error al exportar productos a PDF");
                // Retornar un mensaje de error al usuario
                return StatusCode(500, "Ocurrió un error al exportar los productos a PDF. Por favor, inténtelo de nuevo más tarde.");
            }
        }


        public IActionResult ExportarProductosEnExcel()
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Productos");

                // Agregando un título arriba de la tabla
                worksheet.Cells[1, 1].Value = "Reporte de Productos";
                worksheet.Cells[1, 1].Style.Font.Size = 20;
                worksheet.Cells[1, 1].Style.Font.Bold = true;

                // Cargar los datos en la fila 3 para dejar espacio para el título de Reporte de Productos
                worksheet.Cells[3, 1].LoadFromCollection(_context.DataProducto.ToList(), true);

                // Dar formato a la tabla Reporte de Productos
                var dataRange = worksheet.Cells[2, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column];
                var table = worksheet.Tables.Add(dataRange, "Productos");
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Light6;

                // Estilo para los encabezados de las columnas 
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Font.Bold = true;
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);

                // Ajustar el ancho de las columnas automáticamente
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Productos.xlsx");
            }
            catch (Exception ex)
            {
                // Loguear el error para obtener más detalles
                _logger.LogError(ex, "Error al exportar productos a Excel");
                // Retornar un mensaje de error al usuario
                return StatusCode(500, "Ocurrió un error al exportar los productos a Excel. Por favor, inténtelo de nuevo más tarde.");
            }
        }

        /* Para exportar individualmente los productos */
        public IActionResult ExportarUnSoloProductoEnPDF(int id)
        {
            try
            {

                var product = _context.DataProducto.Find(id);
                if (product == null)
                {
                    return NotFound();
                }

                var html = $@"
            <html>
                <head>
                <meta charset='UTF-8'>
                    <style>
                        table {{
                            width: 100%;
                            border-collapse: collapse;
                        }}
                        th, td {{
                            border: 1px solid black;
                            padding: 8px;
                            text-align: left;
                        }}
                        th {{
                            background-color: #f2f2f2;
                        }}
                        img.logo {{
                            position: absolute;
                            top: 0;
                            right: 0;
                            border-radius:50%;
                            height:3.3rem;
                            width:3.3rem;
                        }}

                        h1 {{
                            color: #40E0D0; /* Color celeste */
                        }}
                    </style>
                </head>
                <body>
                    <img src='https://firebasestorage.googleapis.com/v0/b/proyectos-cb445.appspot.com/o/img_logo_inkamanu.jpeg?alt=media&token=3b834c39-f2ee-4555-8770-4f5a2bc88066&_gl=1*gxgr9z*_ga*MTcyOTkyMjIwMS4xNjk2NDU2NzU2*_ga_CW55HF8NVT*MTY5NjQ1Njc1NS4xLjEuMTY5NjQ1NzkyMy40OC4wLjA.' alt='Logo' width='100' class='logo'/>
                    <h1>Reporte de Producto {id}</h1>
                    <table>
                        <tr>
                            <th>ID</th>
                            <th>Nombre</th>
                            <th>Descripción</th>
                            <th>Imagen</th>
                            <th>Precio</th>
                            <th>Stock</th>
                            <th>Fecha de Creación</th>
                            <th>Fecha de Actualización</th>
                        </tr>";


                // Verifica si la URL de la imagen es válida.
                var imageUrl = Uri.IsWellFormedUriString(product.Imagen, UriKind.Absolute) ? product.Imagen : "URL de imagen no válida";

                html += $@"
                <tr>
                    <td>{product.id}</td>
                    <td>{product.Nombre}</td>
                    <td>{product.Descripcion}</td>
                    <td><img src='{imageUrl}' alt='Imagen del producto' width='40' onerror='this.onerror=null;this.src='https://firebasestorage.googleapis.com/v0/b/athletix-app.appspot.com/o/AthletiX%2FWhatsApp%20Image%202023-09-29%20at%206.58.13%20PM%20%281%29.jpeg?alt=media&token=2a97c125-f96c-413c-ba2b-c0c010cb1139';'/></td>
                    <td>{product.Precio}</td>
                    <td>{product.Stock}</td>
                    <td>{product.fechaCreacion}</td>
                    <td>{product.fechaActualizacion}</td>
                </tr>";


                html += @"
                    </table>
                </body>
            </html>";

                var globalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                };
                var objectSettings = new ObjectSettings
                {
                    HtmlContent = html
                };
                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = globalSettings,
                    Objects = { objectSettings }
                };
                var file = _converter.Convert(pdf);
                return File(file, "application/pdf", $"Producto_{id}.pdf");

            }
            catch (Exception ex)
            {
                // Loguear el error para obtener más detalles
                _logger.LogError(ex, $"Error al exportar el producto {id} a PDF");
                // Retornar un mensaje de error al usuario
                return StatusCode(500, $"Ocurrió un error al exportar el producto {id} a PDF. Por favor, inténtelo de nuevo más tarde.");
            }
        }



        public IActionResult ExportarUnSoloProductoEnExcel(int id)
        {
            try
            {
                var product = _context.DataProducto.Find(id);
                if (product == null)
                {
                    return NotFound();
                }

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Producto");

                // Agregando un título arriba de la tabla
                worksheet.Cells[1, 1].Value = $"Reporte del Producto {id}";
                worksheet.Cells[1, 1].Style.Font.Size = 20;
                worksheet.Cells[1, 1].Style.Font.Bold = true;

                // Cargar los datos en la fila 3 para dejar espacio para el título
                var productList = new List<Producto> { product };
                worksheet.Cells[3, 1].LoadFromCollection(productList, true);

                // Dar formato a la tabla Reporte de Productos
                var dataRange = worksheet.Cells[2, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column];
                var table = worksheet.Tables.Add(dataRange, "Producto");
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Light6;

                // Estilo para los encabezados de las columnas 
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Font.Bold = true;
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);

                // Ajustar el ancho de las columnas automáticamente
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Producto_{id}.xlsx");
            }
            catch (Exception ex)
            {
                // Loguear el error para obtener más detalles
                _logger.LogError(ex, $"Error al exportar el producto {id} a Excel");
                // Retornar un mensaje de error al usuario
                return StatusCode(500, $"Ocurrió un error al exportar el producto {id} a Excel. Por favor, inténtelo de nuevo más tarde.");
            }
        }

        /* Hasta aqui son los metodos para exportar */

        /* metodo para eliminar un producto */
        [HttpPost]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            var producto = await _context.DataProducto.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }
            _context.DataProducto.Remove(producto);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ListaDeProductos));
        }

        /* metodo para buscar */

        public async Task<IActionResult> BuscarProducto(string query)
        {
            // Declara la variable productosPagedList una sola vez aquí
            IPagedList<Producto> productosPagedList;

            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    var todosLosProductos = await _context.DataProducto.ToListAsync();
                    productosPagedList = todosLosProductos.ToPagedList(1, todosLosProductos.Count);
                }
                else
                {
                    query = query.ToUpper();
                    var productos = await _context.DataProducto
                        .Where(p => p.Nombre.ToUpper().Contains(query))
                        .ToListAsync();

                    if (!productos.Any())
                    {
                        TempData["MessageDeRespuesta"] = "No se encontraron productos que coincidan con la búsqueda.";
                        productosPagedList = new PagedList<Producto>(new List<Producto>(), 1, 1);
                    }
                    else
                    {
                        productosPagedList = productos.ToPagedList(1, productos.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["MessageDeRespuesta"] = "Ocurrió un error al buscar productos. Por favor, inténtalo de nuevo más tarde.";
                productosPagedList = new PagedList<Producto>(new List<Producto>(), 1, 1);
            }

            // Retorna la vista con productosPagedList, que siempre tendrá un valor asignado.
            return View("ListaDeProductos", productosPagedList);
        }


        // public IActionResult ListaDeUsuarios()
        // {
        //     var listaDeUsuarios = _context.Users.ToList();

        //     Console.Write(listaDeUsuarios + "HOLAAAAAAAA");

        //     return View("ListaDeUsuarios", listaDeUsuarios);
        // }

        public ActionResult ListaDeUsuarios(int? page)
        {
            int pageNumber = (page ?? 1); // Si no se especifica la página, asume la página 1
            int pageSize = 7; // maximo 7 usuarios por pagina


            pageNumber = Math.Max(pageNumber, 1);// Con esto se asegura de que pageNumber nunca sea menor que 1

            IPagedList listaPaginada = _context.Users.ToPagedList(pageNumber, pageSize);

            return View("ListaDeUsuarios", listaPaginada);
        }

        // public IActionResult EditarUsuario() {

        // }

        [HttpPost]
        public async Task<IActionResult> EliminarUsuario(string id)
        {
            var usuario = await _context.Users.FindAsync(id);

            if (usuario != null)
            {
                _context.Users.Remove(usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ListaDeUsuarios));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<ActionResult> EditarUsuario(string? id)
        {

            ApplicationUser? usuarioEditar = await _context.Users.FindAsync(id);

            if (usuarioEditar == null)
            {
                Console.Write("No se encontro");
                return NotFound();
            }

            UsuarioDTO usuarioEditarDTO = new UsuarioDTO();
            usuarioEditarDTO.Id = usuarioEditar.Id;
            usuarioEditarDTO.Nombres = usuarioEditar.Nombres;
            usuarioEditarDTO.ApellidoPaterno = usuarioEditar.ApellidoPat;
            usuarioEditarDTO.ApellidoMaterno = usuarioEditar.ApellidoMat;
            usuarioEditarDTO.Email = usuarioEditar.Email;
            usuarioEditarDTO.Dni = usuarioEditar.Dni;
            usuarioEditarDTO.Celular = usuarioEditar.Celular;
            usuarioEditarDTO.Genero = usuarioEditar.Genero;
            return View("EditarUsuario", usuarioEditarDTO);

        }

        [HttpPost]
        public async Task<IActionResult> GuardarUsuarioEditado(UsuarioDTO usuarioDTO)
        {

            UsuarioValidator validator = new UsuarioValidator();
            ValidationResult result = validator.Validate(usuarioDTO);

            if (result.IsValid)
            {
                ApplicationUser? user = await _context.Users.FindAsync(usuarioDTO.Id);
                user.Nombres = usuarioDTO.Nombres;
                user.ApellidoPat = usuarioDTO.ApellidoPaterno;
                user.ApellidoMat = usuarioDTO.ApellidoMaterno;
                user.Email = usuarioDTO.Email;
                user.Dni = usuarioDTO.Dni;
                user.Celular = usuarioDTO.Celular;
                user.Genero = usuarioDTO.Genero;
                user.fechaDeActualizacion = DateTime.Now.ToUniversalTime();

                TempData["MessageActualizandoUsuario"] = "Se Actualizaron exitosamente los datos.";
                _context.Users.Update(user);
                _context.SaveChanges();

                return RedirectToAction("EditarUsuario", new { id = usuarioDTO.Id });
            }

            foreach (var failure in result.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }
            return View("EditarUsuario", usuarioDTO);

        }

        public async Task<IActionResult> buscarUsuario(string query)
        {


            IPagedList usuariosPagedList;

            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    var todosLosUsuarios = await _context.Users.ToListAsync();
                    usuariosPagedList = _context.Users.ToPagedList(1, todosLosUsuarios.Count);
                }
                else
                {
                    query = query.ToUpper();
                    var usuarios = await _context.Users
                        .Where(p => p.Nombres.ToUpper().Contains(query) || p.Email.ToUpper().Contains(query))
                        .ToListAsync();

                    if (!usuarios.Any())
                    {
                        TempData["MessageDeRespuesta"] = "No se encontraron productos que coincidan con la búsqueda.";
                        usuariosPagedList = new PagedList<ApplicationUser>(new List<ApplicationUser>(), 1, 1);
                    }
                    else
                    {
                        usuariosPagedList = usuarios.ToPagedList(1, usuarios.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["MessageDeRespuesta"] = "Ocurrió un error al buscar productos. Por favor, inténtalo de nuevo más tarde.";
                usuariosPagedList = new PagedList<ApplicationUser>(new List<ApplicationUser>(), 1, 1);
            }

            // Retorna la vista con productosPagedList, que siempre tendrá un valor asignado.
            return View("ListaDeUsuarios", usuariosPagedList);


        }


        public IActionResult ExportarUsuariosEnPDF()
        {
            try
            {
                var users = _context.Users.ToList();
                var html = @"
            <html>
                <head>
                <meta charset='UTF-8'>
                    <style>
                        table {
                            width: 100%;
                            border-collapse: collapse;
                        }
                        th, td {
                            border: 1px solid black;
                            padding: 8px;
                            text-align: left;
                        }
                        th {
                            background-color: #f2f2f2;
                        }
                        img.logo {
                            position: absolute;
                            top: 0;
                            right: 0;
                            border-radius:50%;
                            height:3.3rem;
                            width:3.3rem;
                        }

                        h1 {
                            color: #40E0D0; /* Color celeste */
                        }
                    </style>
                </head>
                <body>
                    <img src='https://firebasestorage.googleapis.com/v0/b/proyectos-cb445.appspot.com/o/img_logo_inkamanu.jpeg?alt=media&token=3b834c39-f2ee-4555-8770-4f5a2bc88066&_gl=1*gxgr9z*_ga*MTcyOTkyMjIwMS4xNjk2NDU2NzU2*_ga_CW55HF8NVT*MTY5NjQ1Njc1NS4xLjEuMTY5NjQ1NzkyMy40OC4wLjA.' alt='Logo' width='100' class='logo'/>
                    <h1>Reporte de Usuarios</h1>
                    <table>
                        <tr>
                            <th>ID</th>
                            <th>Nombre</th>
                            <th>Apellido Paterno</th>
                            <th>Apellido Materno</th>
                            <th>Email</th>
                            <th>Fecha De Registro</th>
                            <th>Fecha Ultima Actualización</th>
                            <th>Celular</th>
                            <th>Rol</th>
                        </tr>";

                foreach (var user in users)
                {

                    html += $@"
                <tr>
                    <td>{user.Id}</td>
                    <td>{user.Nombres}</td>
                    <td>{user.ApellidoPat}</td>
                    <td>{user.ApellidoMat}</td>
                    <td>{user.Email}</td>
                    <td>{user.fechaDeRegistro}</td>
                    <td>{user.fechaDeActualizacion}</td>
                    <td>{user.Celular}</td>
                    <td>{user.Rol}</td>
                </tr>";
                }

                html += @"
                    </table>
                </body>
            </html>";

                var globalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                };
                var objectSettings = new ObjectSettings { HtmlContent = html };
                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = globalSettings,
                    Objects = { objectSettings }
                };
                var file = _converter.Convert(pdf);

                return File(file, "application/pdf", "Usuarios.pdf");

            }
            catch (Exception ex)
            {
                // Loguear el error para obtener más detalles
                _logger.LogError(ex, "Error al exportar Usuarios a PDF");
                // Retornar un mensaje de error al usuario
                return StatusCode(500, "Ocurrió un error al exportar los Usuarios a PDF. Por favor, inténtelo de nuevo más tarde.");
            }
        }


        public IActionResult ExportarUsuariosEnExcel()
        {
            try 
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Usuarios");

                // Agregando un título arriba de la tabla
                worksheet.Cells[1, 1].Value = "Reporte de Usuarios";
                worksheet.Cells[1, 1].Style.Font.Size = 20;
                worksheet.Cells[1, 1].Style.Font.Bold = true;

                // Cargar los datos en la fila 3 para dejar espacio para el título de Reporte de Usuarios
                worksheet.Cells[3, 1].LoadFromCollection(_context.Users.ToList(), true);

                // Dar formato a la tabla Reporte de Usuarios
                var dataRange = worksheet.Cells[2, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column];
                var table = worksheet.Tables.Add(dataRange, "Usuarios");
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Light6;

                // Estilo para los encabezados de las columnas 
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Font.Bold = true;
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);

                // Ajustar el ancho de las columnas automáticamente
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Usuarios.xlsx");
            }
            catch (Exception ex)
            {
                // Loguear el error para obtener más detalles
                _logger.LogError(ex, "Error al exportar Usuarios a Excel");
                // Retornar un mensaje de error al usuario
                return StatusCode(500, "Ocurrió un error al exportar los Usuarios a Excel. Por favor, inténtelo de nuevo más tarde.");
            }
        }

        public async Task<IActionResult> DetalleProducto(int? id)
        {
            Producto objProduct = await _context.DataProducto.FindAsync(id);
            if (objProduct == null)
            {
                return NotFound();
            }
            return View(objProduct);
        }

    }

}