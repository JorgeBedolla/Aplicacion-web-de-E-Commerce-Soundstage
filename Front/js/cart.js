//import { WSClient } from './WSClient.js';

function desplegarCarrito(){
    var shopWindow = document.querySelector(".shop");
    var cartWindow = document.querySelector(".cart");

    shopWindow.classList.add("hide");
    cartWindow.classList.remove("hide");

    obtenerCarrito();
}

function cerrarSesionCarrito(){
    var cartWindow = document.querySelector(".cart");
    var loginTitle = document.querySelector(".loginTitle");
    var loginWindow = document.querySelector(".login");
    cartWindow.classList.add("hide");
    loginTitle.classList.remove("hide");
    loginWindow.classList.remove("hide");
    eliminarCookie("sesion");
}

function volverTienda(){
    var shopWindow = document.querySelector(".shop");
    var cartWindow = document.querySelector(".cart");

    shopWindow.classList.remove("hide");
    cartWindow.classList.add("hide");

}

function obtenerCarrito(){
    var cookie = getCookie("sesion");
    
    var usuario= new WSClient(URL);

    usuario.postJson("obtener_carrito",
    {"cookie":cookie},
    function(code, result){
        if(code == 200){
            //alert(JSON.stringify(result));
            desplegarArticulosCarrito(result);
            
        }else{
            desplegarErrorMesage("Por favor inicie sesión");
            volverTienda();
        }
    }
    );
    
}

function desplegarArticulosCarrito(result){
    var divResultadosCarrito = document.querySelector(".articulosCarrito");
    divResultadosCarrito.innerHTML = '';

    var costoTotal = 0;

    for(nombre in result){
        cargarArticuloCarrito(nombre, result[nombre]);

        var monto = result[nombre]["precio"] * result[nombre]["cantidad"];
        costoTotal += monto;
    }
    if(Object.keys(result) == 0){
        desplegarAlertMesage("El carrito esta vacio");
        var sinArticulosMensaje = document.createElement("h3");
        sinArticulosMensaje.classList.add("SinArticulosCarrito");
        sinArticulosMensaje.innerText = "Sin articulos en el carrito";
        divResultadosCarrito.appendChild(sinArticulosMensaje);
        
        return;
    }

    //DIV precio
    var divPrecioBotones = document.createElement("div");
    divPrecioBotones.classList.add("carritoFinal");

    var divMontoBox = document.createElement("div");
    divMontoBox.classList.add("montoBox");

    var montoTotalTexto = document.createElement("h2");
    montoTotalTexto.innerHTML = "<b>Monto total:</b> $"+costoTotal;

    divMontoBox.appendChild(montoTotalTexto);
    

    //DIV botones
    var divBotonesFinal = document.createElement("div");
    divBotonesFinal.classList.add("operacionesCarritoFinal");

    var botonPagarCarrito = document.createElement("button");
    botonPagarCarrito.classList.add("botonPagarCarrito");
    botonPagarCarrito.textContent = "Pagar";
    botonPagarCarrito.onclick = pagarCarrito;

    var botonEliminarCarrito = document.createElement("button");
    botonEliminarCarrito.classList.add("botonEliminarCarrito");
    botonEliminarCarrito.textContent = "Eliminar carrito";
    botonEliminarCarrito.onclick = eliminarCarrito;

    divBotonesFinal.appendChild(botonPagarCarrito);
    divBotonesFinal.appendChild(botonEliminarCarrito);

    divPrecioBotones.appendChild(divMontoBox);
    divPrecioBotones.appendChild(divBotonesFinal);

    divResultadosCarrito.appendChild(divPrecioBotones);



}

function cargarArticuloCarrito(nombre, datosJson){
    var divResultadosCarrito = document.querySelector(".articulosCarrito");

    var divProductoCarrito = document.createElement("div");
    divProductoCarrito.classList.add("productoC");

    //CREAMOS IMAGEN
    var imagenProductoCarrito = document.createElement("img");
    imagenProductoCarrito.alt = "Imagen producto carrito";
    if(datosJson["foto"] == null){
        imagenProductoCarrito.src = "img/image.png";
    }else{
        imagenProductoCarrito.src = "data:image/jpeg;base64,"+datosJson["foto"];
    }

    //Creamos DIV de los datos del producto
    var divDatosProducto = document.createElement("div");
    divDatosProducto.classList.add("productoDatosC");

    var nombreProductoC = document.createElement("h3");
    nombreProductoC.innerText = nombre;

    var precioProductoC = document.createElement("p");
    precioProductoC.innerHTML = "<b>Precio: </b>$" + datosJson["precio"];

    var cantidadProductoC = document.createElement("p");
    cantidadProductoC.innerHTML = "<b>Cantidad: </b>" + datosJson["cantidad"];

    var costoProductoC = document.createElement("p");
    var costoProducto = datosJson["precio"] * datosJson["cantidad"];
    costoProductoC.innerHTML = "<b>Costo: </b>$" + costoProducto;

    divDatosProducto.appendChild(nombreProductoC);
    divDatosProducto.appendChild(precioProductoC);
    divDatosProducto.appendChild(cantidadProductoC);
    divDatosProducto.appendChild(costoProductoC);

    //Creamos DIV de los botones de acciones
    var divActions = document.createElement("div");
    divActions.classList.add("productActionC");

    var incrementarBoton = document.createElement("button");
    incrementarBoton.classList.add("addButton");
    incrementarBoton.textContent = "(+) Incrementar cantidad";
    incrementarBoton.onclick = () => incrementarCantidad(datosJson["id_articulo"]);

    var decrementarBoton = document.createElement("button");
    decrementarBoton.classList.add("dicreaseButton");
    decrementarBoton.textContent = "(-) Decrementar cantidad";
    decrementarBoton.onclick = () => decrementarCantidad(datosJson["id_articulo"]);

    var eliminarBoton = document.createElement("button");
    eliminarBoton.classList.add("deleteArticleButton");
    eliminarBoton.textContent = "(x) Eliminar articulo";
    eliminarBoton.onclick = () => eliminarArticulo(datosJson["id_articulo"], nombre);

    divActions.appendChild(incrementarBoton);
    divActions.appendChild(decrementarBoton);
    divActions.appendChild(eliminarBoton);

    divProductoCarrito.appendChild(imagenProductoCarrito);
    divProductoCarrito.appendChild(divDatosProducto);
    divProductoCarrito.appendChild(divActions);

    divResultadosCarrito.appendChild(divProductoCarrito);
}

function incrementarCantidad(idArticulo){
    var cliente = new WSClient(URL);
    var cookie = getCookie("sesion");

    cliente.putJson("incrementar_cantidad",
    {"cookie":cookie, "id_articulo":idArticulo},
    function(code, result){
        if(code == 200){
            obtenerCarrito();
        }else{
            desplegarErrorMesage(result["error"]);
            //cerrarSesionCarrito();
        }
    }
    );

}

function decrementarCantidad(idArticulo){
    var cliente = new WSClient(URL);
    var cookie = getCookie("sesion");

    cliente.putJson("decrementar_cantidad", 
    {"cookie":cookie, "id_articulo":idArticulo},
    function(code, result){
        if(code == 200){
            obtenerCarrito();
        }else{
            desplegarErrorMesage(result["error"]);
        }
    }
    );
}

function eliminarArticulo(idArticulo, nombre){
    var mensaje = "¿Esta seguro de que desea eliminar el articulo "+nombre+"?";
    confirmarEliminarArticulo(mensaje, idArticulo);
    desplegarConfirmMesage();
}

function confirmarEliminarArticulo(mensaje, idArticulo){

    // Crear el elemento div principal
    var divPrincipal = document.querySelector(".confirmMesaggeWindow");
    divPrincipal.innerHTML = "";

    // Crear el elemento de imagen
    var img = document.createElement("img");
    img.src = "img/question.png";
    img.alt = "Advertencia";

    // Crear el elemento h3
    var h3 = document.createElement("h3");
    h3.textContent = mensaje;

    // Crear el div para los botones
    var divBotones = document.createElement("div");
    divBotones.classList.add("confirmMessageButtons");

    // Crear el botón "Si"
    var btnSi = document.createElement("button");
    btnSi.id = "yesButton";
    btnSi.textContent = "Si";
    btnSi.onclick = () => operacionEliminarArticulo(idArticulo);

    // Crear el botón "No"
    var btnNo = document.createElement("button");
    btnNo.id = "noButton";
    btnNo.textContent = "No";
    btnNo.onclick = cerrarConfirmMesage;

    // Agregar los elementos al div de botones
    divBotones.appendChild(btnSi);
    divBotones.appendChild(btnNo);

    // Agregar los elementos al div principal
    divPrincipal.appendChild(img);
    divPrincipal.appendChild(h3);
    divPrincipal.appendChild(divBotones);


}

function operacionEliminarArticulo(idArticulo){
    cerrarConfirmMesage();

    var cookie = getCookie("sesion");
    var cliente = new WSClient(URL);

    cliente.deleteJson("eliminar_articulo",
    {"cookie":cookie,"id_articulo":idArticulo},
    function(code, result){
        if(code == 200){
            //alert(JSON.stringify(result));
            desplegarAlertMesage(result["mensaje"]);
            obtenerCarrito();
        }else{
            desplegarErrorMesage("Ha ocurrido un error al eliminar el articulo");
        }
    }  
    );
}

function eliminarCarrito(){
    confirmarEliminarCarrito();
    desplegarConfirmMesage();
}

function confirmarEliminarCarrito(){

    // Crear el elemento div principal
    var divPrincipal = document.querySelector(".confirmMesaggeWindow");
    divPrincipal.innerHTML = "";

    // Crear el elemento de imagen
    var img = document.createElement("img");
    img.src = "img/question.png";
    img.alt = "Advertencia";

    // Crear el elemento h3
    var h3 = document.createElement("h3");
    h3.innerText = "¿Esta seguro que desea eliminar todos los articulos del carrito?";

    // Crear el div para los botones
    var divBotones = document.createElement("div");
    divBotones.classList.add("confirmMessageButtons");

    // Crear el botón "Si"
    var btnSi = document.createElement("button");
    btnSi.id = "yesButton";
    btnSi.textContent = "Si";
    btnSi.onclick = operacionEliminarCarrito;

    // Crear el botón "No"
    var btnNo = document.createElement("button");
    btnNo.id = "noButton";
    btnNo.textContent = "No";
    btnNo.onclick = cerrarConfirmMesage;

    // Agregar los elementos al div de botones
    divBotones.appendChild(btnSi);
    divBotones.appendChild(btnNo);

    // Agregar los elementos al div principal
    divPrincipal.appendChild(img);
    divPrincipal.appendChild(h3);
    divPrincipal.appendChild(divBotones);
}

function operacionEliminarCarrito(){
    cerrarConfirmMesage();

    var cookie = getCookie("sesion");

    var cliente = new WSClient(URL);

    cliente.deleteJson("eliminar_carrito",{"cookie":cookie},
        function(code, result){
            if(code == 200){
                //desplegarAlertMesage("Se ha eliminado el carrito correctamente");
                obtenerCarrito();
            }else{
                desplegarErrorMesage("Ha ocurrido un error al eliminar el carrito");
            }
        }
    );

}

function pagarCarrito(){
    alert("A partir de aqui implementar APIs de terceros");
    OperacionHacerCompra();
}

function OperacionHacerCompra(){
    var cookie = getCookie("sesion");

    var cliente = new WSClient(URL);

    cliente.postJson(
        "hacer_compra",
        {"cookie":cookie},
        function(code, result){
            if(code == 200){
                desplegarAlertMesage("Se ha realizado la compra con exito");
                cargarDiscosMenuPrincipal();
            }
        }
    );
}
