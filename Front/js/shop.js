function cerrarSesion(){
    var shopWindow = document.querySelector(".shop");
    var loginTitle = document.querySelector(".loginTitle");
    var loginWindow = document.querySelector(".login");
    shopWindow.classList.add("hide");
    loginTitle.classList.remove("hide");
    loginWindow.classList.remove("hide");

    eliminarCookie("sesion");
}

function seleccionarInstrumentos(){
    validarCookieU()
    var botonSelInstru = document.querySelector(".ButtonInstruments");
    var botonSelDisc = document.querySelector(".ButtonDisc");
    var botonSelSearch = document.querySelector(".sectionSearch");
    botonSelInstru.classList.add("selected");
    botonSelDisc.classList.remove("selected");
    botonSelSearch.classList.remove("selected");
    ocultarBarraBusqueda();
    generarTituloSeccion("Instrumentos");
    obtenerInstrumentos();
}

function seleccionarDiscos(){
    validarCookieU()
    var botonSelInstru = document.querySelector(".ButtonInstruments");
    var botonSelDisc = document.querySelector(".ButtonDisc");
    var botonSelSearch = document.querySelector(".sectionSearch");
    botonSelInstru.classList.remove("selected");
    botonSelDisc.classList.add("selected");
    botonSelSearch.classList.remove("selected");

    obtenerDiscos()
  .then(data => {
    //DATA
    console.log(data);

    var panelResultados = document.querySelector(".results");
    panelResultados.innerHTML = ""; // Limpiamos el panel de resultados

    for (id in data) {
      desplegarDiscos(data[id]);
    }
    ocultarBarraBusqueda();
    generarTituloSeccion("Discos");

  })
  .catch(error => {
    // Manejo de errores
    console.error(error);
  });


    //obtenerDiscos();
}

function seleccionarBuscar(){
    validarCookieU()
    var botonSelInstru = document.querySelector(".ButtonInstruments");
    var botonSelDisc = document.querySelector(".ButtonDisc");
    var botonSelSearch = document.querySelector(".sectionSearch");
    botonSelInstru.classList.remove("selected");
    botonSelDisc.classList.remove("selected");
    botonSelSearch.classList.add("selected");
    desplegarBarraBusqueda();
    generarTituloSeccion("Busqueda");
}

function desplegarBarraBusqueda(){
    var barra = document.querySelector(".searchBar");
    barra.classList.remove("hide");
}

function ocultarBarraBusqueda(){
    var barra  = document.querySelector(".searchBar");
    barra.classList.add("hide");
}

function cerrarDescripcion(){
    var descripcionWindow = document.querySelector(".descripcionMenu");
    descripcionWindow.classList.add("hide");
}

function desplegarDescripcion(){
    var descripcionWindow = document.querySelector(".descripcionMenu");
    descripcionWindow.classList.remove("hide");
}


function generarBorrar(){
    alert("Se ejcuto");
    crearCookie("Pablo","AlfaOmega",2);
}

function crearCookie(nombre, valor, dias) {
    var fecha = new Date();
    fecha.setTime(fecha.getTime() + (dias * 24 * 60 * 60 * 1000));
    var expira = "expires=" + fecha.toUTCString();
    document.cookie = nombre + "=" + valor + ";" + expira + ";path=/";
}

function generarTituloSeccion(cadena){
    var tituloSeccion = document.querySelector(".tituloSeccion");
    tituloSeccion.textContent = cadena;
}

function obtenerInstrumentos(){
    var cliente = new WSClient(URL);
    var cookie = getCookie("sesion");

    cliente.postJson("obtener_instrumentos_basico",{
        "cookie":cookie
    },
    function(code, result){
        if(code == 200){
            var panelResultados = document.querySelector(".results");
            panelResultados.innerHTML="";//Limpiamos el panel de resultados

            for(id in result){
                desplegarInstrumentos(result[id]);
            }
        }else{
            alert("Ha ocurrido un error");
        }
    });

}

function obtenerInstrumentos() {
  return new Promise((resolve, reject) => {
    var cliente = new WSClient(URL);
    var cookie = getCookie("sesion");

    cliente.postJson("obtener_instrumentos_basico", {
        "cookie": cookie
      },
      function(code, result) {
        if (code == 200) {
          var panelResultados = document.querySelector(".results");
          panelResultados.innerHTML = ""; //Limpiamos el panel de resultados

          for (id in result) {
            desplegarInstrumentos(result[id]);
          }
          resolve(result); // Resolvemos la promesa con el resultado de la petición
        } else {
          reject(new Error("Ha ocurrido un error")); // Rechazamos la promesa en caso de error
        }
      });
  });
}

function desplegarInstrumentos(articuloJson){
     var panelResultados = document.querySelector(".results");

     var productoDiv = document.createElement('div');
     var imagen = document.createElement('img');
     var titulo = document.createElement('h3');
     var precio = document.createElement('p');
     //var entrada = document.createElement('input');

     //Configuramos la imagen
     
     imagen.alt = "productoImagen";
     if(articuloJson["foto"] == null){
        imagen.src = "img/image.png";
     }else{
        imagen.src = "data:image/jpeg;base64,"+articuloJson["foto"];
     }

     //Configuramos el titulo
     titulo.textContent = articuloJson["nombre"];
     titulo.classList.add("titleProduct");

     //Configuramos el precio 
     precio.textContent = "$"+articuloJson["precio"];
     precio.classList.add("Price");

     //Configuramos la entrada
     /*
     entrada.classList.add = "hide";
     entrada.type = "text";
     entrada.value = "1";
     */

     productoDiv.classList.add("product");
     productoDiv.onclick = () => obtenerDatosIntrumento(articuloJson["nombre"]);
     productoDiv.appendChild(imagen);
     productoDiv.appendChild(titulo);
     productoDiv.appendChild(precio);
     //productoDiv.appendChild(entrada);

     panelResultados.appendChild(productoDiv);

}

function obtenerDatosIntrumento(nombreArticulo){
    var usuario = new WSClient(URL);

    usuario.postJson("obtener_articulo",
    {"busqueda":nombreArticulo},
    function(code, result){
        if(code == 200){
            //alert(JSON.stringify(result[0][nombreArticulo]));
            cargarDatosInstrumento(nombreArticulo, result[0][nombreArticulo]);
            desplegarDescripcion();
        }else{
            desplegarErrorMesage("Ha ocurrido un error al realizar la busqueda");
        }
    }
    );
}

function cargarDatosInstrumento(nombreArticulo , jsonArticulo){
    var datoArticuloDiv = document.querySelector(".descripcionMenu");
    datoArticuloDiv.innerHTML = '';
    //Configuracion de imagen
    var imagenDiv = document.createElement('div');
    imagenDiv.classList.add("descripcionImagen");

    var imagen = document.createElement('img');
    imagen.alt = "AlbumPhoto";

    if(jsonArticulo["foto"] == null){
        imagen.src = "img/image.png";
    }else{
        imagen.src = "data:image/jpeg;base64,"+jsonArticulo["foto"];
    }
    imagenDiv.appendChild(imagen);
    datoArticuloDiv.appendChild(imagenDiv);

    //Anadimos descripciones
    var descripcionIntrumento = document.createElement('div');
    descripcionIntrumento.classList.add("descripcionDatos");

    var nombre = document.createElement('h3');
    nombre.innerText = nombreArticulo;

    var precio = document.createElement('p');
    precio.innerHTML = "<b>Precio: </b>$" + jsonArticulo["precio"];

    var descripcion = document.createElement('p');
    descripcion.id = 'descripcionAlbum';
    descripcion.innerHTML = "<b>Descripci&oacute;n: </b>" + jsonArticulo["descripcion"];

    descripcionIntrumento.appendChild(nombre);
    descripcionIntrumento.appendChild(precio);
    descripcionIntrumento.appendChild(descripcion);


    //Anadimos Input de cantidad
    var divInputCantidad = document.createElement('div');
    divInputCantidad.classList.add("descripcionButtonCantidad");

    var labelInput = document.createElement('label');
    labelInput.setAttribute('for', 'cantidad');
    labelInput.textContent = "Cantidad:";

    var inputCantidad = document.createElement('input');
    inputCantidad.type = "number";
    inputCantidad.setAttribute('min', '1');
    inputCantidad.name = 'cantidad';
    inputCantidad.id = 'cantidad';
    inputCantidad.value = '1';

    divInputCantidad.appendChild(labelInput);
    divInputCantidad.appendChild(inputCantidad);

    descripcionIntrumento.appendChild(divInputCantidad);

    //Creamos el div de los botones 
    var divBotones = document.createElement('div');
    divBotones.classList.add("descripcionButtons");

    var botonAnadirCarrito = document.createElement('button');
    botonAnadirCarrito.classList.add("addButton");
    botonAnadirCarrito.textContent = "Añadir al carrito";
    botonAnadirCarrito.onclick = () => anadirArticuloCarrito(jsonArticulo["id_articulo"], nombreArticulo);

    var botonCerrarDescripcion = document.createElement('button');
    botonCerrarDescripcion.classList.add("closeButton");
    botonCerrarDescripcion.onclick = cerrarDescripcion;
    botonCerrarDescripcion.textContent = "Cerrar";

    divBotones.appendChild(botonAnadirCarrito);
    divBotones.appendChild(botonCerrarDescripcion);

    descripcionIntrumento.appendChild(divBotones);

    datoArticuloDiv.appendChild(descripcionIntrumento);

}

/*
function obtenerDiscos(){
    var cliente = new WSClient(URL);
    var cookie = getCookie("sesion");

    cliente.postJson("obtener_discos_basico",{
        "cookie":cookie
    },
    function(code, result){
        if(code == 200){
            //alert(JSON.stringify(result));
            var panelResultados = document.querySelector(".results");
            panelResultados.innerHTML="";//Limpiamos el panel de resultados

            for(id in result){
                desplegarDiscos(result[id]);
            }
        }else{
            alert("Ha ocurrido un error");
        }
    });

}*/


function obtenerDiscos() {
    return new Promise((resolve, reject) => {
      var cliente = new WSClient(URL);
      var cookie = getCookie("sesion");
  
      cliente.postJson("obtener_discos_basico", {
          "cookie": cookie
        },
        function(code, result) {
          if (code == 200) {
         /*   var panelResultados = document.querySelector(".results");
            panelResultados.innerHTML = ""; // Limpiamos el panel de resultados
  
            for (id in result) {
              desplegarDiscos(result[id]);
            }*/
            resolve(result); // Resolvemos la promesa con el resultado de la petición
          } else {
            reject(new Error("Ha ocurrido un error")); // Rechazamos la promesa en caso de error
          }
        });
    });
}


function desplegarDiscos(articuloJson){
     var panelResultados = document.querySelector(".results");

     var productoDiv = document.createElement('div');
     var imagen = document.createElement('img');
     var titulo = document.createElement('h3');
     var precio = document.createElement('p');
     //var entrada = document.createElement('input');

     //Configuramos la imagen
     
     imagen.alt = "productoImagen";
     if(articuloJson["foto"] == null){
        imagen.src = "img/image.png";
     }else{
        imagen.src = "data:image/jpeg;base64,"+articuloJson["foto"];
     }

     //Configuramos el titulo
     titulo.textContent = articuloJson["nombre"];
     titulo.classList.add("titleProduct");

     //Configuramos el precio 
     precio.textContent = "$"+articuloJson["precio"];
     precio.classList.add("Price");

     //Configuramos la entrada
     /*
     entrada.classList.add = "hide";
     entrada.type = "text";
     entrada.value = "1";
     */

     productoDiv.classList.add("product");
     productoDiv.onclick = () => obtenerDatosDisco(articuloJson["nombre"]);
     productoDiv.appendChild(imagen);
     productoDiv.appendChild(titulo);
     productoDiv.appendChild(precio);
     //productoDiv.appendChild(entrada);

     panelResultados.appendChild(productoDiv);

}

function obtenerDatosDisco(nombreArticulo){
    var usuario = new WSClient(URL);

    usuario.postJson("obtener_articulo",
    {"busqueda":nombreArticulo},
    function(code, result){
        if(code == 200){
            //alert(JSON.stringify(result[0][nombreArticulo]));
            cargarDatosDisco(nombreArticulo, result[0][nombreArticulo]);
            desplegarDescripcion();
        }else{
            desplegarErrorMesage("ERROR");
        }
    }
    );
}

function cargarDatosDisco(nombreArticulo , jsonArticulo){
    var datoArticuloDiv = document.querySelector(".descripcionMenu");
    datoArticuloDiv.innerHTML = '';
    //Configuracion de imagen
    var imagenDiv = document.createElement('div');
    imagenDiv.classList.add("descripcionImagen");

    var imagen = document.createElement('img');
    imagen.alt = "AlbumPhoto";

    if(jsonArticulo["foto"] == null){
        imagen.src = "img/image.png";
    }else{
        imagen.src = "data:image/jpeg;base64,"+jsonArticulo["foto"];
    }
    imagenDiv.appendChild(imagen);
    datoArticuloDiv.appendChild(imagenDiv);

    //Anadimos descripciones
    var descripcionDisco = document.createElement('div');
    descripcionDisco.classList.add("descripcionDatos");

    var nombre = document.createElement('h3');
    nombre.innerText = nombreArticulo;

    var autor = document.createElement('p');
    autor.innerHTML = "<b>Autor: </b>" + jsonArticulo["autor"];

    var year = document.createElement('p');
    year.innerHTML = "<b>Año: </b>" + jsonArticulo["ano_disco"];

    var duracion = document.createElement('p');
    duracion.innerHTML = "<b>Duraci&oacute;n: </b> " + jsonArticulo["duracion"];

    var precio = document.createElement('p');
    precio.innerHTML = "<b>Precio: </b>$" + jsonArticulo["precio"];

    var descripcion = document.createElement('p');
    descripcion.id = 'descripcionAlbum';
    descripcion.innerHTML = "<b>Descripci&oacute;n: </b>" + jsonArticulo["descripcion"];

    descripcionDisco.appendChild(nombre);
    descripcionDisco.appendChild(autor);
    descripcionDisco.appendChild(year);
    descripcionDisco.appendChild(duracion);
    descripcionDisco.appendChild(precio);
    descripcionDisco.appendChild(descripcion);


    //Anadimos Input de cantidad
    var divInputCantidad = document.createElement('div');
    divInputCantidad.classList.add("descripcionButtonCantidad");

    var labelInput = document.createElement('label');
    labelInput.setAttribute('for', 'cantidad');
    labelInput.textContent = "Cantidad:";

    var inputCantidad = document.createElement('input');
    inputCantidad.type = "number";
    inputCantidad.setAttribute('min', '1');
    inputCantidad.name = 'cantidad';
    inputCantidad.id = 'cantidad';
    inputCantidad.value = '1';

    divInputCantidad.appendChild(labelInput);
    divInputCantidad.appendChild(inputCantidad);

    descripcionDisco.appendChild(divInputCantidad);

    //Creamos el div de los botones 
    var divBotones = document.createElement('div');
    divBotones.classList.add("descripcionButtons");

    var botonAnadirCarrito = document.createElement('button');
    botonAnadirCarrito.classList.add("addButton");
    botonAnadirCarrito.textContent = "Añadir al carrito";
    botonAnadirCarrito.onclick = () => anadirArticuloCarrito(jsonArticulo["id_articulo"], nombreArticulo);

    var botonCerrarDescripcion = document.createElement('button');
    botonCerrarDescripcion.classList.add("closeButton");
    botonCerrarDescripcion.onclick = cerrarDescripcion;
    botonCerrarDescripcion.textContent = "Cerrar";

    divBotones.appendChild(botonAnadirCarrito);
    divBotones.appendChild(botonCerrarDescripcion);

    descripcionDisco.appendChild(divBotones);

    datoArticuloDiv.appendChild(descripcionDisco);

}

function buscarArticulo(){
    var textoBusqueda = document.getElementById("barraBusqueda").value;
    
    var usuario = new WSClient(URL);

    usuario.postJson("buscar_articulo",
    {"busqueda":textoBusqueda},
    function(code, result){
        if(code == 200){
            
            if(result.length == 0){
                desplegarAlertMesage("No se encontraron resultados");
            }else{
                desplegarResultadosBusqueda(result);
            }

        }else{
            desplegarErrorMesage("Ha ocurrido un error al realizar la busqueda");
        }
    }
    );
}

function desplegarResultadosBusqueda(resultados){
    var divResultados = document.querySelector(".results");
    divResultados.innerHTML = "";


    for(dato in resultados){
        var nombreArticulo = Object.keys(resultados[dato])[0];
        var datosArticulo = resultados[dato][nombreArticulo];

        var jsonArticulo = {
            "nombre": nombreArticulo,
            "precio": datosArticulo["precio"],
            "foto": datosArticulo["foto"]
        }

        if(datosArticulo["id_disco"] == 1){ //Es un disco
            desplegarDiscos(jsonArticulo);
        }else{ //Es un instrumento
            desplegarInstrumentos(jsonArticulo);
        }


    }
}

function anadirArticuloCarrito(id_articulo, nombre_articulo){
    var cantidad = document.getElementById("cantidad").value;
    //alert("Articulo = "+nombre_articulo+" ID = "+id_articulo+"Cantidad = "+cantidad );
    var cookie = getCookie("sesion");
    var cliente = new WSClient(URL);

    cliente.postJson("anadir_carrito", 
    {"cookie":cookie,
    "id_articulo":id_articulo,
    "cantidad":cantidad},
    function(code, result){
        if(code == 200){
            if(cantidad == 1){
                desplegarAlertMesage("Se ha añadido 1 unidad del articulo "+nombre_articulo);
            }else{
                desplegarAlertMesage("Se han añadido "+cantidad+" unidades del articulo "+nombre_articulo);
            }
            
        }else{
            desplegarErrorMesage(result["error"]);
            //desplegarErrorMesage("Por favor inicie sesión");
            //cerrarSesion();
        }
    }
    );
}


function getCookie(name){
    const value = "; "+document.cookie;
    const parts = value.split("; "+ name + "=");

    if(parts.length === 2){
        return parts.pop().split(";").shift();
    }
}

function eliminarCookie(nombre) {
    document.cookie = nombre + "=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/Front;";

}

function validarCookieU(){
    var cookie = getCookie("sesion");

    var admin = new WSClient(URL);

    admin.postJson("testCookie_user",{"valor":cookie},
        function(code, result){
            if(code != 200){
                cerrarSesion();
                return false;
            }else{
                return true;
            }
        }
    );

}
  