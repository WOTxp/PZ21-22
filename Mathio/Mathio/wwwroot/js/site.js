
function ChangeUserName(){
    let data = $("#usernameForm").serialize();
    SendDataTo(data, "/Profile/Settings/UpdateDisplayName");
}
function ChangeFirstName(){
    let data = $("#firstNameForm").serialize();
    SendDataTo(data, "/Profile/Settings/UpdateFirstName");
}
function ChangeLastName(){
    let data = $("#lastNameForm").serialize();
    SendDataTo(data, "/Profile/Settings/UpdateLastName");
}
function ChangeDescription(){
    let data = $("#descritionForm").serialize();
    SendDataTo(data, "/Profile/Settings/UpdateDescription");
}
function OnSuccess(response){
    let successField = $("#success");
    let errorField = $("#error");
    if(response["success"] === true){
        location.reload();
    }
    successField.text("");
    errorField.text("");
    let errors = response["errors"];
    for(let x in response["errors"]){
        if(x==="auth"){
            location.replace("/Profile/SignIn?returnUrl=/Profile/Settings")
        }
        else if(x==="error"){
            errorField.text(errors[x][0]);
        }
        else{
            let field = '[data-valmsg-for="'+x+'"]';
            $(field).html(errors[x][0]);
        }
    }
    
}
function SendDataTo(data, endpoint, successMsg){
    $.ajax({
            type: "POST",
            url: endpoint,
            data: data,
            dataType: "json",
            success: function (response){
                OnSuccess(response);
            },
            error: function (){
                $("#error").text("Error sending request");
            }
    });
}