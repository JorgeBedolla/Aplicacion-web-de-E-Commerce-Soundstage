


function iniciarSesion(){
    var inputUser = document.getElementById("userName");
    var inputPass = document.getElementById("pass");
    var user = inputUser.value;
    var pass = inputPass.value;
    var clientLogin = new WSClient(URL);

    clientLogin.postJson("iniciar_sesion",
        {
            "Usuario":user,
            "Contrasena":pass
        },
        function(code, result){
            if(code == 200){
                crearCookieSesion(result);
                if(result["status"]==1){
                    seleccionarInstrumentos();
                    window.location.href = 'ad.html';
                }else{
                    desplegarMenuPrincipal();
                }   
            }    
            else{
                desplegarErrorMesage(result["message"])
            }
        }
    );

    inputUser.value = "";
    inputPass.value = "";
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
