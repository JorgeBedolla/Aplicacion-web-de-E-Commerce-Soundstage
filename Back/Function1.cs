using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Net.Http;
using System.Net;
using System.Data;

using System.Security.Cryptography;
using System.Text;

using System.Text.Json.Serialization;

using System.Collections.Generic;

using static System.Net.Mime.MediaTypeNames;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using Image = System.Net.Mime.MediaTypeNames.Image;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Diagnostics.Metrics;

namespace SoundStageProject
{
    public static class Servicio
    {

        static string connectionString = null;
        static string serverKey = null;

        static Servicio()
        {
            try
            {
                string server = Environment.GetEnvironmentVariable("Server");
                string UserID = Environment.GetEnvironmentVariable("UserID");
                string Password = Environment.GetEnvironmentVariable("Password");
                string Database = Environment.GetEnvironmentVariable("Database");

                serverKey = Environment.GetEnvironmentVariable("llaveServidor");
                connectionString = "Server=" + server + ";UserID=" + UserID + ";Password=" + Password + ";Database=" + Database + ";SslMode=Preferred;";
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        [FunctionName("crear_usuario")]
        public static async Task<IActionResult> CrearUser(
         [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
         ILogger log)
        {
            try
            {
                string requestBody = await req.ReadAsStringAsync();
                ParamCrearUsuario userRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamCrearUsuario>(requestBody);

                if (userRequest == null)
                {
                    return new BadRequestObjectResult("La solicitud no contiene un cuerpo JSON válido.");
                }

                string hashedPassword = HashPassword(userRequest.Contrasena);

                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using MySqlTransaction transaction = await connection.BeginTransactionAsync();
              
                try
                {
                    // Insertar el nuevo usuario en la tabla
                    MySqlCommand command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandType = CommandType.Text;
                    command.CommandText = "INSERT INTO usuarios (usuario, contrasena, nombre, ap_paterno, ap_materno, direccion, correo, tipo_usuario) " +
                        "VALUES (@Usuario, @Contrasena, @Nombre, @ApPaterno, @ApMaterno, @Direccion, @Correo, @TipoUsuario)";
                    command.Parameters.AddWithValue("@Usuario", userRequest.Usuario);
                    command.Parameters.AddWithValue("@Contrasena", hashedPassword);
                    command.Parameters.AddWithValue("@Nombre", userRequest.Nombre);
                    command.Parameters.AddWithValue("@ApPaterno", userRequest.ApPaterno);
                    command.Parameters.AddWithValue("@ApMaterno", userRequest.ApMaterno);
                    command.Parameters.AddWithValue("@Direccion", userRequest.Direccion);
                    command.Parameters.AddWithValue("@Correo", userRequest.Correo);
                    command.Parameters.AddWithValue("@TipoUsuario", userRequest.TipoUsuario);

                    await command.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();

                    return new OkObjectResult("Usuario registrado exitosamente.");
                }
                catch (Exception)
                {

                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }





    [FunctionName("iniciar_sesion")]
        public static IActionResult Iniciar(
          [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
          ILogger log)
        {
            try
            {

                string requestBody = new StreamReader(req.Body).ReadToEnd();
                ParamIniciarSesion parametros = JsonConvert.DeserializeObject<ParamIniciarSesion>(requestBody);

                string usuario = parametros.Usuario;
                string contrasena = parametros.Contrasena;


                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();


                    string query = "SELECT contrasena,id_usuario, tipo_usuario FROM usuarios WHERE usuario = @usuario";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@usuario", usuario);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string hashedPassword = reader.GetString("contrasena");
                                int idUsurio = reader.GetInt32("id_usuario");
                                int tipo_usuario = reader.GetInt32("tipo_usuario");


                                // Verificar la contraseña ingresada es correcta
                                if (BCrypt.Net.BCrypt.Verify(contrasena, hashedPassword))
                                {

                                    string encryptedUserId = Encrypt(idUsurio.ToString(), serverKey);//Encriptamos el ID
                                    string signature = GenerateSignature(encryptedUserId, serverKey);

                                    var cookieOptions = new CookieOptions
                                    {
                                        Expires = DateTime.Now.AddDays(1),
                                        HttpOnly = true,
                                        Secure = true,
                                        SameSite = SameSiteMode.Strict
                                    };

                                    //Comprobamos si es usuario convencional o administrador
                                    if (tipo_usuario > 0)
                                    {

                                        Console.WriteLine("Sesion iniciada como administador");
                                        string encryptedTypeUser = Encrypt(tipo_usuario.ToString(), serverKey);//Encriptamos el tipo de usuario

                                        var responseAdmin = new
                                        {
                                            message = "Sesion iniciada correctamente",
                                            cookie = $"{encryptedUserId}.{encryptedTypeUser}.{signature}",
                                            status = 1
                                        };
                                        return new OkObjectResult(responseAdmin);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Sesion iniciada como cliente");

                                        // Establecer la cookie de sesión
                                        // Agrega la cookie firmada a la respuesta
                                        var responseUser = new
                                        {
                                            message = "Sesion iniciada correctamente",
                                            cookie = $"{encryptedUserId}.{signature}",
                                            status = 0
                                        };

                                        return new OkObjectResult(responseUser);

                                    }
                                }
                            }
                        }
                    }
                }

                // Si las credenciales no son correctas o no se encuentra el usuario en la base de datos

                var responseE = new
                {
                    message = "Usuario y/o contrasena incorrectos"
                };

                return new BadRequestObjectResult(responseE);

            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al iniciar sesión");
                return new StatusCodeResult(500);
            }
        }


    [FunctionName("obtener_instrumentos")]
        public static async Task<IActionResult> obtenerInstumentos(
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
           ILogger log)
        {
            try
            {

                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();


                List<Instrumento> instrumentos = new List<Instrumento>();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "SELECT id_articulo, nombre, descripcion, precio, stock FROM articulos WHERE id_disco IS NULL";

                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {

                            Instrumento instrumento = new Instrumento();
                            instrumento.id_articulo = reader.GetInt32("id_articulo");
                            instrumento.nombre = reader.GetString("nombre");
                            instrumento.descripcion = reader.GetString("descripcion");
                            instrumento.precio = reader.GetInt32("precio");
                            instrumento.stock = reader.GetInt32("stock");
                            instrumento.foto = null;

                            /*
                            if (Convert.IsDBNull(reader["foto"]))
                            {
                                instrumento.foto = null;
                            }
                            else
                            {
                                instrumento.foto = (byte[])reader["foto"];
                            }*/

                            instrumentos.Add(instrumento);
                        }
                    }
                }

                var response = new Dictionary<int, object>();
                foreach (var instrumento in instrumentos)
                {
                    var instrumentoData = new Dictionary<string, object>
                    {   { "nombre", instrumento.nombre },
                        { "descripcion", instrumento.descripcion },
                        { "precio", instrumento.precio },
                        { "stock", instrumento.stock },
                        { "id_disco", null},
                        {"foto",instrumento.foto }
                    };

                    response.Add(instrumento.id_articulo, instrumentoData);
                }

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

    [FunctionName("obtener_discos")]
        public static async Task<IActionResult> obtenerDiscos(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                List<Disco> discos = new List<Disco>();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "SELECT articulos.id_articulo, articulos.nombre, discos.autor, discos.ano_disco, discos.duracion, articulos.descripcion, articulos.precio, articulos.stock, articulos.id_disco " +
                                          "FROM articulos " +
                                          "INNER JOIN discos ON articulos.id_disco = discos.id_disco " +
                                          "WHERE articulos.id_disco IS NOT NULL";

                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {

                            Disco disco = new Disco();
                            disco.id_articulo = reader.GetInt32("id_articulo");
                            disco.nombre = reader.GetString("nombre");
                            disco.autor = reader.GetString("autor");
                            disco.ano_disco = reader.GetInt32("ano_disco");
                            disco.duracion = reader.GetString("duracion");
                            disco.descripcion = reader.GetString("descripcion");
                            disco.precio = reader.GetInt32("precio");
                            disco.stock = reader.GetInt32("stock");
                            disco.id_disco = reader.GetInt32("id_disco");

                            disco.foto = null;

                            /*
                            if (Convert.IsDBNull(reader["foto"]))
                            {
                                disco.foto = null;
                            }
                            else
                            {
                                disco.foto = (byte[])reader["foto"];
                            }*/



                            discos.Add(disco);
                        }
                    }
                }

                var response = new Dictionary<int, object>();
                foreach (var disco in discos)
                {
                    var discoData = new Dictionary<string, object>
                    {
                        { "nombre", disco.nombre },
                        { "autor", disco.autor },
                        { "ano_disco", disco.ano_disco },
                        { "duracion", disco.duracion },
                        { "descripcion", disco.descripcion },
                        { "precio", disco.precio },
                        { "stock", disco.stock },
                        { "foto", disco.foto },
                        { "id_disco", disco.id_disco}
                    };

                    response.Add(disco.id_articulo, discoData);
                }

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }



     [FunctionName("obtener_instrumentos_basico")]
        public static async Task<IActionResult> obtenerInstumentosBasico(
               [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
               ILogger log)
        {
            try
            {

                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();


                List<InstrumentoPanel> instrumentos = new List<InstrumentoPanel>();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "SELECT nombre, precio, foto FROM articulos WHERE id_disco IS NULL";

                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {

                            InstrumentoPanel instrumento = new InstrumentoPanel();
                            instrumento.nombre = reader.GetString("nombre");
                            instrumento.precio = reader.GetInt32("precio");
                            if (Convert.IsDBNull(reader["foto"]))
                            {
                                instrumento.foto = null;
                            }
                            else
                            {
                                byte[] reducedFoto = ReducirResolucion((byte[])reader["foto"], 120, 120, 80);
                                byte[] compressedFoto = ComprimirImagen(reducedFoto, 70);
                                instrumento.foto = compressedFoto;
                            }

                            instrumentos.Add(instrumento);
                        }
                    }
                }

                var response = new Dictionary<int, object>();
                int contador = 0;
                foreach (var instrumento in instrumentos)
                {
                    var instrumentoData = new Dictionary<string, object>
                    {   { "nombre", instrumento.nombre },
                        { "precio", instrumento.precio },
                        {"foto",instrumento.foto }
                    };

                    response.Add(contador, instrumentoData);
                    contador++;
                }

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [FunctionName("obtener_discos_basico")]
        public static async Task<IActionResult> obtenerDiscosBasico(
                [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
                ILogger log)
        {
            try
            {
                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                List<DiscoPanel> discos = new List<DiscoPanel>();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "SELECT articulos.nombre, articulos.precio, articulos.foto " +
                                          "FROM articulos " +
                                          "INNER JOIN discos ON articulos.id_disco = discos.id_disco " +
                                          "WHERE articulos.id_disco IS NOT NULL";

                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {

                            DiscoPanel disco = new DiscoPanel();
                            disco.nombre = reader.GetString("nombre");
                            disco.precio = reader.GetInt32("precio");

                            if (Convert.IsDBNull(reader["foto"]))
                            {
                                disco.foto = null;
                            }
                            else
                            {
                                byte[] reducedFoto = ReducirResolucion((byte[])reader["foto"], 120, 120, 80);
                                byte[] compressedFoto = ComprimirImagen(reducedFoto, 70);
                                disco.foto = compressedFoto;


                            }



                            discos.Add(disco);
                        }
                    }
                }

                var response = new Dictionary<int, object>();
                int contador = 0;
                foreach (var disco in discos)
                {
                    var discoData = new Dictionary<string, object>
                    {
                        { "nombre", disco.nombre },
                        { "precio", disco.precio },
                        { "foto", disco.foto },
                    };

                    response.Add(contador, discoData);
                    contador++;
                }

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
        //Devuelve el articulo con todo y IMAGEN EN MAXIMA RESOLUCION
        [FunctionName("obtener_articulo")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {

                string requestBody = await new System.IO.StreamReader(req.Body).ReadToEndAsync();
                ParamBusquedaArticulo busquedaRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamBusquedaArticulo>(requestBody);


                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();


                List<Dictionary<string, object>> resultados = new List<Dictionary<string, object>>();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "SELECT articulos.id_articulo, articulos.nombre, articulos.descripcion, articulos.precio, articulos.stock, articulos.foto, " +
                                          "discos.autor, discos.ano_disco, discos.duracion " +
                                          "FROM articulos " +
                                          "LEFT JOIN discos ON articulos.id_disco = discos.id_disco " +
                                          "WHERE articulos.nombre LIKE @Busqueda OR articulos.descripcion LIKE @Busqueda";

                    command.Parameters.AddWithValue("@Busqueda", "%" + busquedaRequest.busqueda + "%");

                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            Dictionary<string, object> resultado = new Dictionary<string, object>();
                            int id_articulo = reader.GetInt32("id_articulo");
                            string nombre = reader.GetString("nombre");
                            string descripcion = reader.GetString("descripcion");
                            int precio = reader.GetInt32("precio");
                            int stock = reader.GetInt32("stock");
                            byte[] foto;

                            if (Convert.IsDBNull(reader["foto"]))
                            {
                                foto = null;
                            }
                            else
                            {
                                foto = (byte[])reader["foto"];
                            }

                            int id_disco = 0;

                            if (!reader.IsDBNull(reader.GetOrdinal("autor")))
                            {
                                string autor = reader.GetString("autor");
                                int ano_disco = reader.GetInt32("ano_disco");
                                string duracion = reader.GetString("duracion");
                                id_disco = 1;

                                resultado.Add("autor", autor);
                                resultado.Add("ano_disco", ano_disco);
                                resultado.Add("duracion", duracion);

                            }

                            resultado.Add("descripcion", descripcion);
                            resultado.Add("precio", precio);
                            resultado.Add("stock", stock);
                            resultado.Add("id_disco", id_disco);
                            resultado.Add("foto", foto);
                            resultado.Add("id_articulo", id_articulo);

                            resultados.Add(new Dictionary<string, object> { { nombre, resultado } });
                        }
                    }
                }

                return new OkObjectResult(resultados);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        //Buscar Articulo reduce la resolucion de imagen
        [FunctionName("buscar_articulo")]
        public static async Task<IActionResult> buscarArticulo(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
             
                string requestBody = await new System.IO.StreamReader(req.Body).ReadToEndAsync();
                ParamBusquedaArticulo busquedaRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamBusquedaArticulo>(requestBody);

                
                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                
                List<Dictionary<string, object>> resultados = new List<Dictionary<string, object>>();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "SELECT articulos.id_articulo, articulos.nombre, articulos.descripcion, articulos.precio, articulos.stock, articulos.foto, " +
                                          "discos.autor, discos.ano_disco, discos.duracion " +
                                          "FROM articulos " +
                                          "LEFT JOIN discos ON articulos.id_disco = discos.id_disco " +
                                          "WHERE articulos.nombre LIKE @Busqueda OR articulos.descripcion LIKE @Busqueda";

                    command.Parameters.AddWithValue("@Busqueda", "%" + busquedaRequest.busqueda + "%");

                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            Dictionary<string, object> resultado = new Dictionary<string, object>();
                            int id_articulo = reader.GetInt32("id_articulo");
                            string nombre = reader.GetString("nombre");
                            string descripcion = reader.GetString("descripcion");
                            int precio = reader.GetInt32("precio");
                            int stock = reader.GetInt32("stock");
                            byte[] foto;

                             if (Convert.IsDBNull(reader["foto"]))
                            {
                                foto = null;
                            }
                            else
                            {
                                byte[] reducedFoto = ReducirResolucion((byte[])reader["foto"], 120, 120, 80);
                                byte[] compressedFoto = ComprimirImagen(reducedFoto, 70);
                                foto = compressedFoto;
                            }

                            int id_disco = 0;

                            if (!reader.IsDBNull(reader.GetOrdinal("autor")))
                            {
                                string autor = reader.GetString("autor");
                                int ano_disco = reader.GetInt32("ano_disco");
                                string duracion = reader.GetString("duracion");
                                id_disco = 1;

                                resultado.Add("autor", autor);
                                resultado.Add("ano_disco", ano_disco);
                                resultado.Add("duracion", duracion);

                            }

                            resultado.Add("descripcion", descripcion);
                            resultado.Add("precio", precio);
                            resultado.Add("stock", stock);
                            resultado.Add("id_disco", id_disco);
                            resultado.Add("foto", foto);
                            resultado.Add("id_articulo", id_articulo);

                            resultados.Add(new Dictionary<string, object> { { nombre, resultado } });
                        }
                    }
                }

                return new OkObjectResult(resultados);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }


        [FunctionName("buscar_inventario")]
        public static async Task<IActionResult> BuscarInventario(
         [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
         ILogger log)
        {
            try
            {

                string requestBody = await new System.IO.StreamReader(req.Body).ReadToEndAsync();
                ParamBusquedaArticulo busquedaRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamBusquedaArticulo>(requestBody);


                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();


                List<Dictionary<string, object>> resultados = new List<Dictionary<string, object>>();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "SELECT articulos.id_articulo, articulos.nombre, articulos.descripcion, articulos.precio, articulos.stock, " +
                                          "discos.autor, discos.ano_disco, discos.duracion " +
                                          "FROM articulos " +
                                          "LEFT JOIN discos ON articulos.id_disco = discos.id_disco " +
                                          "WHERE articulos.nombre LIKE @Busqueda OR articulos.descripcion LIKE @Busqueda";

                    command.Parameters.AddWithValue("@Busqueda", "%" + busquedaRequest.busqueda + "%");

                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            Dictionary<string, object> resultado = new Dictionary<string, object>();
                            int id_articulo = reader.GetInt32("id_articulo");
                            string nombre = reader.GetString("nombre");
                            string descripcion = reader.GetString("descripcion");
                            int precio = reader.GetInt32("precio");
                            int stock = reader.GetInt32("stock");
                            byte[] foto = null;
                            /*
                            if (Convert.IsDBNull(reader["foto"]))
                            {
                                foto = null;
                            }
                            else
                            {
                                foto = (byte[])reader["foto"];
                            }*/

                            int id_disco = 0;

                            if (!reader.IsDBNull(reader.GetOrdinal("autor")))
                            {
                                string autor = reader.GetString("autor");
                                int ano_disco = reader.GetInt32("ano_disco");
                                string duracion = reader.GetString("duracion");
                                id_disco = 1;

                                resultado.Add("autor", autor);
                                resultado.Add("ano_disco", ano_disco);
                                resultado.Add("duracion", duracion);

                            }

                            resultado.Add("descripcion", descripcion);
                            resultado.Add("precio", precio);
                            resultado.Add("stock", stock);
                            resultado.Add("id_disco", id_disco);
                            resultado.Add("foto", foto);
                            resultado.Add("id_articulo", id_articulo);

                            resultados.Add(new Dictionary<string, object> { { nombre, resultado } });
                        }
                    }
                }

                return new OkObjectResult(resultados);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [FunctionName("capturar_instrumento")]
        public static async Task<IActionResult> capturar_instrumento(
         [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
         ILogger log)
        {
            try
            {

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamCapturarInstrumento instrumentoRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamCapturarInstrumento>(requestBody);
                
                // Comprobamos que la cookie sea valida
                if (comprobarAdministrador(instrumentoRequest.cookie) < 0)
                {
                    return new BadRequestObjectResult(new { error = "Usted no está autorizado a realizar esta operación" });
                }

                // Validar el instrumento
                if (!ValidarInstrumento(instrumentoRequest, out string errorMessage))
                {
                    return new BadRequestObjectResult(new { error = errorMessage });
                }


                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using MySqlTransaction transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Insertar el instrumento en la tabla
                    MySqlCommand command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "INSERT INTO articulos (nombre, descripcion, precio, stock, foto) " +
                        "VALUES (@Nombre, @Descripcion, @Precio, @Stock, @Foto)";
                    command.Parameters.AddWithValue("@Nombre", instrumentoRequest.nombre);
                    command.Parameters.AddWithValue("@Descripcion", instrumentoRequest.descripcion);
                    command.Parameters.AddWithValue("@Precio", instrumentoRequest.precio);
                    command.Parameters.AddWithValue("@Stock", instrumentoRequest.stock);
                    command.Parameters.AddWithValue("@Foto", instrumentoRequest.foto ?? (object)DBNull.Value);

                    await command.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();

                    return new OkObjectResult("Instrumento capturado exitosamente.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
                
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }


        private static bool ValidarInstrumento(ParamCapturarInstrumento instrumento, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrEmpty(instrumento.nombre) || instrumento.nombre.Length > 80)
            {
                errorMessage = "El nombre del instrumento debe tener entre 1 y 80 caracteres.";
                return false;
            }

            if (string.IsNullOrEmpty(instrumento.descripcion) || instrumento.descripcion.Length > 280)
            {
                errorMessage = "La descripción del instrumento debe tener entre 1 y 280 caracteres.";
                return false;
            }

            if (instrumento.precio == null)
            {
                errorMessage = "Ingrese un precio al instrumento";
                return false;
            }

            if (instrumento.stock == null)
            {
                errorMessage = "Ingrese una cantidad al instrumento";
                return false;
            }

            if (instrumento.precio <= 0)
            {
                errorMessage = "El precio del instrumento debe ser mayor a 0.";
                return false;
            }

            if (instrumento.stock <= 0)
            {
                errorMessage = "El stock del instrumento debe ser mayor a 0.";
                return false;
            }

            return true;
        }


        [FunctionName("capturar_disco")]
        public static async Task<IActionResult> capturar_disco(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
    ILogger log)
        {
            try
            {
                // Leer el JSON de la solicitud
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamCapturarDisco discoRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamCapturarDisco>(requestBody);

                //Comprobamos que la cookie sea valida
                if (comprobarAdministrador(discoRequest.cookie) < 0)
                {
                    return new BadRequestObjectResult(new { error = "Usted no está autorizado a realizar esta operación" });
                }

                // Validar el disco
                if (!ValidarDisco(discoRequest, out string errorMessage))
                {
                    return new BadRequestObjectResult(new { error = errorMessage });
                }

                // Crear la conexión a la base de datos
                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using MySqlTransaction transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Insertar el disco en la tabla discos
                    int idDisco;
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "INSERT INTO discos (autor, ano_disco, duracion) " +
                            "VALUES (@Autor, @AnoDisco, @Duracion); SELECT LAST_INSERT_ID();";
                        command.Parameters.AddWithValue("@Autor", discoRequest.autor);
                        command.Parameters.AddWithValue("@AnoDisco", discoRequest.ano_disco);
                        command.Parameters.AddWithValue("@Duracion", discoRequest.duracion);

                        idDisco = Convert.ToInt32(await command.ExecuteScalarAsync());
                    }

                    // Insertar el resto de los campos en la tabla articulos
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "INSERT INTO articulos (nombre, descripcion, precio, stock, foto, id_disco) " +
                            "VALUES (@Nombre, @Descripcion, @Precio, @Stock, @Foto, @IdDisco)";
                        command.Parameters.AddWithValue("@Nombre", discoRequest.nombre);
                        command.Parameters.AddWithValue("@Descripcion", discoRequest.descripcion);
                        command.Parameters.AddWithValue("@Precio", discoRequest.precio);
                        command.Parameters.AddWithValue("@Stock", discoRequest.stock);
                        command.Parameters.AddWithValue("@Foto", discoRequest.foto ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IdDisco", idDisco);

                        await command.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();

                    return new OkObjectResult("Disco capturado exitosamente.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [FunctionName("anadir_carrito")]
        public static async Task<IActionResult> anadirCarrito(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
    ILogger log)
        {
            try
            {
                // Leer el JSON de la solicitud
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamAnadirCarrito carritoRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamAnadirCarrito>(requestBody);

                int id_usuario = comprobarUsuario(carritoRequest.cookie);

                if (id_usuario < 0)
                {
                    return new BadRequestObjectResult(new { error = "Por favor inicie sesion" });
                }

                // Crear la conexión a la base de datos
                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using MySqlTransaction transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Verificar la cantidad disponible en la tabla articulos
                    int stockDisponible;
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "SELECT stock FROM articulos WHERE id_articulo = @IdArticulo";
                        command.Parameters.AddWithValue("@IdArticulo", carritoRequest.id_articulo);

                        object result = await command.ExecuteScalarAsync();
                        stockDisponible = Convert.ToInt32(result);
                    }

                    // Verificar si hay suficiente cantidad disponible
                    if (carritoRequest.cantidad > stockDisponible)
                    {
                        await transaction.RollbackAsync();
                        return new BadRequestObjectResult(new { error = $"Solo hay {stockDisponible} unidades disponibles." });
                    }

                    // Actualizar la cantidad en la tabla articulos
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "UPDATE articulos SET stock = stock - @Cantidad WHERE id_articulo = @IdArticulo";
                        command.Parameters.AddWithValue("@Cantidad", carritoRequest.cantidad);
                        command.Parameters.AddWithValue("@IdArticulo", carritoRequest.id_articulo);

                        await command.ExecuteNonQueryAsync();
                    }

                    //FUNCIONES ADICIONALES===============================================

                    bool existeRegistro;
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "SELECT COUNT(*) FROM carrito WHERE id_articulo = @IdArticulo AND id_usuario = @IdUsuario";
                        command.Parameters.AddWithValue("@IdArticulo", carritoRequest.id_articulo);
                        command.Parameters.AddWithValue("@IdUsuario", id_usuario);

                        int rowCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                        existeRegistro = rowCount > 0;
                    }

                    if (existeRegistro)
                    {
                        using (MySqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandType = System.Data.CommandType.Text;
                            command.CommandText = "UPDATE carrito SET cantidad = cantidad + @Cantidad WHERE id_articulo = @IdArticulo AND id_usuario = @IdUsuario";
                            command.Parameters.AddWithValue("@Cantidad", carritoRequest.cantidad);
                            command.Parameters.AddWithValue("@IdArticulo", carritoRequest.id_articulo);
                            command.Parameters.AddWithValue("@IdUsuario", id_usuario);

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        // Insertar el registro en la tabla carrito
                        using (MySqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandType = System.Data.CommandType.Text;
                            command.CommandText = "INSERT INTO carrito (id_articulo, id_usuario, cantidad) " +
                                                  "VALUES (@IdArticulo, @IdUsuario, @Cantidad)";
                            command.Parameters.AddWithValue("@IdArticulo", carritoRequest.id_articulo);
                            command.Parameters.AddWithValue("@IdUsuario", id_usuario); // Id de usuario obtenido mediante la cookie
                            command.Parameters.AddWithValue("@Cantidad", carritoRequest.cantidad);

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    //====================================================================
                    await transaction.CommitAsync();

                    // Retornar la respuesta exitosa
                    string mensaje = $"Se ha añadido exitosamente {carritoRequest.cantidad} unidades.";
                    return new OkObjectResult(new { mensaje });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    log.LogError(ex, "Error al procesar la solicitud");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }



        [FunctionName("incrementar_cantidad")]
        public static async Task<IActionResult> incrementarCantidad(
    [HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req,
    ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamIncrementarCantidad incrementarCantidadRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamIncrementarCantidad>(requestBody);

                // Comprobamos que la cookie sea válida
                int id_usuario = comprobarUsuario(incrementarCantidadRequest.cookie);

                if (id_usuario < 0)
                {
                    return new BadRequestObjectResult(new { error = "Su sesión ha expirado" });
                }

                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using MySqlTransaction transaction = await connection.BeginTransactionAsync();

                try
                {
                    int cantidadDisponible;
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "SELECT stock FROM articulos WHERE id_articulo = @IdArticulo";
                        command.Parameters.AddWithValue("@IdArticulo", incrementarCantidadRequest.id_articulo);

                        object result = await command.ExecuteScalarAsync();
                        cantidadDisponible = Convert.ToInt32(result);
                    }

                    if (cantidadDisponible <= 0)
                    {
                        await transaction.RollbackAsync();
                        return new BadRequestObjectResult(new { error = "No hay más artículos disponibles." });
                    }

                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "UPDATE articulos SET stock = stock - 1 WHERE id_articulo = @IdArticulo";
                        command.Parameters.AddWithValue("@IdArticulo", incrementarCantidadRequest.id_articulo);

                        await command.ExecuteNonQueryAsync();
                    }

                    // Incrementar la cantidad en la tabla carrito
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "UPDATE carrito SET cantidad = cantidad + 1 " +
                                              "WHERE id_usuario = @IdUsuario AND id_articulo = @IdArticulo";
                        command.Parameters.AddWithValue("@IdUsuario", id_usuario);
                        command.Parameters.AddWithValue("@IdArticulo", incrementarCantidadRequest.id_articulo);

                        await command.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();

                    // Retornar la respuesta exitosa
                    string mensaje = "Se ha añadido exitosamente una unidad de este artículo.";
                    return new OkObjectResult(new { mensaje });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    log.LogError(ex, "Error al procesar la solicitud");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [FunctionName("decrementar_cantidad")]
        public static async Task<IActionResult> decrementarCantidad(
    [HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req,
    ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamDecrementarCantidad decrementarCantidadRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamDecrementarCantidad>(requestBody);

                int id_usuario = comprobarUsuario(decrementarCantidadRequest.cookie);

                if (id_usuario < 0)
                {
                    return new BadRequestObjectResult(new { error = "Su sesión ha expirado" });
                }

                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using MySqlTransaction transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Obtener la cantidad actual del carrito
                    int cantidadActual;
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "SELECT cantidad FROM carrito WHERE id_usuario = @IdUsuario AND id_articulo = @IdArticulo";
                        command.Parameters.AddWithValue("@IdUsuario", id_usuario);
                        command.Parameters.AddWithValue("@IdArticulo", decrementarCantidadRequest.id_articulo);

                        object result = await command.ExecuteScalarAsync();
                        cantidadActual = Convert.ToInt32(result);
                    }

                    // Verificar si hay más de una unidad en el carrito
                    if (cantidadActual > 1)
                    {
                        // Actualizar la cantidad en la tabla carrito
                        using (MySqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandType = System.Data.CommandType.Text;
                            command.CommandText = "UPDATE carrito SET cantidad = cantidad - 1 WHERE id_usuario = @IdUsuario AND id_articulo = @IdArticulo";
                            command.Parameters.AddWithValue("@IdUsuario", id_usuario);
                            command.Parameters.AddWithValue("@IdArticulo", decrementarCantidadRequest.id_articulo);

                            await command.ExecuteNonQueryAsync();
                        }

                        // Incrementar la cantidad en la tabla articulos
                        using (MySqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandType = System.Data.CommandType.Text;
                            command.CommandText = "UPDATE articulos SET stock = stock + 1 WHERE id_articulo = @IdArticulo";
                            command.Parameters.AddWithValue("@IdArticulo", decrementarCantidadRequest.id_articulo);

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    else if (cantidadActual == 1)
                    {
                        // Eliminar el registro del carrito
                        using (MySqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandType = System.Data.CommandType.Text;
                            command.CommandText = "DELETE FROM carrito WHERE id_usuario = @IdUsuario AND id_articulo = @IdArticulo";
                            command.Parameters.AddWithValue("@IdUsuario", id_usuario);
                            command.Parameters.AddWithValue("@IdArticulo", decrementarCantidadRequest.id_articulo);

                            await command.ExecuteNonQueryAsync();
                        }

                        // Incrementar la cantidad en la tabla articulos
                        using (MySqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandType = System.Data.CommandType.Text;
                            command.CommandText = "UPDATE articulos SET stock = stock + 1 WHERE id_articulo = @IdArticulo";
                            command.Parameters.AddWithValue("@IdArticulo", decrementarCantidadRequest.id_articulo);

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return new BadRequestObjectResult(new { error = "No hay unidades para decrementar en el carrito." });
                    }

                    await transaction.CommitAsync();

                    // Retornar la respuesta exitosa
                    string mensaje = "Se ha decrementado exitosamente una unidad.";
                    return new OkObjectResult(new { mensaje });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    log.LogError(ex, "Error al procesar la solicitud");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }


        [FunctionName("eliminar_articulo")]
        public static async Task<IActionResult> eliminarArticulo(
    [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
    ILogger log)
        {
            try
            {
                // Leer el JSON de la solicitud
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamEliminarArticulo eliminarArticuloRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamEliminarArticulo>(requestBody);

                int id_usuario = comprobarUsuario(eliminarArticuloRequest.cookie);

                if (id_usuario < 0)
                {
                    return new BadRequestObjectResult(new { error = "Su sesión ha expirado" });
                }

                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using MySqlTransaction transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Obtener la cantidad en el carrito
                    int cantidadEnCarrito;
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "SELECT cantidad FROM carrito WHERE id_usuario = @IdUsuario AND id_articulo = @IdArticulo";
                        command.Parameters.AddWithValue("@IdUsuario", id_usuario);
                        command.Parameters.AddWithValue("@IdArticulo", eliminarArticuloRequest.id_articulo);

                        object result = await command.ExecuteScalarAsync();
                        cantidadEnCarrito = Convert.ToInt32(result);
                    }

                    // Incrementar la cantidad en la tabla articulos
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "UPDATE articulos SET stock = stock + @Cantidad WHERE id_articulo = @IdArticulo";
                        command.Parameters.AddWithValue("@Cantidad", cantidadEnCarrito);
                        command.Parameters.AddWithValue("@IdArticulo", eliminarArticuloRequest.id_articulo);

                        await command.ExecuteNonQueryAsync();
                    }

                    // Eliminar el registro del carrito
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "DELETE FROM carrito WHERE id_usuario = @IdUsuario AND id_articulo = @IdArticulo";
                        command.Parameters.AddWithValue("@IdUsuario", id_usuario);
                        command.Parameters.AddWithValue("@IdArticulo", eliminarArticuloRequest.id_articulo);

                        await command.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();

                    // Retornar la respuesta exitosa
                    string mensaje = "Se ha eliminado el artículo correctamente.";
                    return new OkObjectResult(new { mensaje });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    log.LogError(ex, "Error al procesar la solicitud");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }


        [FunctionName("eliminar_carrito")]
        public static async Task<IActionResult> eliminarCarrito(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                // Leer el JSON de la solicitud
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamEliminarCarrito eliminarCarritoRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamEliminarCarrito>(requestBody);

                int id_usuario = comprobarUsuario(eliminarCarritoRequest.cookie);

                if (id_usuario < 0)
                {
                    return new BadRequestObjectResult(new { error = "Su sesión ha expirado" });
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (MySqlTransaction transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // Obtener los registros del carrito del usuario
                            using (MySqlCommand command = connection.CreateCommand())
                            {
                                command.Transaction = transaction;
                                command.CommandType = System.Data.CommandType.Text;
                                command.CommandText = "SELECT id_articulo, cantidad FROM carrito WHERE id_usuario = @IdUsuario";
                                command.Parameters.AddWithValue("@IdUsuario", id_usuario);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (reader.Read())
                                    {
                                        int idArticulo = reader.GetInt32("id_articulo");
                                        int cantidad = reader.GetInt32("cantidad");

                                        // Incrementar la cantidad en la tabla articulos
                                        using (MySqlConnection connection2 = new MySqlConnection(connectionString))
                                        {
                                            await connection2.OpenAsync();

                                            using (MySqlCommand updateCommand = connection2.CreateCommand())
                                            {
                                                updateCommand.Transaction = transaction;
                                                updateCommand.CommandType = System.Data.CommandType.Text;
                                                updateCommand.CommandText = "UPDATE articulos SET stock = stock + @Cantidad WHERE id_articulo = @IdArticulo";
                                                updateCommand.Parameters.AddWithValue("@Cantidad", cantidad);
                                                updateCommand.Parameters.AddWithValue("@IdArticulo", idArticulo);

                                                await updateCommand.ExecuteNonQueryAsync();
                                            }
                                        }
                                    }

                                    reader.Close();
                                }
                            }

                            // Eliminar los registros del carrito del usuario
                            using (MySqlCommand command = connection.CreateCommand())
                            {
                                command.Transaction = transaction;
                                command.CommandType = System.Data.CommandType.Text;
                                command.CommandText = "DELETE FROM carrito WHERE id_usuario = @IdUsuario";
                                command.Parameters.AddWithValue("@IdUsuario", id_usuario);

                                await command.ExecuteNonQueryAsync();
                            }

                            await transaction.CommitAsync();

                            // Retornar la respuesta exitosa
                            string mensaje = "Se ha eliminado con éxito el carrito.";
                            return new OkObjectResult(new { mensaje });
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            log.LogError(ex, "Error al procesar la solicitud");
                            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }



        [FunctionName("obtener_carrito")]
        public static async Task<IActionResult> seleccionarCarrito(
         [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
         ILogger log)
        {
            try
            {
                // Leer el JSON de la solicitud
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamSeleccionarCarrito seleccionarCarritoRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamSeleccionarCarrito>(requestBody);

                int id_usuario = comprobarUsuario(seleccionarCarritoRequest.cookie);

                if(id_usuario < 0)
                {
                    return new BadRequestObjectResult(new { error = "Su sesion ha expirado" });
                }

                // Crear la conexión a la base de datos
                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                List<ArticuloSeleccionado> articulosSeleccionados = new List<ArticuloSeleccionado>();

                // Seleccionar los artículos del carrito y sus cantidades
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "SELECT carrito.id_articulo, articulos.nombre, articulos.precio, articulos.foto, carrito.cantidad " +
                                          "FROM carrito " +
                                          "JOIN articulos ON carrito.id_articulo = articulos.id_articulo " +
                                          "WHERE carrito.id_usuario = @IdUsuario";
                    command.Parameters.AddWithValue("@IdUsuario", id_usuario);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            int idArticulo = reader.GetInt32("id_articulo");
                            string nombre = reader.GetString("nombre");
                            int precio = reader.GetInt32("precio");
                            //byte[] foto = (byte[])reader["foto"];
                            int cantidad = reader.GetInt32("cantidad");

                            ArticuloSeleccionado articulo = new ArticuloSeleccionado();

                            articulo.id_articulo = idArticulo;
                            articulo.nombre = nombre;
                            articulo.precio = precio;
                            articulo.cantidad = cantidad;
                            if (Convert.IsDBNull(reader["foto"]))
                            {
                                articulo.foto = null;
                            }
                            else
                            {
                                byte[] reducedFoto = ReducirResolucion((byte[])reader["foto"], 240, 240, 80);
                                byte[] compressedFoto = ComprimirImagen(reducedFoto, 70);
                                articulo.foto = compressedFoto;
                            }

                            articulosSeleccionados.Add(articulo);
                        }
                    }
                }

                // Retornar los artículos seleccionados
                Dictionary<string, object> result = new Dictionary<string, object>();
                foreach (var articulo in articulosSeleccionados)
                {
                    result.Add(articulo.nombre, new
                    {
                        id_articulo = articulo.id_articulo,
                        precio = articulo.precio,
                        cantidad = articulo.cantidad,
                        foto = articulo.foto
                    });
                }

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }



        [FunctionName("hacer_compra")]
        public static async Task<IActionResult> hacerCompra(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
      ILogger log)
        {
            try
            {
                // Leer el JSON de la solicitud
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamHacerCompra hacerCompraRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamHacerCompra>(requestBody);

                int id_usuario = comprobarUsuario(hacerCompraRequest.cookie);
                if (id_usuario < 0)
                {
                    return new BadRequestObjectResult(new { mensaje = "Su sesión ha expirado" });
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (MySqlTransaction transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // Obtener los registros del carrito del usuario
                            using (MySqlConnection selectConnection = new MySqlConnection(connectionString))
                            {
                                await selectConnection.OpenAsync();

                                using (MySqlCommand command = selectConnection.CreateCommand())
                                {
                                    command.CommandType = System.Data.CommandType.Text;
                                    command.CommandText = "SELECT carrito.id_articulo, articulos.precio, carrito.cantidad " +
                                                          "FROM carrito " +
                                                          "JOIN articulos ON carrito.id_articulo = articulos.id_articulo " +
                                                          "WHERE carrito.id_usuario = @IdUsuario";
                                    command.Parameters.AddWithValue("@IdUsuario", id_usuario);

                                    using (var reader = await command.ExecuteReaderAsync())
                                    {
                                        while (reader.Read())
                                        {
                                            int idArticulo = reader.GetInt32("id_articulo");
                                            int precio = reader.GetInt32("precio");
                                            int cantidad = reader.GetInt32("cantidad");

                                            int importe = precio * cantidad;
                                            DateTime fechaActual = DateTime.Now.Date;

                                            // Insertar el registro en la tabla ventas
                                            using (MySqlConnection insertConnection = new MySqlConnection(connectionString))
                                            {
                                                await insertConnection.OpenAsync();

                                                using (MySqlCommand insertCommand = insertConnection.CreateCommand())
                                                {
                                                    insertCommand.Transaction = transaction;
                                                    insertCommand.CommandType = System.Data.CommandType.Text;
                                                    insertCommand.CommandText = "INSERT INTO ventas (id_articulo, id_usuario, cantidad, importe, fecha) " +
                                                                                "VALUES (@IdArticulo, @IdUsuario, @Cantidad, @Importe, @Fecha)";
                                                    insertCommand.Parameters.AddWithValue("@IdArticulo", idArticulo);
                                                    insertCommand.Parameters.AddWithValue("@IdUsuario", id_usuario);
                                                    insertCommand.Parameters.AddWithValue("@Cantidad", cantidad);
                                                    insertCommand.Parameters.AddWithValue("@Importe", importe);
                                                    insertCommand.Parameters.AddWithValue("@Fecha", fechaActual);

                                                    await insertCommand.ExecuteNonQueryAsync();
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Eliminar los registros del carrito del usuario
                            using (MySqlConnection deleteConnection = new MySqlConnection(connectionString))
                            {
                                await deleteConnection.OpenAsync();

                                using (MySqlCommand deleteCommand = deleteConnection.CreateCommand())
                                {
                                    deleteCommand.Transaction = transaction;
                                    deleteCommand.CommandType = System.Data.CommandType.Text;
                                    deleteCommand.CommandText = "DELETE FROM carrito WHERE id_usuario = @IdUsuario";
                                    deleteCommand.Parameters.AddWithValue("@IdUsuario", id_usuario);

                                    await deleteCommand.ExecuteNonQueryAsync();
                                }
                            }

                            // Confirmar la transacción
                            await transaction.CommitAsync();

                            // Retornar la respuesta exitosa
                            string mensaje = "Se ha realizado la compra con exito!!!";
                            return new OkObjectResult(new { mensaje });
                        }
                        catch (Exception ex)
                        {
                            // Si ocurre un error, revertir la transacción
                            await transaction.RollbackAsync();
                            log.LogError(ex, "Error al procesar la solicitud");
                            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }




        [FunctionName("obtener_ventas")]
        public static async Task<IActionResult> obtenerVentas(
          [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
          ILogger log)
        {
            try
            {
                // Leer el JSON de la solicitud
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamObtenerVentas obtenerVentasRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamObtenerVentas>(requestBody);

                int id_usuario = comprobarAdministrador(obtenerVentasRequest.cookie);
                if(id_usuario < 0)
                {
                    return new BadRequestObjectResult(new {error = "Su sesion ha expirado"});
                }


                // Crear la conexión a la base de datos
                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                Dictionary<int, Dictionary<string, object>> ventas = new Dictionary<int, Dictionary<string, object>>();

                // Seleccionar los datos de ventas y artículos
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "SELECT ventas.id_articulo, articulos.nombre, articulos.precio, ventas.cantidad, ventas.importe, ventas.fecha, articulos.id_disco " +
                                          "FROM ventas " +
                                          "JOIN articulos ON ventas.id_articulo = articulos.id_articulo";
              
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            int idArticulo = reader.GetInt32("id_articulo");
                            string nombre = reader.GetString("nombre");
                            int precio = reader.GetInt32("precio");
                            int cantidad = reader.GetInt32("cantidad");
                            int importe = reader.GetInt32("importe");
                            int? idDisco = reader.IsDBNull(reader.GetOrdinal("id_disco")) ? null : (int?)reader.GetInt32("id_disco");
                            DateTime fecha = reader.GetDateTime("fecha");

                            if (!ventas.ContainsKey(idArticulo))
                            {
                                ventas[idArticulo] = new Dictionary<string, object>
                                {
                                    { "nombre", nombre },
                                    { "precio", precio },
                                    { "cantidad", cantidad },
                                    { "importe", importe },
                                    { "fecha", fecha },
                                    {"id_disco", idDisco }
                                };
                            }
                        }
                    }
                }

                // Retornar los datos de ventas en formato de arreglo de JSONs
                Dictionary<string, object>[] result = new Dictionary<string, object>[ventas.Count];
                int index = 0;
                foreach (var venta in ventas)
                {
                    result[index] = new Dictionary<string, object>
                    {
                        { venta.Key.ToString(), venta.Value }
                    };
                    index++;
                }

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [FunctionName("obtener_ventas_fecha")]
        public static async Task<IActionResult> obtenerVentasFecha(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            try
            {
                // Leer el JSON de la solicitud
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamObtenerVentasFecha obtenerVentasFechaRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ParamObtenerVentasFecha>(requestBody);
                int id_usuario = comprobarAdministrador(obtenerVentasFechaRequest.cookie);
                if(id_usuario < 0)
                {
                    return new BadRequestObjectResult(new {mensaje = "Su sesion ha expirado"});
                }

                // Crear la conexión a la base de datos
                using MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                Dictionary<int, Dictionary<string, object>> ventas = new Dictionary<int, Dictionary<string, object>>();

                // Seleccionar los datos de ventas y artículos por fecha
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "SELECT ventas.id_articulo, articulos.nombre, articulos.precio, ventas.cantidad, ventas.importe, ventas.fecha, articulos.id_disco " +
                                          "FROM ventas " +
                                          "JOIN articulos ON ventas.id_articulo = articulos.id_articulo " +
                                          "WHERE ventas.fecha = @Fecha";
                    command.Parameters.AddWithValue("@Fecha", obtenerVentasFechaRequest.fecha);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            int idArticulo = reader.GetInt32("id_articulo");
                            string nombre = reader.GetString("nombre");
                            int precio = reader.GetInt32("precio");
                            int cantidad = reader.GetInt32("cantidad");
                            int importe = reader.GetInt32("importe");
                            int? idDisco = reader.IsDBNull(reader.GetOrdinal("id_disco")) ? null : (int?)reader.GetInt32("id_disco");
                            DateTime fecha = reader.GetDateTime("fecha");

                            if (!ventas.ContainsKey(idArticulo))
                            {
                                ventas[idArticulo] = new Dictionary<string, object>
                                {
                                    { "nombre", nombre },
                                    { "precio", precio },
                                    { "cantidad", cantidad },
                                    { "importe", importe },
                                    { "fecha", fecha },
                                    {"id_disco", idDisco }
                                };
                            }
                        }
                    }
                }

                // Retornar los datos de ventas en formato de arreglo de JSONs
                Dictionary<string, object>[] result = new Dictionary<string, object>[ventas.Count];
                int index = 0;
                foreach (var venta in ventas)
                {
                    result[index] = new Dictionary<string, object>
                    {
                        { venta.Key.ToString(), venta.Value }
                    };
                    index++;
                }

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al procesar la solicitud");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }




        private static bool ValidarDisco(ParamCapturarDisco disco, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrEmpty(disco.nombre) || disco.nombre.Length > 80)
            {
                errorMessage = "El nombre del disco debe tener entre 1 y 80 caracteres.";
                return false;
            }

            if (string.IsNullOrEmpty(disco.autor) || disco.autor.Length > 40)
            {
                errorMessage = "El autor del disco debe tener entre 1 y 40 caracteres.";
                return false;
            }

            if(disco.ano_disco == null)
            {
                errorMessage = "Ingrese un año al disco";
                return false;
            }

            if (disco.precio == null)
            {
                errorMessage = "Ingrese un precio al disco";
                return false;
            }

            if (disco.stock == null)
            {
                errorMessage = "Ingrese un stock al disco";
                return false;
            }




            if (disco.ano_disco < 1000 || disco.ano_disco > 2023)
            {
                errorMessage = "El año del disco debe estar entre 1000 y 2023.";
                return false;
            }

            if (string.IsNullOrEmpty(disco.duracion) || disco.duracion.Length > 14)
            {
                errorMessage = "La duración del disco debe tener entre 1 y 14 caracteres.";
                return false;
            }

            if (string.IsNullOrEmpty(disco.descripcion) || disco.descripcion.Length > 1000)
            {
                errorMessage = "La descripción del disco debe tener entre 1 y 1000 caracteres.";
                return false;
            }

            if (disco.precio <= 0)
            {
                errorMessage = "El precio del disco debe ser mayor a 0.";
                return false;
            }

            if (disco.stock <= 0)
            {
                errorMessage = "El stock del disco debe ser mayor a 0.";
                return false;
            }

            return true;
        }


        [FunctionName("testCookie_admin")]
        public static IActionResult CookieValidatorAdmin(
                    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
                    ILogger log)
        {

            string requestBody = new StreamReader(req.Body).ReadToEnd();

            var objetoCookie = JsonConvert.DeserializeObject<Cookie>(requestBody);

            var id = comprobarAdministrador(objetoCookie.valor);
            Console.WriteLine(objetoCookie.valor);

            if(id < 0)
            {
                return new BadRequestResult();
            }
            else
            {
                return new OkResult();
            }
            
        }

        [FunctionName("testCookie_user")]
        public static IActionResult CookieValidatorUser(
             [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log)
        {

            string requestBody = new StreamReader(req.Body).ReadToEnd();

            var objetoCookie = JsonConvert.DeserializeObject<Cookie>(requestBody);
            var valorCookie = objetoCookie.valor;

            var id = comprobarUsuario(valorCookie);
            if (id < 0)
            {
                return new BadRequestResult();
            }
            return new OkResult();
        }





        private static int comprobarUsuario(string valorCookie)
        {
            if(valorCookie != null && IsValidCookieString(valorCookie))//comprobamos si existe la cookie y si su formato es valido
            {
                string[] cookieParts = valorCookie.Split('.');
                //verificamos que el tamano sea el adecuado
                if(cookieParts.Length == 2)
                {
                    string encryptedUserId = cookieParts[0];
                    string signature = cookieParts[1];

                    //Verifica la firma de la cookie
                    if (VerifySignature(encryptedUserId, signature, serverKey))
                    {
                        string id_usuario = Decrypt(encryptedUserId, serverKey);

                        // Realiza las operaciones necesarias con el ID de usuario descifrado
                        Console.WriteLine($"ID de usuario descifrado: {id_usuario}");
                        return int.Parse(id_usuario);

                    }
                }
            }
            Console.WriteLine("Cookie para usuario incorrecta");
            return -1;
        }

        private static byte[] ComprimirImagen(byte[] originalImage, int calidad)
        {
            using (MemoryStream inputStream = new MemoryStream(originalImage))
            using (MemoryStream outputStream = new MemoryStream())
            using (SixLabors.ImageSharp.Image image = SixLabors.ImageSharp.Image.Load(inputStream))
            {
                // Configurar los parámetros de compresión de la imagen
                var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                {
                    Quality = calidad
                };

                // Comprimir la imagen y guardarla en el stream de salida
                image.Save(outputStream, encoder);

                return outputStream.ToArray();
            }
        }



        // Método para reducir la resolución y comprimir la imagen
        private static byte[] ReducirResolucion(byte[] originalImage, int nuevoAncho, int nuevoAlto, int calidad)
        {
            using (MemoryStream inputStream = new MemoryStream(originalImage))
            using (MemoryStream outputStream = new MemoryStream())
            using (SixLabors.ImageSharp.Image image = SixLabors.ImageSharp.Image.Load(inputStream))
            {
                // Redimensionar la imagen a la nueva resolución
                image.Mutate(x => x.Resize(nuevoAncho, nuevoAlto));

                // Comprimir la imagen y guardarla en formato JPEG con la calidad especificada
                image.SaveAsJpeg(outputStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder()
                {
                    Quality = calidad
                });

                return outputStream.ToArray();
            }
        }

        private static int comprobarAdministrador(string valorCookie)
        {

            if (valorCookie != null && IsValidCookieString(valorCookie))//comprobamos si existe la cookie y si su formato es valido
            {
                string[] cookieParts = valorCookie.Split('.');
                //verificamos que el tamano sea el adecuado
                if (cookieParts.Length == 3)
                {

                    string encryptedUserId = cookieParts[0];
                    string encryptedUserType = cookieParts[1];
                    string signature = cookieParts[2];

                    //Verifica la firma de la cookie
                    if (VerifySignature(encryptedUserId, signature, serverKey))
                    {
                        string id_usuario = Decrypt(encryptedUserId, serverKey);
                        string tipo_usuario = Decrypt(encryptedUserType, serverKey);

                        if(int.Parse(tipo_usuario) > 0)
                        {
                            // Realiza las operaciones necesarias con el ID de usuario descifrado
                            Console.WriteLine($"ID de ADMIN descifrado: {id_usuario}");
                            return int.Parse(id_usuario);


                        }
                    }
                }
            }

            Console.WriteLine("Cookie para Admin Incorrecta");
            return -1;
        }


        private static string HashPassword(string password)
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            Console.WriteLine(hashedPassword);
            return hashedPassword;
        }

        private static string GenerateSignature(string data, string secretKey)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private static string Encrypt(string data, string key)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.GenerateIV();

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                byte[] encryptedData;

                using (var msEncrypt = new System.IO.MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(data);
                    }

                    encryptedData = msEncrypt.ToArray();
                }

                byte[] result = new byte[aesAlg.IV.Length + encryptedData.Length];
                Array.Copy(aesAlg.IV, 0, result, 0, aesAlg.IV.Length);
                Array.Copy(encryptedData, 0, result, aesAlg.IV.Length, encryptedData.Length);

                return Convert.ToBase64String(result);
            }
        }

        private static bool IsValidCookieString(string cookieString)
        {
            string[] parts = cookieString.Split('.');
            return parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]);
        }

        private static bool VerifySignature(string data, string signature, string secretKey)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] expectedSignatureBytes = Convert.FromBase64String(signature);
                byte[] computedSignatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));

                return expectedSignatureBytes.Length == computedSignatureBytes.Length
                    && expectedSignatureBytes.AsSpan().SequenceEqual(computedSignatureBytes);
            }
        }

        private static string Decrypt(string encryptedData, string key)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                byte[] iv = new byte[aesAlg.BlockSize / 8];
                byte[] cipherText = new byte[encryptedBytes.Length - iv.Length];
                Array.Copy(encryptedBytes, iv, iv.Length);
                Array.Copy(encryptedBytes, iv.Length, cipherText, 0, cipherText.Length);

                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                string decryptedData;

                using (var msDecrypt = new System.IO.MemoryStream(cipherText))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                {
                    decryptedData = srDecrypt.ReadToEnd();
                }

                return decryptedData;
            }
        }




    }


    public class ParamCrearUsuario{   
        public string Usuario { get; set; }
        public string Contrasena { get; set; }
        public string Nombre { get; set; }
        public string ApPaterno { get; set; }
        public string ApMaterno { get; set; }
        public string Direccion { get; set; }
        public string Correo { get; set; }
        public int TipoUsuario { get; set; }
    }

    public class ParamIniciarSesion
    {
        public string Usuario { get; set; }
        public string Contrasena { get; set; }

    }


    public class UserData
    {
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string Username { get; set; }
    }

    public class Cookie
    {
        public string valor { get; set; }
    }

    public class ParamCapturarInstrumento
    {
        public string cookie { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public int? precio { get; set; }
        public int? stock { get; set; }
        public byte[] foto { get; set; }
    }


    public class ParamCapturarDisco
    {
        public string cookie { get; set; }
        public string nombre { get; set; }
        public string autor { get; set; }
        public int? ano_disco { get; set; }
        public string duracion {get; set;}
        public string descripcion { get; set; }
        public int? precio { get; set; }
        public int? stock { get; set; }
        public byte[] foto { get; set; }
    }

    public class Instrumento
    {
        public int id_articulo { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public int precio { get; set; }
        public int stock { get; set; }
        public byte[] foto { get; set; }
        public int id_disco { get; set; }
    }

    public class InstrumentoPanel
    {

        public string nombre { get; set; }
        public int precio { get; set; }
        public byte[] foto { get; set; }

    }

    public class Disco
    {
        public int id_articulo { get; set; }
        public string nombre { get; set; }
        public string autor { get; set; }
        public int ano_disco { get; set; }
        public string duracion { get; set; }
        public string descripcion { get; set; }
        public int precio { get; set; }
        public int stock { get; set; }
        public byte[] foto { get; set; }
        public int id_disco { get; set; }
    }


    public class DiscoPanel
    {
        public string nombre { get; set; }
        public int precio { get; set; }
        public byte[] foto { get; set; }
    }

    public class ParamBusquedaArticulo
    {
        public string busqueda { get; set; }
    }

    public class ParamAnadirCarrito
    {
        public string cookie { get; set; }
        public int id_articulo { get; set; }
        public int cantidad { get; set; }
    }

    public class ParamIncrementarCantidad
    {
        public string cookie { get; set; }
        public int id_articulo { get; set; }
    }

    public class ParamDecrementarCantidad
    {
        public string cookie { get; set; }
        public int id_articulo { get; set; }
    }

    public class ParamEliminarArticulo
    {
        public string cookie { get; set; }
        public int id_articulo { get; set; }
    }

    public class ParamEliminarCarrito
    {
        public string cookie { get; set; }
    }

    public class ParamSeleccionarCarrito
    {
        public string cookie { get; set; }
    }

    public class ArticuloSeleccionado
    {
        public int id_articulo { get; set; }
        public string nombre { get; set; }
        public int precio { get; set; }
        public int cantidad { get; set; }
        public byte[] foto { get; set; }
    }


    public class ParamHacerCompra
    {
        public string cookie { get; set; }
    }

    public class ParamObtenerVentas
    {
        public string cookie { get; set; }
    }

    public class ParamObtenerVentasFecha
    {
        public string cookie { get; set; }
        public DateTime fecha { get; set; }
    }

}
