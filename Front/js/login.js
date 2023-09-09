
function iniciarSesion(){
    ocultarPantallaLogin();
    desplegarPantallaLoginCarga();

    comprobarCredenciales().then(data =>{
        crearCookieSesion(data);
        ocultarPantallaLoginCarga();
        if(data["status"] == 1){
            seleccionarInstrumentos();
            window.location.href = 'ad.html';
        }else{
            desplegarMenuPrincipal();
        }
    }).catch(error =>{
        desplegarErrorMesage(error);
        desplegarPantallaLogin();
        ocultarPantallaLoginCarga();
    })


}

function desplegarPantallaLoginCarga(){
    var cuadro_carga_login = document.querySelector(".load_login");
    cuadro_carga_login.classList.remove("hide");
}

function desplegarPantallaLogin(){
    var cuadro_login = document.querySelector(".login");
    cuadro_login.classList.remove("hide");
}

function ocultarPantallaLogin(){
    var cuadro_login = document.querySelector(".login");
    cuadro_login.classList.add("hide");
}

function ocultarPantallaLoginCarga(){
    var cuadro_carga_login = document.querySelector(".load_login");
    cuadro_carga_login.classList.add("hide")
}

function comprobarCredenciales(){

    return new Promise((resolve, reject)=>{
        var inputUser = document.getElementById("userName");
        var inputPass = document.getElementById("pass");
        var user = inputUser.value;
        var pass = inputPass.value;
        var clientLogin = new WSClient(URL);
        inputUser.value = "";
        inputPass.value = "";

        clientLogin.postJson("iniciar_sesion",
            {
                "Usuario":user,
                "Contrasena":pass
            },
            function(code, result){
                if(code == 200){
                    /*
                    crearCookieSesion(result);
                    if(result["status"]==1){
                        seleccionarInstrumentos();
                        window.location.href = 'ad.html';
                    }else{
                        desplegarMenuPrincipal();
                    }   */
                    resolve(result);
                }    
                else{
                    reject(result["message"]);
                }
            }
        );

        
    });
}

function crearCookieSesion(jsonCookie){
    var fechaExpiracion = new Date();
    fechaExpiracion.setDate(fechaExpiracion.getDate() + 1);
    var cookie = jsonCookie["cookie"];
    
    document.cookie = "sesion="+cookie+ "; expires=" + fechaExpiracion.toUTCString();    
}



function desplegarMenuPrincipal(){
    var titleLogin = document.querySelector(".loginTitle");
    var loginBox = document.querySelector(".login");
    var shopBox = document.querySelector(".shop");

    titleLogin.classList.add("hide");
    loginBox.classList.add("hide");
    shopBox.classList.remove("hide");
    obtenerInstrumentos();
}
