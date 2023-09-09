var URL = "http://192.168.1.66:7077/api";
var foto = null;

function seleccionarInventario(){
    
        validarCookie()
            //PESTAÑAS
        var pestInv = document.querySelector(".ButtonInv");
        var pestReg = document.querySelector(".ButtonReg");
        var pestVen = document.querySelector(".sectionVen");

        pestInv.classList.add("selected");
        pestReg.classList.remove("selected");
        pestVen.classList.remove("selected");

        //BOXES
        var invBox = document.querySelector(".inventario");
        var regBox = document.querySelector(".registrarPa");
        var venBox = document.querySelector(".ventas");

        invBox.classList.remove("hide");
        regBox.classList.add("hide");
        venBox.classList.add("hide");

        desplegarInventario();
}

function seleccionarRegistrar(){
    
        
    validarCookie()
        //PESTAÑAS
    var pestInv = document.querySelector(".ButtonInv");
    var pestReg = document.querySelector(".ButtonReg");
    var pestVen = document.querySelector(".sectionVen");

    pestInv.classList.remove("selected");
    pestReg.classList.add("selected");
    pestVen.classList.remove("selected");

        //BOXES
    var invBox = document.querySelector(".inventario");
    var regBox = document.querySelector(".registrarPa");
    var venBox = document.querySelector(".ventas");

    invBox.classList.add("hide");
    regBox.classList.remove("hide");
    venBox.classList.add("hide");
     
 
}



function seleccionarVentas(){

      //PESTAÑAS
    var pestInv = document.querySelector(".ButtonInv");
    var pestReg = document.querySelector(".ButtonReg");
    var pestVen = document.querySelector(".sectionVen");
  
    pestInv.classList.remove("selected");
    pestReg.classList.remove("selected");
    pestVen.classList.add("selected");
  
      //BOXES
    var invBox = document.querySelector(".inventario");
    var regBox = document.querySelector(".registrarPa");
    var venBox = document.querySelector(".ventas");
  
    invBox.classList.add("hide");
    regBox.classList.add("hide");
    venBox.classList.remove("hide");
  
    var allRadioButton = document.getElementById('todas');
    allRadioButton.checked = true;

    seleccionarTodasLasVentas();
}


function seleccionarRegistrarInstrumento(){
    var bloqueDisc = document.querySelector(".camposDisco");
    bloqueDisc.classList.add("hide");
}

function seleccionarRegistrarDisco(){
    var bloqueDisc = document.querySelector(".camposDisco");
    bloqueDisc.classList.remove("hide");
}

function seleccionarImagen(files, imagen){
    
    
    var file = files[0];
    if(!file)return;

    var reader = new FileReader();
    reader.onload = function(e){
        imagen.src = reader.result;
        foto = reader.result.split(',')[1];
    };
    reader.readAsDataURL(file);
}

function quita_foto(){
    foto = null;
    document.getElementById('imagenPre').src = 'img/imgAdmin/image.png';
    document.getElementById('imagen').value = '';
}

function seleccionarTodasLasVentas(){
    var panelFecha = document.querySelector(".panelFecha");
    panelFecha.classList.add("hide");

    var cookie = getCookie("sesion");
    var admin = new WSAdmin(URL);

    admin.postJson("obtener_ventas",{"cookie":cookie},
        function(code, result){
            if(code == 200){
                //Cargar VENTAS
                cargarTablaVentas(result);
            }    
            else{
                alert("Por favor inicie sesión");
            }
        }
    );

}


function cargarTablaVentas(arregloJsons){
    var totalVentas = 0;
    crearEncabezadosTablaVentas();
    

    for(id in arregloJsons){
        var nombre = Object.keys(arregloJsons[id])[0];
        insertarFilaTablaVentas(arregloJsons[id][nombre]);
        totalVentas += arregloJsons[id][nombre]["importe"];
    }

    var divFinal = document.querySelector(".divImporteTotalDia");
    divFinal.innerHTML = "";

    var importe = document.createElement('h3');
    importe.innerHTML = "<b>TOTAL DE VENTAS: </b>$"+ totalVentas;

    divFinal.appendChild(importe);
    tablaVentas.append(divFinal);

}

function crearEncabezadosTablaVentas(){

    var tablaVentas = document.querySelector(".ventaTable");
    tablaVentas.innerHTML="";

    var filaEncabezados = document.createElement('tr');

    var thTipo = document.createElement('th');
    thTipo.textContent = 'Tipo';
    filaEncabezados.appendChild(thTipo);

    
    var thNombre = document.createElement('th');
    thNombre.textContent = 'Nombre';
    filaEncabezados.appendChild(thNombre);

    
    var thFecha = document.createElement('th');
    thFecha.textContent = 'Fecha';
    filaEncabezados.appendChild(thFecha);

    
    var thPrecio = document.createElement('th');
    thPrecio.textContent = 'Precio';
    filaEncabezados.appendChild(thPrecio);

    var thCantidad = document.createElement('th');
    thCantidad.textContent = 'Cantidad';
    filaEncabezados.appendChild(thCantidad);

    var thImporte = document.createElement('th');
    thImporte.textContent = 'Importe';
    filaEncabezados.appendChild(thImporte);

    tablaVentas.appendChild(filaEncabezados);
}

function insertarFilaTablaVentas(jsonVenta){

    var tablaVentas = document.querySelector(".ventaTable");

    var fila = tablaVentas.insertRow();

    var celdaTipo = fila.insertCell();
    if(jsonVenta["id_disco"] != null){
        celdaTipo.textContent = "Instrumento";
    }else{
        celdaTipo.textContent = "Disco";
    }

    var celdaNombre = fila.insertCell();
    celdaNombre.textContent = jsonVenta["nombre"];

    var celdaFecha = fila.insertCell();
    celdaFecha.textContent = jsonVenta["fecha"];

    var celdaPrecio = fila.insertCell();
    celdaPrecio.textContent = jsonVenta["precio"];

    var celdaCantidad = fila.insertCell();
    celdaCantidad.textContent = jsonVenta["cantidad"];

    var celdaImporte = fila.insertCell();
    celdaImporte.textContent = jsonVenta["importe"];

}

function seleccionarVentasHoy(){
    var panelFecha = document.querySelector(".panelFecha");
    panelFecha.classList.add("hide");

    var cookie = getCookie("sesion");
    var admin = new WSAdmin(URL);
    const today = new Date();

    const year = today.getFullYear();
    const month = String(today.getMonth() + 1).padStart(2, '0');
    const day = String(today.getDate()).padStart(2, '0');

    var fecha = `${year}-${month}-${day}`;

    //var fecha = "2023-07-14";

    admin.postJson("obtener_ventas_fecha",{"cookie":cookie, "fecha":fecha},
        function(code, result){
            if(code == 200){
                //Cargar VENTAS    
                if(result.length == 0){
                    desplegarErrorMesage("No se han realizado ventas el dia de hoy");
                }else{
                    cargarTablaVentas(result);
                }
                
            }    
            else{
                alert("Ha ocurrido un error al cargar el historial de ventas");
            }
        }
    );

    
}

function seleccionarFecha(){
    var panelFecha = document.querySelector(".panelFecha");
    panelFecha.classList.remove("hide");
}

function desplegarInventario(){
    var admin = new WSAdmin(URL);

    admin.getJson("obtener_discos",
    {},
        function(code, result){
            if(code == 200){
                desplegarTablaDiscos(result);
            }    
            else{
                alert("Ha ocurrido un error al cargar los discos");
            }
        }
    );

    admin.getJson("obtener_instrumentos",
    {},
        function(code, result){
            if(code == 200){
                desplegarTablaInstrumentos(result);
            }    
            else{
                alert("Ha ocurrido un error al cargar los intrumentos");
            }
        }
    );
   
}

  
  

function desplegarTablaDiscos(result){
    desplegarEncabezadosTablaDiscos();
    for(dato in result){
        insertarFilaDisco(result[dato])
    }
}

function desplegarEncabezadosTablaDiscos(){

    var tablaDiscos = document.querySelector(".discosTable");
    tablaDiscos.innerHTML = "";


    var filaEncabezados = document.createElement('tr');

    var thNombre = document.createElement('th');
    thNombre.textContent = 'Nombre';
    thNombre.id = 'colNombreD';
    filaEncabezados.appendChild(thNombre);

    var thAutor = document.createElement('th');
    thAutor.textContent = 'Autor';
    thAutor.id = 'colAutor';
    filaEncabezados.appendChild(thAutor);

    var thAno = document.createElement('th');
    thAno.textContent = 'Año';
    thAno.id = 'colAno';
    filaEncabezados.appendChild(thAno);

    var thDuracion = document.createElement('th');
    thDuracion.textContent = 'Duración';
    thDuracion.id = 'colDur';
    filaEncabezados.appendChild(thDuracion);

    var thDescripcion = document.createElement('th');
    thDescripcion.textContent = 'Descripción';
    thDescripcion.id = 'colDesc';
    filaEncabezados.appendChild(thDescripcion);

    var thPrecio = document.createElement('th');
    thPrecio.textContent = 'Precio';
    thPrecio.id = 'colPre';
    filaEncabezados.appendChild(thPrecio);

    var thStock = document.createElement('th');
    thStock.textContent = 'Stock';
    thStock.id = 'colStock';
    filaEncabezados.appendChild(thStock);

    tablaDiscos.appendChild(filaEncabezados);
    
}

function desplegarTablaInstrumentos(result){
    desplegarEncabezadosTablaInstrumentos();
    for(dato in result){
        insertarFilaInstrumento(result[dato]);
    }
}

function desplegarEncabezadosTablaInstrumentos(){
    var tablaInstrumentos = document.querySelector(".instrumentosTable");
    tablaInstrumentos.innerHTML="";

    var filaEncabezados = document.createElement('tr');

    var thNombre = document.createElement('th');
    thNombre.textContent = 'Nombre';
    filaEncabezados.appendChild(thNombre);

    
    var thDescripcion = document.createElement('th');
    thDescripcion.textContent = 'Descripcion';
    filaEncabezados.appendChild(thDescripcion);

    
    var thPrecio = document.createElement('th');
    thPrecio.textContent = 'Precio';
    filaEncabezados.appendChild(thPrecio);

    
    var thStock = document.createElement('th');
    thStock.textContent = 'Stock';
    filaEncabezados.appendChild(thStock);

    tablaInstrumentos.appendChild(filaEncabezados);

}

function insertarFilaDisco(jsonArticulo){
    var tablaDiscos = document.querySelector(".discosTable");

    var fila = tablaDiscos.insertRow();

    var celdaNombre = fila.insertCell();
    celdaNombre.textContent = jsonArticulo["nombre"];

    var celdaAutor = fila.insertCell();
    celdaAutor.textContent = jsonArticulo["autor"];

    var celdaAno = fila.insertCell();
    celdaAno.textContent = jsonArticulo["ano_disco"];

    var celdaDuracion = fila.insertCell();
    celdaDuracion.textContent = jsonArticulo["duracion"];

    var celdaDescripcion = fila.insertCell();
    celdaDescripcion.textContent = jsonArticulo["descripcion"];

    var celdaPrecio = fila.insertCell();
    celdaPrecio.textContent = jsonArticulo["precio"];

    var celdaStock = fila.insertCell();
    celdaStock.textContent = jsonArticulo["stock"]; 
}

function insertarFilaInstrumento(jsonArticulo){
    tablaInstrumentos = document.querySelector(".instrumentosTable");

    var fila = tablaInstrumentos.insertRow();

    var nombreCelda = fila.insertCell();
    nombreCelda.textContent = jsonArticulo["nombre"];

    var descripcionCelda = fila.insertCell();
    descripcionCelda.textContent = jsonArticulo["descripcion"];

    var precioCelda = fila.insertCell();
    precioCelda.textContent = jsonArticulo["precio"];

    var stockCelda = fila.insertCell();
    stockCelda.textContent = jsonArticulo["stock"];

}



function buscarArticuloInventario(){
    var textoBusqueda = document.querySelector('#textoBusquedaInv').value;
  
    var admin = new WSAdmin(URL);
    
    admin.postJson("buscar_articulo",
    {"busqueda":textoBusqueda},
        function(code, result){
            if(code == 200){

                if(result.length == 0){
                    desplegarAlertMesage("No se encontraron resultados");
                }else{
                    desplegarResultadosBusqueda(result);
                }
                
            }    
            else{
                desplegarErrorMesage("Ha ocurrido un error");

            }
        }
    );
}

function desplegarResultadosBusqueda(result){
    desplegarEncabezadosTablaDiscos();
    desplegarEncabezadosTablaInstrumentos();

    for(dato in result){
        
        var tempJson = result[dato];
        var nombre = Object.keys(tempJson)[0];
        

        var descripcion = tempJson[nombre]["descripcion"];
        var precio = tempJson[nombre]["precio"];
        var stock = tempJson[nombre]["stock"];
        var idDisco = tempJson[nombre]["id_disco"];


        //1 inidca que es disco
        if(idDisco == 1){
            var autor = tempJson[nombre]["autor"];
            var ano_disco = tempJson[nombre]["ano_disco"];
            var duracion = tempJson[nombre]["duracion"];


            insertarFilaDisco({
                "nombre":nombre,
                "descripcion":descripcion,
                "precio":precio,
                "stock":stock,
                "autor":autor,
                "ano_disco":ano_disco,
                "duracion":duracion
            });
            
        }else{// 0 indica que es instrumento
            insertarFilaInstrumento({
                "nombre":nombre,
                "descripcion":descripcion,
                "precio":precio,
                "stock":stock
            });

        }

    }



}


function resgistrarArticulo(){
    var seleccionInstrumento = document.getElementById('instrumento');
    var seleccionarDisco = document.getElementById('disco');

    if(seleccionInstrumento.checked){
        //Registrar instrumento
        registrarInstrumento();
    }else if(seleccionarDisco.checked){
        //registrar disco
        registrarDisco();
    }
}

function registrarInstrumento(){
    var cookie = getCookie("sesion");
    var nombre = document.getElementById('name').value;
    var descripcion = document.getElementById('descripcion').value;
    var precio = document.getElementById('precio').value;
    var stock = document.getElementById('stock').value;

    var instrumento = {
        "cookie":cookie,
        "nombre":nombre,
        "descripcion":descripcion,
        "precio":precio,
        "stock":stock,
        "foto":foto
    }

    var admin = new WSAdmin(URL);
    
    admin.postJson("capturar_instrumento",
    instrumento,
        function(code, result){
            if(code == 200){
                desplegarRegistroExitosoMesage("¡Se ha registrado el Instrumento exitosamente!");
            }    
            else{
                desplegarErrorMesage(result["error"]);
            }
        }
    );
}

function registrarDisco(){
    var cookie = getCookie("sesion");
    var nombre = document.getElementById('name').value;
    var descripcion = document.getElementById('descripcion').value;
    var precio = document.getElementById('precio').value;
    var stock = document.getElementById('stock').value;

    //DATOS DISCO
    var autor = document.getElementById('autor').value;
    var year = document.getElementById('year').value;
    var duracion = document.getElementById('duracion').value;

    var disco = {
        "cookie":cookie,
        "nombre":nombre,
        "autor": autor,
        "ano_disco":year,
        "duracion": duracion,
        "descripcion":descripcion,
        "precio":precio,
        "stock":stock,
        "foto":foto
    }

    var admin = new WSAdmin(URL);
    
    admin.postJson("capturar_disco",
    disco,
        function(code, result){
            if(code == 200){
                desplegarRegistroExitosoMesage("¡Se ha registrado el disco exitosamente!");
            }    
            else{
                desplegarErrorMesage(result["error"]);
            }
        }
    );
}

function buscarFecha(){
    var fechaSeleccionada = document.getElementById('fechaSeleccionada').value;

    var cookie = getCookie("sesion");
    var admin = new WSAdmin(URL);

    admin.postJson("obtener_ventas_fecha",{"cookie":cookie, "fecha":fechaSeleccionada},
        function(code, result){
            if(code == 200){
                //Cargar VENTAS    
                if(result.length == 0){
                    desplegarErrorMesage("No se encontraron ventas el dia: "+fechaSeleccionada);
                }else{
                    cargarTablaVentas(result);
                }
                
            }    
            else{
                alert("Ha ocurrido un error al cargar el historial de ventas");
            }
        }
    );

}

function cerrarAlertMessage(){
    var windowAlert = document.querySelector(".alertMesaggeWindow");
    windowAlert.classList.add("hide");
}

function desplegarErrorMesage(mensaje){
    var windowError = document.querySelector(".errorMesaggeWindow h3");
    windowError.textContent = mensaje;
    var windowError = document.querySelector(".errorMesaggeWindow");
    windowError.classList.remove("hide");
}

function desplegarAlertMesage(mensaje){
    var alertMessageText = document.querySelector(".alertMesaggeWindow  h3");
    alertMessageText.textContent = mensaje
    var windowAlert = document.querySelector(".alertMesaggeWindow");
    windowAlert.classList.remove("hide");

}

function desplegarRegistroExitosoMesage(mensaje){
    var registroExitosoMessageText = document.querySelector(".registroExitosoWindow  h3");
    registroExitosoMessageText.textContent = mensaje
    var windowRegistroExitoso = document.querySelector(".registroExitosoWindow");
    windowRegistroExitoso.classList.remove("hide");

}

function cerrarRegistroExitosoMesage(){
    var windowError = document.querySelector(".registroExitosoWindow");
    windowError.classList.add("hide");
    location.reload();
}
function cerrarErrorMesage(){
    var windowError = document.querySelector(".errorMesaggeWindow");
    windowError.classList.add("hide");
}

function cerrarSesion(){
    eliminarCookie("sesion");
    window.location.href = 'index.html';
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

function validarCookie(){

    var cookie = getCookie("sesion");

   var admin = new WSAdmin(URL);
   var status = true;

    admin.postJson("testCookie_admin",{"valor":cookie},
        function(code, result){
            if(code == 200){
                status = true;
            }else{
                window.location.href = 'index.html';
                status = false;
            }
        }
    );

    return status;

}
  
