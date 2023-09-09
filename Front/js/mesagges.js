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

function cerrarErrorMesage(){
    var windowError = document.querySelector(".errorMesaggeWindow");
    windowError.classList.add("hide");
}

function desplegarConfirmMesage(){
    var divConfirMesage = document.querySelector(".confirmMesaggeWindow");
    divConfirMesage.classList.remove("hide");
}

function cerrarConfirmMesage(){
    var divConfirMesage = document.querySelector(".confirmMesaggeWindow");
    divConfirMesage.classList.add("hide");
}


