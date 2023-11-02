

cargarDiscosMenuPrincipal();


function desplegarDiscos(disco){
    contenedorResultados = document.querySelector(".contenido");

    contenedorDisco = document.createElement('div');
    imagenDisco =document.createElement('img');
    tituloDisco = document.createElement('h1');
    precioDisco = document.createElement('p');


    contenedorDisco.classList.add('disco');
    contenedorDisco.onclick = () => obtenerDatosDisco(disco["nombre"]);


    //IMAGEN
    imagenDisco.alt = "Imagen del disco";
    if(disco["foto"] == null){
      imagenDisco.src = "img/image.png";
    }else{
      imagenDisco.src = "data:image/jpeg;base64,"+disco["foto"];
    }

    //TITULO
    tituloDisco.textContent = disco["nombre"];


    //PRECIO
    precioDisco.textContent =" $" + disco["precio"];

    //DESPLEGAMOS
    contenedorDisco.appendChild(imagenDisco);
    contenedorDisco.appendChild(tituloDisco);
    contenedorDisco.appendChild(precioDisco);

    contenedorResultados.appendChild(contenedorDisco);
}

function redireccionarPantallaLogin(){
    window.location.href = 'index.html';
}

function cerrarDescripcion(){
    descripcionBox = document.querySelector(".descripcionMenu");
    descripcionBox.classList.add("hide");
}

function cargarDiscosMenuPrincipal(){
    obtenerDiscosMain().then(data => {
        for(id in data){
          desplegarDiscos(data[id]);
        }

    }).catch(error => {
        alert("ERROR: " + error)
    });
}

function obtenerDiscosMain() {
    return new Promise((resolve, reject) => {
      var cliente = new WSClient(URL);
  
      cliente.postJson("obtener_discos_basico", {"tamano" : "500"},
        function(code, result) {
          if (code == 200) {
         /*   var panelResultados = document.querySelector(".results");
            panelResultados.innerHTML = ""; // Limpiamos el panel de resultados
  
            for (id in result) {
              desplegarDiscos(result[id]);
            }*/
            resolve(result); // Resolvemos la promesa con el resultado de la petición
          } else {
            reject("Ha ocurrido un error"); // Rechazamos la promesa en caso de error
          }
        });
    });
}

function obtenerDatosDisco(nombre){
  alert(nombre);
}